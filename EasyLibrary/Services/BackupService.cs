using EasySave.Core;
using EasySave.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Services;

public class BackupService
{
    private readonly ConfigService _config;
    private readonly CryptoService _crypto;
    private readonly BusinessSoftwareMonitor _monitor; // Ajouté pour UpdateControlState
    private readonly LoggerService _logger = new();

    private readonly Queue<FileTask> _prioSmall = new(), _prioLarge = new(), _nonPrioSmall = new(), _nonPrioLarge = new();
    private readonly object _lock = new();
    private int _totalFiles, _processedFiles;

    public event Action<BackupState>? OnProgress;

    // Constructeur mis à jour pour recevoir le moniteur
    public BackupService(ConfigService config, CryptoService crypto, BusinessSoftwareMonitor monitor)
    {
        _config = config;
        _crypto = crypto;
        _monitor = monitor;
    }

    private record FileTask(string Source, string Dest, BackupJob Job);

    public async Task Execute(List<BackupJob> jobs)
    {
        _processedFiles = 0;
        Md5Service.LoadCache();

        PrepareQueues(jobs);
        if (_totalFiles == 0) return;

        var workers = Enumerable.Range(0, _config.Current.MaxParallelFiles)
                                .Select(_ => Task.Run(WorkerLoop)).ToList();

        await Task.WhenAll(workers);
        Md5Service.SaveCache();
    }

    private void PrepareQueues(List<BackupJob> jobs)
    {
        lock (_lock)
        {
            _prioSmall.Clear(); _prioLarge.Clear(); _nonPrioSmall.Clear(); _nonPrioLarge.Clear();
            foreach (var job in jobs)
            {
                if (!Directory.Exists(job.SourceDir)) continue;
                foreach (var f in Directory.GetFiles(job.SourceDir, "*.*", SearchOption.AllDirectories))
                {
                    string dest = f.Replace(job.SourceDir, job.TargetDir);
                    if (job.Type == BackupType.Differential && !Md5Service.HasChanged(job.Name, f, Md5Service.GetHash(f))) continue;

                    Dispatch(f, dest, job);
                }
            }
            _totalFiles = _prioSmall.Count + _prioLarge.Count + _nonPrioSmall.Count + _nonPrioLarge.Count;
        }
    }

    private void WorkerLoop()
    {
        while (!JobControlService.IsStopped)
        {
            FileTask? task = null;
            lock (_lock)
            {
                if (_prioSmall.Count > 0) task = _prioSmall.Dequeue();
                else if (_prioLarge.Count > 0) task = _prioLarge.Dequeue();
                else if (_nonPrioSmall.Count > 0) task = _nonPrioSmall.Dequeue();
                else if (_nonPrioLarge.Count > 0) task = _nonPrioLarge.Dequeue();
            }

            if (task == null) break;
            ProcessFile(task);
        }
    }

    private void ProcessFile(FileTask task)
    {
        // 1. Check logiciel métier + Pause
        _monitor.UpdateControlState();
        JobControlService.WaitIfPaused();

        if (JobControlService.IsStopped) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(task.Dest)!);
            File.Copy(task.Source, task.Dest, true);

            // 2. Check Cryptage + Pause entre deux étapes
            if (_config.Current.EncryptionExtensions.Any(e => task.Dest.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
            {
                _monitor.UpdateControlState();
                JobControlService.WaitIfPaused();
                if (JobControlService.IsStopped) return;

                _crypto.Encrypt(task.Dest);
            }

            FinalizeFile(task);
        }
        catch { }
    }

    private void FinalizeFile(FileTask task)
    {
        // On regarde dans la config si on doit logger en JSON ou non (XML)
        bool useJson = _config.Current.LogFormat == LogFormat.Json;

        // Log l'opération avec le bon paramètre
        _logger.Write(new LogEntry
        {
            JobName = task.Job.Name,
            Source = task.Source,
            Target = task.Dest,
            Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
            FileSize = new FileInfo(task.Source).Length
        }, useJson);

        // Incrémente le compteur global
        Interlocked.Increment(ref _processedFiles);

        // Notifie l'UI
        NotifyProgress(task);
    }
    private void NotifyProgress(FileTask task)
    {
        var state = new BackupState
        {
            JobId = task.Job.Id,
            Status = JobState.Active,
            LastUpdate = DateTime.Now,
            TotalFilesCount = _totalFiles,
            FilesToCopy = GetRemainingFilesForJob(task.Job.Id)
        };

        OnProgress?.Invoke(state);
    }

    private List<string> GetRemainingFilesForJob(int jobId)
    {
        lock (_lock)
        {
            // Liste tous les fichiers qui sont encore dans les files d'attente pour ce job
            return _prioSmall.Concat(_prioLarge)
                             .Concat(_nonPrioSmall)
                             .Concat(_nonPrioLarge)
                             .Where(t => t.Job.Id == jobId)
                             .Select(t => t.Source)
                             .ToList();
        }
    }

    private void Dispatch(string s, string d, BackupJob j)
    {
        bool prio = _config.Current.PriorityExtensions.Any(e => s.EndsWith(e, StringComparison.OrdinalIgnoreCase));
        bool large = new FileInfo(s).Length > (_config.Current.LargeFileThreshold * 1024);

        if (prio) { if (large) _prioLarge.Enqueue(new(s, d, j)); else _prioSmall.Enqueue(new(s, d, j)); }
        else { if (large) _nonPrioLarge.Enqueue(new(s, d, j)); else _nonPrioSmall.Enqueue(new(s, d, j)); }
    }
}
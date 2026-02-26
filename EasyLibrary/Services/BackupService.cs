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
    private readonly LoggerService _logger = new();

    // Les 4 listes imposées par la logique de priorité
    private readonly Queue<FileTask> _prioSmall = new(), _prioLarge = new(), _nonPrioSmall = new(), _nonPrioLarge = new();
    private readonly object _lock = new();
    private int _totalFiles, _processedFiles;

    public event Action<BackupState>? OnProgress;

    public BackupService(ConfigService config, CryptoService crypto)
    {
        _config = config;
        _crypto = crypto;
    }

    private record FileTask(string Source, string Dest, BackupJob Job);

    public async Task Execute(List<BackupJob> jobs)
    {
        _processedFiles = 0;
        Md5Service.LoadCache();

        // 1. PREPROCESSING (Le tri)
        PrepareQueues(jobs);
        if (_totalFiles == 0) return;

        // 2. THREAD POOL (L'exécution parallèle V3)
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
                    // On n'ajoute que si MD5 a changé (Logique différentielle)
                    if (job.Type == BackupType.Differential && !Md5Service.HasChanged(job.Name, f, Md5Service.GetHash(f))) continue;

                    Dispatch(f, dest, job);
                }
            }
            _totalFiles = _prioSmall.Count + _prioLarge.Count + _nonPrioSmall.Count + _nonPrioLarge.Count;
        }
    }

    private void WorkerLoop()
    {
        while (!JobControlService.IsStopped) // Condition d'arrêt ultra simple
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
        // On met à jour l'état de la pause en fonction du logiciel métier
        _monitor.UpdateControlState();

        // On utilise la "douane" de la télécommande qu'on a faite avant
        JobControlService.WaitIfPaused();

        if (JobControlService.IsStopped) return;
        // 1. On passe par la douane : si c'est sur pause, on s'arrête ici
        JobControlService.WaitIfPaused();

        // 2. Si on a cliqué sur Stop, on sort tout de suite
        if (JobControlService.IsStopped) return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(task.Dest)!);
            File.Copy(task.Source, task.Dest, true);

            // Chiffrement (toujours avec la vérification Stop/Pause entre chaque étape)
            if (_config.Current.EncryptionExtensions.Any(e => task.Dest.EndsWith(e)))
            {
                JobControlService.WaitIfPaused();
                if (JobControlService.IsStopped) return;
                _crypto.Encrypt(task.Dest);
            }

            FinalizeFile(task);
        }
        catch { }
    }

    private void Dispatch(string s, string d, BackupJob j)
    {
        bool prio = _config.Current.PriorityExtensions.Any(e => s.EndsWith(e));
        bool large = new FileInfo(s).Length > (_config.Current.LargeFileThreshold * 1024);
        if (prio) { if (large) _prioLarge.Enqueue(new(s, d, j)); else _prioSmall.Enqueue(new(s, d, j)); }
        else { if (large) _nonPrioLarge.Enqueue(new(s, d, j)); else _nonPrioSmall.Enqueue(new(s, d, j)); }
    }
    private void NotifyProgress(FileTask task)
    {
        // On crée l'état instantané
        var state = new BackupState
        {
            JobId = task.Job.Id, // On utilise l'ID
            Status = JobState.Active,
            LastUpdate = DateTime.Now,
            // On filtre la file d'attente pour savoir ce qu'il reste
            // (C'est ici que la magie opère)
            FilesToCopy = GetRemainingFilesForJob(task.Job.Id)
        };

        OnProgress?.Invoke(state);
    }
}
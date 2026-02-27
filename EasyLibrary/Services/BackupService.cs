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
    private readonly ConfigService _configService;
    private readonly CryptoService _cryptoService;
    private readonly BusinessSoftwareMonitor _monitor;
    private readonly StateService _stateService = new StateService();

    // Files d'attente pour le partitionnement des tâches
    private readonly Queue<FileTask> _prioSmall = new(), _prioLarge = new(), _nonPrioSmall = new(), _nonPrioLarge = new();

    private readonly object _lock = new();
    private bool _isLargeFileSlotBusy = false;
    private int _totalFilesCount;

    private List<BackupJob> _allJobsList = new();

    // Événements pour l'UI et le ViewModel
    public event Action<BackupState>? OnProgress;
    public event Action<TransferResult>? OnFileCompleted;

    private record FileTask(string Source, string Dest, BackupJob Job);

    public BackupService(ConfigService config, CryptoService crypto, BusinessSoftwareMonitor monitor)
    {
        _configService = config;
        _cryptoService = crypto;
        _monitor = monitor;

        // Lancement initial des workers (Pool persistant)
        int maxThreads = _configService.Current.MaxParallelFiles > 0 ? _configService.Current.MaxParallelFiles : 4;
        for (int i = 0; i < maxThreads; i++)
        {
            Task.Run(WorkerLoop);
        }

        Task.Run(async () =>
        {
            while (true) // Ou utilise un booléen de contrôle
            {
                _monitor.UpdateControlState(_allJobsList.ToList());

        // On attend 1 seconde SANS bloquer le thread
        await Task.Delay(1000);
            }
        });
    }

    // --- REMPLISSAGE DES FILES ---

    public void AddJobs(List<BackupJob> jobs)
    {
        JobControlService.Reset();

        lock (_lock)
        {
            _prioSmall.Clear();
            _prioLarge.Clear();
            _nonPrioSmall.Clear();
            _nonPrioLarge.Clear();
            _isLargeFileSlotBusy = false; // On libère aussi le slot "Gros fichiers"
        }
        _allJobsList = jobs;
        Md5Service.LoadCache();

        var savedStates = _stateService.ReadStates();

        Task.Run(() =>
        {
            lock (_lock)
            {
                //_totalFilesCount = 0;
                foreach (var job in jobs)
                {
                    if (JobControlService.IsStoppedAll) return;
                    if (!Directory.Exists(job.SourceDir)) continue;

                    job.TotalFilesForThisJob = 0;

                    var state = savedStates.FirstOrDefault(s => s.JobId == job.Id);

                    // Si le statut est "Paused" ou "Waiting", on peut essayer de reprendre la liste du JSON
                    if (state != null && state.FilesToCopy.Any() && state.Status == JobState.Paused)
                    {
                        foreach (var f in state.FilesToCopy)
                        {
                            if (!File.Exists(f)) continue;
                            string dest = Path.Combine(job.TargetDir, Path.GetRelativePath(job.SourceDir, f));
                            Dispatch(f, dest, job);
                            job.TotalFilesForThisJob++;
                        }
                    }
                    // SINON (si c'est Stopped, Inactive, ou un bug "Active"), on repart sur un scan frais

                    else
                    {
                        foreach (var f in Directory.EnumerateFiles(job.SourceDir, "*.*", SearchOption.AllDirectories))
                        {
                            string dest = Path.Combine(job.TargetDir, Path.GetRelativePath(job.SourceDir, f));

                            if (job.Type == BackupType.Differential &&
                                !Md5Service.HasChanged(job.Name, f, Md5Service.GetHash(f))) continue;

                            Dispatch(f, dest, job);
                            job.TotalFilesForThisJob ++;
                        }
                    }
                }
                Monitor.PulseAll(_lock); // Réveille les threads qui attendent du travail
            }
        });
    }

    // --- BOUCLE DES WORKERS ---

    private void WorkerLoop()
    {
        while (!JobControlService.IsStoppedAll)
        {
            FileTask? task = GetNextTask(out bool isLargeTask);

            if (task == null)
            {
                lock (_lock) { Monitor.Wait(_lock, 1000); }
                continue;
            }

            // Le worker s'arrête ici si le bouton "Pause All" est cliqué 
            // OU si le bouton "Pause" de ce job précis est cliqué.
            JobControlService.WaitIfPaused(task.Job);

            if (JobControlService.IsStoppedAll) break;

            try { ProcessFile(task); }
            finally
            {
                if (isLargeTask) lock (_lock) { _isLargeFileSlotBusy = false; Monitor.PulseAll(_lock); }
            }
        }
    }

    private FileTask? GetNextTask(out bool assignedToLargeSlot)
    {
        assignedToLargeSlot = false;
        lock (_lock)
        {
            // Priorité 1 : Gros Prio (si slot libre)
            if (_prioLarge.Count > 0 && !_isLargeFileSlotBusy)
            {
                assignedToLargeSlot = true; _isLargeFileSlotBusy = true;
                return _prioLarge.Dequeue();
            }
            // Priorité 2 : Petit Prio
            if (_prioSmall.Count > 0) return _prioSmall.Dequeue();

            // Priorité 3 : Gros Non-Prio (si slot libre)
            if (_nonPrioLarge.Count > 0 && !_isLargeFileSlotBusy)
            {
                assignedToLargeSlot = true; _isLargeFileSlotBusy = true;
                return _nonPrioLarge.Dequeue();
            }
            // Priorité 4 : Petit Non-Prio
            if (_nonPrioSmall.Count > 0) return _nonPrioSmall.Dequeue();

            return null;
        }
    }

    private void ProcessFile(FileTask task)
    {
        // Le moniteur met à jour l'état de JobControlService
        _monitor.UpdateControlState(_allJobsList);

        // On vérifie une dernière fois si le moniteur vient de nous bloquer
        JobControlService.WaitIfPaused(task.Job);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        long cryptTime = 0;
        bool success = true;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(task.Dest)!);
            File.Copy(task.Source, task.Dest, true);

            if (_configService.Current.EncryptionExtensions.Any(e => task.Dest.EndsWith(e, StringComparison.OrdinalIgnoreCase)))
            {
                cryptTime = _cryptoService.Encrypt(task.Dest);
            }
        }
        catch { success = false; }

        sw.Stop();
        FinalizeFile(task, sw.ElapsedMilliseconds, cryptTime, success);
    }

    private void FinalizeFile(FileTask task, long duration, long crypt, bool success)
    {
        // 1. On prépare les stats du fichier (pour le log)
        var info = new FileInfo(task.Source);
        var result = new TransferResult
        {
            JobName = task.Job.Name,
            Source = task.Source,
            Dest = task.Dest,
            Size = info.Exists ? info.Length : 0,
            TransferTimeMs = duration,
            EncryptionTimeMs = crypt,
            Success = success
        };

        // 2. On prépare l'état global du job (pour le state)
        var remainingFiles = GetRemainingFiles(task.Job.Id);
        JobState currentStatus;

        if (remainingFiles.Count == 0) currentStatus = JobState.Inactive;
        else if (JobControlService.IsStoppedAll || task.Job.IsStopped)
        {
            // Si on a cliqué sur Stop, on l'affiche dans le JSON avant que le thread ne meure
            currentStatus = JobState.Stopped;
        }
        else if (JobControlService.IsPausedAll || task.Job.IsPaused)
        {
            currentStatus = JobState.Paused;
        }
        else
        {
            currentStatus = JobState.Active;
        }

        var currentState = new BackupState
        {
            JobId = task.Job.Id,
            Status = currentStatus,
            TotalFilesCount = task.Job.TotalFilesForThisJob,
            FilesToCopy = remainingFiles
        };

        // 3. LE MOTEUR indique que c'est fini
        OnFileCompleted?.Invoke(result);
        OnProgress?.Invoke(currentState);
    }

    private void Dispatch(string s, string d, BackupJob j)
    {
        bool prio = _configService.Current.PriorityExtensions.Any(e => s.EndsWith(e, StringComparison.OrdinalIgnoreCase));
        bool large = new FileInfo(s).Length > (_configService.Current.LargeFileThreshold * 1024);

        if (prio) { if (large) _prioLarge.Enqueue(new(s, d, j)); else _prioSmall.Enqueue(new(s, d, j)); }
        else { if (large) _nonPrioLarge.Enqueue(new(s, d, j)); else _nonPrioSmall.Enqueue(new(s, d, j)); }
    }

    private List<string> GetRemainingFiles(int id)
    {
        lock (_lock)
        {
            return _prioSmall.Concat(_prioLarge).Concat(_nonPrioSmall).Concat(_nonPrioLarge)
                             .Where(t => t.Job.Id == id).Select(t => t.Source).ToList();
        }
    }

    // Pour réveiller les workers si le MainViewModel appelle StopAll
    public void ForcePulse()
    {
        lock (_lock) { Monitor.PulseAll(_lock); }
    }

}
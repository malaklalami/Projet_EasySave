using EasySave.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace EasySave.Services;

/// <summary>
/// Gère la file d'attente des fichiers à copier selon leur priorité et taille.
/// 4 files : priorité+gros, priorité+petit, normal+gros, normal+petit.
/// </summary>
public class FileTaskScheduler
{
    private readonly ConfigService _configService;
    private readonly object _lock = new();

    private readonly Queue<FileTask> _priorityLargeFiles = new();
    private readonly Queue<FileTask> _prioritySmallFiles = new();
    private readonly Queue<FileTask> _nonPriorityLargeFiles = new();
    private readonly Queue<FileTask> _nonPrioritySmallFiles = new();

    private bool _isLargeFileSlotBusy = false;
    private Dictionary<int, int> _filesCompletedByJob = new();

    public record FileTask(string SourcePath, string DestinationPath, BackupJob BackupJob);

    public FileTaskScheduler(ConfigService configService)
    {
        _configService = configService;
    }

    /// <summary>
    /// Ajoute un fichier à la file appropriée selon sa priorité et taille
    /// </summary>
    public void ScheduleFile(string sourcePath, string destinationPath, BackupJob backupJob)
    {
        bool isPriority = _configService.Current.PriorityExtensions.Any(ext => 
            sourcePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        
        bool isLargeFile = new FileInfo(sourcePath).Length > 
            (_configService.Current.LargeFileThreshold * 1024);

        lock (_lock)
        {
            if (isPriority)
            {
                if (isLargeFile)
                    _priorityLargeFiles.Enqueue(new(sourcePath, destinationPath, backupJob));
                else
                    _prioritySmallFiles.Enqueue(new(sourcePath, destinationPath, backupJob));
            }
            else
            {
                if (isLargeFile)
                    _nonPriorityLargeFiles.Enqueue(new(sourcePath, destinationPath, backupJob));
                else
                    _nonPrioritySmallFiles.Enqueue(new(sourcePath, destinationPath, backupJob));
            }
        }
    }

    /// <summary>
    /// Récupère la prochaine tâche selon la priorité
    /// </summary>
    public FileTask? GetNextTask(out bool assignedToLargeSlot)
    {
        assignedToLargeSlot = false;
        lock (_lock)
        {
            // Priorité 1 : Gros fichiers prioritaires
            if (_priorityLargeFiles.Count > 0 && !_isLargeFileSlotBusy)
            {
                assignedToLargeSlot = true;
                _isLargeFileSlotBusy = true;
                return _priorityLargeFiles.Dequeue();
            }

            // Priorité 2 : Petits fichiers prioritaires
            if (_prioritySmallFiles.Count > 0)
                return _prioritySmallFiles.Dequeue();

            // Priorité 3 : Gros fichiers non-prioritaires
            if (_nonPriorityLargeFiles.Count > 0 && !_isLargeFileSlotBusy)
            {
                assignedToLargeSlot = true;
                _isLargeFileSlotBusy = true;
                return _nonPriorityLargeFiles.Dequeue();
            }

            // Priorité 4 : Petits fichiers non-prioritaires
            if (_nonPrioritySmallFiles.Count > 0)
                return _nonPrioritySmallFiles.Dequeue();

            return null;
        }
    }

    /// <summary>
    /// Libère le slot pour gros fichiers
    /// </summary>
    public void ReleaseLargeFileSlot()
    {
        lock (_lock)
        {
            _isLargeFileSlotBusy = false;
        }
    }

    /// <summary>
    /// Récupère les fichiers restants à traiter pour un job
    /// </summary>
    public List<string> GetRemainingFilesForJob(int jobId)
    {
        lock (_lock)
        {
            return _prioritySmallFiles
                .Concat(_priorityLargeFiles)
                .Concat(_nonPrioritySmallFiles)
                .Concat(_nonPriorityLargeFiles)
                .Where(task => task.BackupJob.Id == jobId)
                .Select(task => task.SourcePath)
                .ToList();
        }
    }

    /// <summary>
    /// Réinitialise les files et le tracking
    /// </summary>
    public void ClearQueues(List<BackupJob> jobs)
    {
        lock (_lock)
        {
            _prioritySmallFiles.Clear();
            _priorityLargeFiles.Clear();
            _nonPrioritySmallFiles.Clear();
            _nonPriorityLargeFiles.Clear();
            _isLargeFileSlotBusy = false;

            _filesCompletedByJob.Clear();
            foreach (var job in jobs)
            {
                _filesCompletedByJob[job.Id] = 0;
            }
        }
    }

    /// <summary>
    /// Incrémente le compteur de fichiers complétés
    /// </summary>
    public int IncrementCompletedFiles(int jobId)
    {
        lock (_lock)
        {
            if (_filesCompletedByJob.ContainsKey(jobId))
            {
                _filesCompletedByJob[jobId]++;
                return _filesCompletedByJob[jobId];
            }
            return 0;
        }
    }

    /// <summary>
    /// Réveille les workers en attente
    /// </summary>
    public void WakeupWaitingWorkers()
    {
        lock (_lock)
        {
            Monitor.PulseAll(_lock);
        }
    }

    /// <summary>
    /// Fait attendre avant la prochaine vérification de tâches
    /// </summary>
    public void WaitForTask(int timeoutMs = 1000)
    {
        lock (_lock)
        {
            Monitor.Wait(_lock, timeoutMs);
        }
    }
}


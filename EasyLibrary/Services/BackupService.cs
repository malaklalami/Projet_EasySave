using EasySave.Core;
using EasySave.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EasySave.Services;

/// <summary>
/// Service de sauvegarde : gère les workers parallèles pour copier les fichiers.
/// Responsabilité unique : copier des fichiers de A à B de manière parallèle.
/// Les autres tâches (chiffrement, logs, etc.) sont gérées par MainViewModel.
/// </summary>
public class BackupService
{
    private readonly ConfigService _configService;
    private readonly CryptoService _cryptoService;
    private readonly BusinessSoftwareMonitor _monitor;
    private readonly FileTaskScheduler _scheduler;

    private List<BackupJob> _allJobs = new();

    // Événement : quand un fichier est complété
    public event Action<TransferResult>? OnFileCompleted;
    // Événement : mise à jour de l'état du job
    public event Action<BackupState>? OnProgress;

    public BackupService(ConfigService configService, CryptoService cryptoService, BusinessSoftwareMonitor monitor)
    {
        _configService = configService;
        _cryptoService = cryptoService;
        _monitor = monitor;
        _scheduler = new FileTaskScheduler(configService);

        // Lancer les workers parallèles
        int maxWorkers = configService.Current.MaxParallelFiles > 0 ? configService.Current.MaxParallelFiles : 4;
        for (int i = 0; i < maxWorkers; i++)
        {
            Task.Run(() => WorkerLoop());
        }

        // Mise à jour périodique du moniteur
        Task.Run(async () =>
        {
            while (true)
            {
                _monitor.UpdateControlState(_allJobs);
                await Task.Delay(1000);
            }
        });
    }

    /// <summary>
    /// Lance la sauvegarde des jobs spécifiés
    /// </summary>
    public void AddJobs(List<BackupJob> backupJobs)
    {
        _allJobs = backupJobs;
        JobControlService.Reset();

        // Scanner les fichiers et les ajouter à la file d'attente
        Task.Run(() => ScanAndScheduleFiles(backupJobs));
    }

    /// <summary>
    /// Scanne les répertoires et ajoute les fichiers à traiter à la file d'attente
    /// </summary>
    private void ScanAndScheduleFiles(List<BackupJob> backupJobs)
    {
        _scheduler.ClearQueues(backupJobs);
        Md5Service.LoadCache();

        var savedStates = new StateService().ReadStates();

        foreach (var backupJob in backupJobs)
        {
            if (JobControlService.IsStoppedAll)
                return;

            if (!Directory.Exists(backupJob.SourceDir))
                continue;

            backupJob.TotalFilesForThisJob = 0;
            var savedState = savedStates.FirstOrDefault(s => s.JobId == backupJob.Id);

            // Cas 1 : Reprendre une sauvegarde en pause
            if (savedState != null && savedState.FilesToCopy.Any() && savedState.Status == JobState.Paused)
            {
                foreach (var filePath in savedState.FilesToCopy)
                {
                    if (File.Exists(filePath))
                    {
                        string destPath = Path.Combine(backupJob.TargetDir, 
                            Path.GetRelativePath(backupJob.SourceDir, filePath));
                        _scheduler.ScheduleFile(filePath, destPath, backupJob);
                        backupJob.TotalFilesForThisJob++;
                    }
                }
            }
            // Cas 2 : Nouvelle sauvegarde
            else
            {
                foreach (var sourceFilePath in Directory.EnumerateFiles(backupJob.SourceDir, "*.*", SearchOption.AllDirectories))
                {
                    // Sauvegarde différentielle : skip les fichiers non-modifiés
                    if (backupJob.Type == BackupType.Differential)
                    {
                        string fileHash = Md5Service.GetHash(sourceFilePath);
                        if (!Md5Service.HasChanged(backupJob.Name, sourceFilePath, fileHash))
                            continue;
                    }

                    string destPath = Path.Combine(backupJob.TargetDir, 
                        Path.GetRelativePath(backupJob.SourceDir, sourceFilePath));
                    _scheduler.ScheduleFile(sourceFilePath, destPath, backupJob);
                    backupJob.TotalFilesForThisJob++;
                }
            }
        }

        _scheduler.WakeupWaitingWorkers();
    }

    /// <summary>
    /// Boucle principale d'un worker : récupère les tâches et les exécute
    /// </summary>
    private void WorkerLoop()
    {
        while (!JobControlService.IsStoppedAll)
        {
            // Récupérer la prochaine tâche
            FileTaskScheduler.FileTask? currentTask = _scheduler.GetNextTask(out bool isLargeFile);

            if (currentTask == null)
            {
                _scheduler.WaitForTask(1000);
                continue;
            }

            // Vérifier les commandes de pause
            JobControlService.WaitIfPaused(currentTask.BackupJob);

            if (JobControlService.IsStoppedAll)
                break;

            try
            {
                // Copier le fichier
                CopyFile(currentTask);
            }
            finally
            {
                if (isLargeFile)
                {
                    _scheduler.ReleaseLargeFileSlot();
                    _scheduler.WakeupWaitingWorkers();
                }
            }
        }
    }

    /// <summary>
    /// Copie un fichier de la source vers la destination.
    /// Gère aussi le chiffrement si nécessaire.
    /// </summary>
    private void CopyFile(FileTaskScheduler.FileTask fileTask)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        long encryptionTimeMs = 0;
        bool success = true;

        try
        {
            // Créer le répertoire de destination
            string destinationDirectory = Path.GetDirectoryName(fileTask.DestinationPath)!;
            Directory.CreateDirectory(destinationDirectory);

            // Copier le fichier
            File.Copy(fileTask.SourcePath, fileTask.DestinationPath, overwrite: true);

            // Chiffrer si nécessaire
            if (_configService.Current.EncryptionExtensions.Any(ext => 
                fileTask.DestinationPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                encryptionTimeMs = _cryptoService.Encrypt(fileTask.DestinationPath);
            }
        }
        catch
        {
            success = false;
        }

        stopwatch.Stop();

        // Incrémenter le compteur de fichiers complétés
        int completedCount = _scheduler.IncrementCompletedFiles(fileTask.BackupJob.Id);

        // Préparer le rapport
        var sourceFileInfo = new FileInfo(fileTask.SourcePath);
        var transferResult = new TransferResult
        {
            JobName = fileTask.BackupJob.Name,
            Source = fileTask.SourcePath,
            Dest = fileTask.DestinationPath,
            Size = sourceFileInfo.Exists ? sourceFileInfo.Length : 0,
            TransferTimeMs = stopwatch.ElapsedMilliseconds,
            EncryptionTimeMs = encryptionTimeMs,
            Success = success
        };

        // Déterminer le statut du job
        var remainingFiles = _scheduler.GetRemainingFilesForJob(fileTask.BackupJob.Id);
        JobState jobStatus = DetermineJobStatus(fileTask.BackupJob, remainingFiles.Count);

        var backupState = new BackupState
        {
            JobId = fileTask.BackupJob.Id,
            Status = jobStatus,
            TotalFilesCount = fileTask.BackupJob.TotalFilesForThisJob,
            FilesToCopy = remainingFiles,
            FilesCompleted = completedCount
        };

        // Émettre les événements
        OnFileCompleted?.Invoke(transferResult);
        OnProgress?.Invoke(backupState);
    }

    /// <summary>
    /// Détermine l'état actuel du job
    /// </summary>
    private JobState DetermineJobStatus(BackupJob backupJob, int remainingFileCount)
    {
        if (remainingFileCount == 0)
            return JobState.Inactive;

        if (JobControlService.IsStoppedAll || backupJob.IsStopped)
            return JobState.Stopped;

        if (JobControlService.IsPausedAll || backupJob.IsPaused)
            return JobState.Paused;

        return JobState.Active;
    }

    /// <summary>
    /// Force les workers à se réveiller
    /// </summary>
    public void ForcePulse()
    {
        _scheduler.WakeupWaitingWorkers();
    }
}


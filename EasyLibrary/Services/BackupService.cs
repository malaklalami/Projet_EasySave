using System;
using System.IO;
using System.Diagnostics;
using EasySave.Models;
using EasySave.Core;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EasySave.Services;

public class BackupService
{
    private readonly ConfigService _config;
    private readonly LoggerService _logger = new();
    private readonly CryptoService _crypto;
    private readonly BusinessSoftwareMonitor _monitor;

    private static readonly SemaphoreSlim _largeFileSemaphore = new(1, 1);
    private static ManualResetEventSlim _pauseEvent = new(true);
    private static CancellationTokenSource _cts = new();
    private static int _globalPriorityFilesCount = 0;

    public BackupService(ConfigService config, CryptoService crypto)
    {
        _config = config;
        _crypto = crypto;
        _monitor = new BusinessSoftwareMonitor(_config, _logger);
    }

    // --- INTERFACE AVEC L'UI (Update, Pause, Stop) ---

    public void UpdateJobInList(List<BackupJob> jobs, int index, string n, string s, string t, BackupType ty)
    {
        if (index >= 0 && index < jobs.Count)
        {
            var job = jobs[index];
            job.Name = n;
            job.SourceDir = s;
            job.TargetDir = t;
            job.Type = ty;
            job.PauseEvent.Set();
        }
    }

    public void PauseAll() => _pauseEvent.Reset();
    public void ResumeAll() => _pauseEvent.Set();
    public void StopAll() => _cts.Cancel();

    public void PauseJob(BackupJob job) => job.PauseEvent.Reset();
    public void ResumeJob(BackupJob job) => job.PauseEvent.Set();
    public void StopJob(BackupJob job) => job.JobCts.Cancel();

    // --- MÉTHODE PRINCIPALE (L'ORCHESTRATEUR) ---

    public async Task Execute(List<BackupJob> jobs, Action<BackupState> onProgress)
    {
        if (_cts.IsCancellationRequested) _cts = new CancellationTokenSource();
        _pauseEvent.Set();

        using var tcpLogger = await InitializeTcpLogger();
        var sortedTasks = PrepareTasks(jobs, onProgress);

        int processedCount = 0;
        var options = new ParallelOptions { MaxDegreeOfParallelism = _config.Current.MaxParallelFiles };

        await Parallel.ForEachAsync(sortedTasks, options, async (task, ct) =>
        {
            await ProcessSingleFile(task, tcpLogger, (fileName) =>
            {
                int current = Interlocked.Increment(ref processedCount);
                onProgress?.Invoke(new BackupState
                {
                    JobName = task.Job.Name,
                    Status = JobState.Active,
                    Progress = sortedTasks.Count > 0 ? (double)current / sortedTasks.Count * 100 : 100,
                    CurrentFile = fileName
                });
            }, onProgress);
        });
    }

    // --- TRAITEMENT INDIVIDUEL DES FICHIERS ---

    private async Task ProcessSingleFile((string FilePath, BackupJob Job, bool IsPriority) task, PersistentTcpLogger? tcpLogger, Action<string> reportProgress, Action<BackupState> onProgress)
    {
        _pauseEvent.Wait();
        task.Job.PauseEvent.Wait();
        if (_cts.Token.IsCancellationRequested) return;

        _monitor.CheckActivity(task.Job.Name, onProgress);

        string dest = task.FilePath.Replace(task.Job.SourceDir, task.Job.TargetDir);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        FileInfo fi = new FileInfo(task.FilePath);

        bool isLarge = await ApplyConstraints(task, fi, onProgress);

        try
        {
            var sw = Stopwatch.StartNew();

            await Task.Run(() => File.Copy(task.FilePath, dest, true), _cts.Token);

            // Appel de la méthode de chiffrement corrigée
            long cryptTime = ExecuteEncryptionIfRequired(dest);

            sw.Stop();

            LogExecution(task, dest, fi.Length, sw.ElapsedMilliseconds, cryptTime, tcpLogger);

            reportProgress(Path.GetFileName(task.FilePath));
        }
        finally
        {
            ReleaseConstraints(task, isLarge);
        }
    }

    // --- LOGIQUE MÉTIER SECONDAIRE ---

    private List<(string FilePath, BackupJob Job, bool IsPriority)> PrepareTasks(List<BackupJob> jobs, Action<BackupState> onProgress)
    {
        var tasks = new List<(string FilePath, BackupJob Job, bool IsPriority)>();
        foreach (var job in jobs)
        {
            job.JobCts = new CancellationTokenSource();
            job.PauseEvent.Set();
            _monitor.CheckActivity(job.Name, onProgress, true);

            var files = Directory.GetFiles(job.SourceDir, "*.*", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                bool priority = _config.Current.PriorityExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
                if (priority) Interlocked.Increment(ref _globalPriorityFilesCount);
                tasks.Add((f, job, priority));
            }
        }
        return tasks.OrderByDescending(t => t.IsPriority).ThenBy(t => Guid.NewGuid()).ToList();
    }

    private async Task<bool> ApplyConstraints((string FilePath, BackupJob Job, bool IsPriority) task, FileInfo fi, Action<BackupState> onProgress)
    {
        if (!task.IsPriority)
        {
            while (Interlocked.CompareExchange(ref _globalPriorityFilesCount, 0, 0) > 0)
            {
                await Task.Delay(500);
                _pauseEvent.Wait();
                if (_cts.Token.IsCancellationRequested) return false;
            }
        }

        bool isLarge = fi.Length > _config.Current.LargeFileThreshold;
        if (isLarge)
        {
            onProgress?.Invoke(new BackupState { JobName = task.Job.Name, Status = JobState.Waiting, CurrentFile = fi.Name });
            await _largeFileSemaphore.WaitAsync(_cts.Token);
        }
        return isLarge;
    }

    private void ReleaseConstraints((string FilePath, BackupJob Job, bool IsPriority) task, bool isLarge)
    {
        if (task.IsPriority) Interlocked.Decrement(ref _globalPriorityFilesCount);
        if (isLarge) _largeFileSemaphore.Release();
    }

    private long ExecuteEncryptionIfRequired(string destPath)
    {
        // On garde le point (ex: ".pdf") pour correspondre au MainViewModel
        string ext = Path.GetExtension(destPath).ToLower();

        if (_config.Current.EncryptionExtensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase)))
        {
            long res = _crypto.Encrypt(destPath);
            return res > 0 ? res : 0;
        }
        return 0;
    }

    private void LogExecution((string FilePath, BackupJob Job, bool IsPriority) task, string dest, long size, long time, long cryptoTime, PersistentTcpLogger? tcp)
    {
        var entry = new LogEntry
        {
            JobName = task.Job.Name,
            Source = task.FilePath,
            Target = dest,
            FileSize = size,
            TransferTimeMs = time,
            EncryptionTimeMs = cryptoTime
        };

        var strategy = _config.Current.LogStrategy;
        if (strategy == LogTarget.Local || strategy == LogTarget.Both)
            _logger.Write(entry, _config.Current.LogFormat == LogFormat.Json);

        if (strategy == LogTarget.Remote || strategy == LogTarget.Both)
            tcp?.SendLog(entry);
    }

    private async Task<PersistentTcpLogger?> InitializeTcpLogger()
    {
        var strategy = _config.Current.LogStrategy;
        if (strategy == LogTarget.Remote || strategy == LogTarget.Both)
        {
            try
            {
                var logger = new PersistentTcpLogger();
                await logger.ConnectAsync(_config.Current.RemoteIp);
                return logger;
            }
            catch { return null; }
        }
        return null;
    }
}
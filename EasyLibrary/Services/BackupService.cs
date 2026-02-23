using System;
using System.IO;
using System.Diagnostics;
using EasySave.Models;
using EasySave.Core;
using System.Threading.Tasks;

namespace EasySave.Services;

public class BackupService
{
    private readonly ConfigService _config;
    private readonly LoggerService _logger = new();
    private readonly CryptoService _crypto;
    private readonly BusinessSoftwareMonitor _monitor;

    private static CancellationTokenSource _cts = new();

    public BackupService(ConfigService config, CryptoService crypto)
    {
        _config = config;
        _crypto = crypto;
        _monitor = new BusinessSoftwareMonitor(_config, _logger);
    }


    public async Task Execute(List<BackupJob> jobs, Action<BackupState> onProgress)
    {
        // On récupère la stratégie choisie par l'utilisateur (Local, Remote, Both)
        // Note : LogTarget est une Enum (0: Local, 1: Remote, 2: Both)
        var strategy = _config.Current.LogStrategy;

        // 1. INITIALISATION CONDITIONNELLE DU LOG DISTANT
        PersistentTcpLogger? tcpLogger = null;

        if (strategy == LogTarget.Remote || strategy == LogTarget.Both)
        {
            tcpLogger = new PersistentTcpLogger();
            // On utilise l'IP configurée dans les settings
            await tcpLogger.ConnectAsync(_config.Current.RemoteIp);
        }

        try
        {
            // --- COLLECTE GLOBALE ---
            // On crée une liste qui contient TOUS les fichiers de TOUS les jobs sélectionnés
            var allTasks = new List<(string FilePath, BackupJob Job, bool IsPriority)>();

            foreach (var job in jobs)
            {
                _monitor.CheckActivity(job.Name, onProgress, true);

                var files = Directory.GetFiles(job.SourceDir, "*.*", SearchOption.AllDirectories);

                foreach (var f in files)
                {
                    bool priority = _config.Current.PriorityExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
                    allTasks.Add((f, job, priority));
                }
            }

            // On trie : d'abord TOUS les prioritaires (peu importe le job), puis le reste
            var sortedTasks = allTasks
                .OrderByDescending(t => t.IsPriority)
                .ThenBy(t => t.Job.Name)
                .ToList();

            int processedCount = 0;

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = _config.Current.MaxParallelFiles, // Nombre de threads
                
            };

            // ancienne logique de sauvegarde fichier après fichier
            // for (int i = 0; i < files.Length; i++)

            //nouvelle logique en utilisant le parallélisme
            await Parallel.ForEachAsync(sortedTasks, options, async (task, ct) =>
            {
                string currentFilePath = task.FilePath;
                BackupJob currentJob = task.Job;
                _monitor.CheckActivity(currentJob.Name, onProgress);

                string dest = currentFilePath.Replace(currentJob.SourceDir, currentJob.TargetDir);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                FileInfo fi = new FileInfo(currentFilePath);

                var sw = Stopwatch.StartNew();

                //ancienne logique 
                ///File.Copy(files[i], dest, true);
                ///


                // On utilise Task.Run pour que la copie physique ne bloque pas le thread
                await Task.Run(() => File.Copy(currentFilePath, dest, true), ct);

                long cryptTime = _config.Current.EncryptionExtensions.Contains(Path.GetExtension(dest).ToLower())
                    ? _crypto.Encrypt(dest)
                    : 0;

                sw.Stop();

                var entry = new LogEntry
                {
                    JobName = currentJob.Name,
                    Source = currentFilePath,
                    Target = dest,
                    FileSize = fi.Length,
                    TransferTimeMs = sw.ElapsedMilliseconds,
                    EncryptionTimeMs = cryptTime
                };

                // --- STRATÉGIE DE LOGGING  ---

                // A. LOG LOCAL : Uniquement si Local ou Both
                if (strategy == LogTarget.Local || strategy == LogTarget.Both)
                {
                    _logger.Write(entry, _config.Current.LogFormat == LogFormat.Json);
                }

                // B. LOG DISTANT : Uniquement si Remote ou Both
                if (strategy == LogTarget.Remote || strategy == LogTarget.Both)
                {
                    tcpLogger?.SendLog(entry);
                }

                int current = Interlocked.Increment(ref processedCount);
                onProgress?.Invoke(new BackupState
                {
                    JobName = currentJob.Name,
                    Status = JobState.Active,
                    Progress = sortedTasks.Count > 0 ? (double)current / sortedTasks.Count * 100 : 100,
                    CurrentFile = Path.GetFileName(currentFilePath)
                });
            });
        }
        finally
        {
            // On ferme la connexion proprement si elle a été ouverte
            tcpLogger?.Dispose();
        }
    }
}
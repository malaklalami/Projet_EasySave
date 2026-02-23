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

    private static readonly SemaphoreSlim _largeFileSemaphore = new(1, 1);

    // Pour mettre en pause (true = ça passe, false = on bloque)
    private static ManualResetEventSlim _pauseEvent = new(true);
    // Pour arrêter définitivement (le token qu'on passe à File.Copy)
    private static CancellationTokenSource _cts = new();

    public BackupService(ConfigService config, CryptoService crypto)
    {
        _config = config;
        _crypto = crypto;
        _monitor = new BusinessSoftwareMonitor(_config, _logger);
    }

    public void PauseAll() => _pauseEvent.Reset();  // Met tout en pause
    public void ResumeAll() => _pauseEvent.Set();   // Reprend tout
    public void StopAll() => _cts.Cancel();         // Annule tout

    public void PauseJob(BackupJob job) => job.PauseEvent.Reset();
    public void ResumeJob(BackupJob job) => job.PauseEvent.Set();
    public void StopJob(BackupJob job) => job.JobCts.Cancel();

    public async Task Execute(List<BackupJob> jobs, Action<BackupState> onProgress)
    {
        //Reinitilisation : si on veut relancer une sauvegarde après un stop
        if (_cts.IsCancellationRequested) _cts = new CancellationTokenSource();

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
                // 1. Vérification du STOP (avant de commencer le fichier)
                if (_cts.Token.IsCancellationRequested) return;

                string currentFilePath = task.FilePath;
                BackupJob currentJob = task.Job;
                currentJob.PauseEvent.Wait();

                // 2. Vérification de la pause globale
                // Si _pauseEvent est sur Reset, tous les threads s'arrêtent ici et attendent
                _pauseEvent.Wait();


                _monitor.CheckActivity(currentJob.Name, onProgress);

                string dest = currentFilePath.Replace(currentJob.SourceDir, currentJob.TargetDir);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                FileInfo fi = new FileInfo(currentFilePath);

                // C. La règle des n Ko (Bande passante)
                bool isLarge = fi.Length > _config.Current.LargeFileThreshold;
                if (isLarge)
                {
                    // On envoie un signal "Attente" avant de bloquer
                    onProgress?.Invoke(new BackupState
                    {
                        JobName = currentJob.Name,
                        Status = JobState.Waiting,
                        CurrentFile = Path.GetFileName(currentFilePath)
                    });

                    await _largeFileSemaphore.WaitAsync(_cts.Token);// Le thread s'arrête ici tant qu'un autre gros fichier n'a pas fini
                }

                try
                {

                    var sw = Stopwatch.StartNew();

                    //ancienne logique 
                    ///File.Copy(files[i], dest, true);
                    ///


                    // On utilise Task.Run pour que la copie physique ne bloque pas le thread
                    await Task.Run(() => File.Copy(currentFilePath, dest, true), _cts.Token);

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

                }
                
                catch (OperationCanceledException)
                {
                    // L'utilisateur a appuyé sur STOP. 
                    // On sort de la boucle pour ce fichier.
                    return;
                }
                finally
                {
                    if (isLarge) _largeFileSemaphore.Release();
                }
            });
        }
        finally
        {
            // On ferme la connexion proprement si elle a été ouverte
            tcpLogger?.Dispose();
        }
    }
}
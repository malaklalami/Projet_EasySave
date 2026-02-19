using System;
using System.IO;
using System.Diagnostics;
using EasySave.Models;
using EasySave.Core;

namespace EasySave.Services;

public class BackupService
{
    private readonly ConfigService _config;
    private readonly LoggerService _logger = new();
    private readonly CryptoService _crypto;
    private readonly BusinessSoftwareMonitor _monitor;

    public BackupService(ConfigService config, CryptoService crypto)
    {
        _config = config;
        _crypto = crypto;
        _monitor = new BusinessSoftwareMonitor(_config, _logger);
    }

    public async Task Execute(BackupJob job, Action<BackupState> onProgress)
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
            _monitor.CheckActivity(job.Name, onProgress, true);

            var files = Directory.GetFiles(job.SourceDir, "*.*", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                _monitor.CheckActivity(job.Name, onProgress);

                string dest = files[i].Replace(job.SourceDir, job.TargetDir);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

                var sw = Stopwatch.StartNew();
                File.Copy(files[i], dest, true);

                long cryptTime = _config.Current.EncryptionExtensions.Contains(Path.GetExtension(dest).ToLower())
                    ? _crypto.Encrypt(dest)
                    : 0;

                sw.Stop();

                var entry = new LogEntry
                {
                    JobName = job.Name,
                    Source = files[i],
                    Target = dest,
                    FileSize = new FileInfo(dest).Length,
                    TransferTimeMs = sw.ElapsedMilliseconds,
                    EncryptionTimeMs = cryptTime
                };

                // --- STRATÉGIE DE LOGGING (Dev B) ---

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

                onProgress?.Invoke(new BackupState
                {
                    JobName = job.Name,
                    Status = JobState.Active,
                    Progress = (double)(i + 1) / files.Length * 100,
                    CurrentFile = files[i]
                });
            }
        }
        finally
        {
            // On ferme la connexion proprement si elle a été ouverte
            tcpLogger?.Dispose();
        }
    }
}
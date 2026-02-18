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

    public BackupService(ConfigService config, CryptoService crypto) { _config = config; _crypto = crypto; var tempLogger = new LoggerService(); _monitor = new BusinessSoftwareMonitor(_config, _logger); }

    public void Execute(BackupJob job, Action<BackupState> onProgress)

    {

        // VERIFICATION DE SECURITE (Avant tout calcul)
        // Si le logiciel métier est lancé, on bloque ici jusqu'à sa fermeture.
        _monitor.CheckActivity(job.Name, onProgress, true);

        // 2. PREPARATION (Seulement si la sécurité est OK)
        var files = Directory.GetFiles(job.SourceDir, "*.*", SearchOption.AllDirectories);

        for (int i = 0; i < files.Length; i++)

        {
            // RE-VERIFICATION (Au cas où il est ouvert pendant la copie)
            // Cette ligne va bloquer (pause) tant que le logiciel est ouvert
            _monitor.CheckActivity(job.Name, onProgress);


            string dest = files[i].Replace(job.SourceDir, job.TargetDir);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            var sw = Stopwatch.StartNew();
            File.Copy(files[i], dest, true);
            long cryptTime = _config.Current.EncryptionExtensions.Contains(Path.GetExtension(dest).ToLower()) ? _crypto.Encrypt(dest) : 0;
            sw.Stop();

            _logger.Write(new LogEntry { JobName = job.Name, Source = files[i], Target = dest, FileSize = new FileInfo(dest).Length, TransferTimeMs = sw.ElapsedMilliseconds, EncryptionTimeMs = cryptTime }, _config.Current.LogFormat == LogFormat.Json);

            onProgress?.Invoke(new BackupState { JobName = job.Name, Status = JobState.Active, Progress = (double)(i + 1) / files.Length * 100, CurrentFile = files[i] });
        }
    }
}

//Uniquement l'action de sauvegarde (la boucle de copie).
//Contient la boucle de copie Il vérifie le logiciel métier (via le cache du ConfigService), copie les fichiers, demande le chiffrement et déclenche les logs. Il ne connaît pas la vue
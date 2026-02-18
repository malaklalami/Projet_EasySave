using System.Diagnostics;
using EasySave.Models;
using EasySave.Core;

namespace EasySave.Services;

public class BusinessSoftwareMonitor
{
    private readonly ConfigService _config;
    private readonly LoggerService _logger;

    public BusinessSoftwareMonitor(ConfigService config, LoggerService logger)
    {
        _config = config;
        _logger = logger;
    }


    public void CheckActivity(string jobName, Action<BackupState> onProgress, bool isStarting = false)
    {
        string targetApp = _config.Current.BusinessSoftware;
        if (string.IsNullOrWhiteSpace(targetApp)) return;

        bool hasLoggedPause = false;

        while (Process.GetProcessesByName(targetApp).Length > 0)
        {
            if (!hasLoggedPause)
            {
                _logger.Write(new LogEntry
                {
                    JobName = jobName,
                    Source = "MONITOR",
                    // Si isStarting est vrai, on logge "START_BLOCK", sinon "EXEC_PAUSE"
                    Target = isStarting ? "START_BLOCK" : "EXEC_PAUSE",
                    FileSize = 0
                }, _config.Current.LogFormat == LogFormat.Json);
                hasLoggedPause = true;
            }

            onProgress?.Invoke(new BackupState
            {
                JobName = jobName,
                Status = JobState.Paused,
                // Ici on change le texte selon le moment
                CurrentFile = isStarting
                    ? $"En attente de fermeture de {targetApp} pour démarrer..."
                    : $"Sauvegarde suspendue : {targetApp} est ouvert."
            });

            Thread.Sleep(2000);
        }
    }
}

// Gère la suspension de la sauvegarde tant que le processus métier cible est détecté.
// Assure l'unicité de l'inscription dans les logs et la mise à jour en temps réel de l'état (BackupState) pour l'interface utilisateur.
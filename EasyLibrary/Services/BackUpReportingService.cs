using EasySave.Core;
using EasySave.Models;

namespace EasySave.Services;

public class BackupReportingService
{
    private readonly LoggerService _loggerService;
    private readonly StateService _stateService;
    private readonly ConfigService _configService;

    // Le constructeur prend les outils, et s'abonne au BackupService !
    public BackupReportingService(BackupService backupService, LoggerService loggerService, StateService stateService, ConfigService configService)
    {
        _loggerService = loggerService;
        _stateService = stateService;
        _configService = configService;

        // On branche nos oreilles sur le moteur
        backupService.OnFileCompleted += HandleFileCompleted;
        backupService.OnProgress += HandleProgress;
    }

    // Quand le BackupService crie "Fichier terminé" : on écrit le LOG
    private void HandleFileCompleted(TransferResult result)
    {
        _loggerService.Write(new LogEntry
        {
            JobName = result.JobName,
            Source = result.Source,
            Target = result.Dest,
            FileSize = result.Size,
            TransferTimeMs = result.TransferTimeMs,
            EncryptionTimeMs = result.EncryptionTimeMs
        }, _configService.Current.LogFormat == LogFormat.Json);
    }

    // Quand le BackupService crie "Voici ma progression" : on met à jour le STATE
    private void HandleProgress(BackupState currentState)
    {
        _stateService.UpdateState(currentState);
    }
}

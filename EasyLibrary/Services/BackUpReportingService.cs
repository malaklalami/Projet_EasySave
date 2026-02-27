using EasySave.Core;
using EasySave.Models;
using EasySave.Services;

namespace EasySave.Services;

public class BackupReportingService
{
    private readonly LoggerService _loggerService;
    private readonly StateService _stateService;
    private readonly ConfigService _configService;
    private readonly PersistentTcpLogger _tcpLogger = new();

    public BackupReportingService(BackupService backupService, LoggerService loggerService, StateService stateService, ConfigService configService)
    {
        _loggerService = loggerService;
        _stateService = stateService;
        _configService = configService;

       
        Task.Run(async () => await _tcpLogger.ConnectAsync(_configService.Current.RemoteIp));

        backupService.OnFileCompleted += HandleFileCompleted;
        backupService.OnProgress += HandleProgress;
    }

    private void HandleFileCompleted(TransferResult result)
    {
        var entry = new LogEntry
        {
            JobName = result.JobName,
            Source = result.Source,
            Target = result.Dest,
            FileSize = result.Size,
            TransferTimeMs = result.TransferTimeMs,
            EncryptionTimeMs = result.EncryptionTimeMs
        };

        var currentLogTarget = _configService.Current.LogTarget;

        // Sauvegarde locale (fichier XML ou JSON)
        if (currentLogTarget == LogTarget.Local || currentLogTarget == LogTarget.Both)
        {
            _loggerService.Write(entry, _configService.Current.LogFormat == LogFormat.Json);
        }

        // Envoi réseau vers le serveur ConsoleDeportee
        if (currentLogTarget == LogTarget.Remote || currentLogTarget == LogTarget.Both)
        {
            _tcpLogger.SendLog(entry);
        }
    }

    private void HandleProgress(BackupState currentState)
    {
        _stateService.UpdateState(currentState);
    }
}
using System.Diagnostics;
using System.Threading;
using EasySave.Models;
using EasySave.Core;

namespace EasySave.Services;

public class BusinessSoftwareMonitor
{
    private readonly ConfigService _config;

    public BusinessSoftwareMonitor(ConfigService config)
    {
        _config = config;
    }

    // Cette méthode sera appelée par un thread séparé (le Watcher) 
    // ou au début de chaque fichier dans le BackupService.
    public void UpdateControlState()
    {
        string targetApp = _config.Current.BusinessSoftware;
        if (string.IsNullOrWhiteSpace(targetApp)) return;

        // Si le logiciel est détecté
        if (Process.GetProcessesByName(targetApp).Length > 0)
        {
            // On force la pause dans le service de contrôle global
            JobControlService.IsPaused = true;
        }
        else
        {
            // On ne "libère" la pause que si l'utilisateur n'a pas appuyé 
            // manuellement sur Pause (optionnel, selon ta préférence)
            // Pour faire simple : si le logiciel ferme, on reprend.
            JobControlService.IsPaused = false;
        }
    }
}
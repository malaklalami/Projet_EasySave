using System.Diagnostics;
using System.Threading;
using EasySave.Models;
using EasySave.Core;

namespace EasySave.Services;

public class BusinessSoftwareMonitor
{
    private readonly ConfigService _config;

    public event Action<bool>? OnSoftwareDetectionChanged;

    // Ce flag permet de savoir si la pause actuelle vient du logiciel ou de l'utilisateur
    private bool _pauseTriggeredByBusinessSoftware = false;

    public BusinessSoftwareMonitor(ConfigService config)
    {
        _config = config;
    }

    // Cette méthode sera appelée par un thread séparé (le Watcher) 
    // ou au début de chaque fichier dans le BackupService.
    public void UpdateControlState(List<BackupJob> allJobs)
    {
        string targetApp = _config.Current.BusinessSoftware;
        if (string.IsNullOrWhiteSpace(targetApp)) return;

        // Suppression de l'extension .exe si l'utilisateur l'a mise (GetProcessesByName n'en veut pas)
        if (targetApp.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            targetApp = targetApp.Substring(0, targetApp.Length - 4);

        if (Process.GetProcessesByName(targetApp).Length > 0)
        {
            // On utilise la méthode qui met le "feu rouge"
            if (!JobControlService.IsPausedAll)
            {
                _pauseTriggeredByBusinessSoftware = true;
                JobControlService.PauseAll();
                OnSoftwareDetectionChanged?.Invoke(true);
            }
        }
        else
        {
            // On ne relance QUE si c'est le logiciel métier qui avait mis la pause
            // (Optionnel : si on veut que ça reprenne tout seul)
            if (JobControlService.IsPausedAll && _pauseTriggeredByBusinessSoftware)
            {
                // On passe la liste des jobs si on veut tout réveiller
                // Ou on laisse l'utilisateur cliquer sur Resume manuellement pour plus de sécurité.
                _pauseTriggeredByBusinessSoftware = false;
                JobControlService.ResumeAll(allJobs);
                OnSoftwareDetectionChanged?.Invoke(false);
            }
        }
    }
}
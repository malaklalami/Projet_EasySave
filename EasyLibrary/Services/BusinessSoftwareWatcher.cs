using System.Diagnostics;
using EasySave.Core;

namespace EasySave.Services;

public class BusinessSoftwareWatcher
{
    private readonly ConfigService _config;
    private bool _isRunning;

    // On utilise des "Actions" (delegates) pour appeler les méthodes du Développeur A
    public Action? OnSoftwareDetected { get; set; }
    public Action? OnSoftwareClosed { get; set; }

    public BusinessSoftwareWatcher(ConfigService config)
    {
        _config = config;
    }

    public void Start()
    {
        _isRunning = true;
        // On lance la surveillance dans un thread séparé pour ne pas bloquer l'UI
        Task.Run(MonitorLoop);
    }

    private async Task MonitorLoop()
    {
        bool wasRunning = false;

        while (_isRunning)
        {
            try
            {
                string targetApp = _config.Current.BusinessSoftware;

                if (!string.IsNullOrWhiteSpace(targetApp))
                {
                    bool isRunningNow = Process.GetProcessesByName(targetApp).Length > 0;

                    if (isRunningNow && !wasRunning)
                    {
                        wasRunning = true;
                        // On lance l'alerte dans une tâche séparée (Fire and Forget)
                        // Comme ça, si l'UI ouvre un popup, cette boucle ne freeze pas.
                        _ = Task.Run(() => OnSoftwareDetected?.Invoke());
                    }
                    else if (!isRunningNow && wasRunning)
                    {
                        wasRunning = false;
                        _ = Task.Run(() => OnSoftwareClosed?.Invoke());
                    }
                }
            }
            catch { /* Sécurité pour éviter de crash la boucle */ }

            await Task.Delay(1000);
        }
    }
}
// Surveille en arrière-plan la présence d'un logiciel métier pour suspendre les sauvegardes.
// Déclenche des alertes automatiques à l'ouverture ou à la fermeture de l'application cible.
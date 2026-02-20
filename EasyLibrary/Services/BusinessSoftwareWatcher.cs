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
            string targetApp = _config.Current.BusinessSoftware;

            if (!string.IsNullOrWhiteSpace(targetApp))
            {
                // On vérifie si le processus existe (sans .exe)
                bool isRunningNow = Process.GetProcessesByName(targetApp).Length > 0;

                if (isRunningNow && !wasRunning)
                {
                    // LE LOGICIEL VIENT D'OUVRIR -> ON DÉCLENCHE PAUSE()
                    OnSoftwareDetected?.Invoke();
                    wasRunning = true;
                }
                else if (!isRunningNow && wasRunning)
                {
                    // LE LOGICIEL VIENT DE FERMER -> ON DÉCLENCHE RESUME()
                    OnSoftwareClosed?.Invoke();
                    wasRunning = false;
                }
            }

            await Task.Delay(1000); // On vérifie toutes les secondes
        }
    }

    public void Stop() => _isRunning = false;
}
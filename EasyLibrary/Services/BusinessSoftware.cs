//BusinessSoftware.cs
using System;
using System.Diagnostics;
using System.Linq;
using EasyLibrary.Models;

namespace EasyLibrary.Services
{
    public class BusinessSoftware
    {
        private readonly SettingsJsonService _settingsService = new SettingsJsonService();

        // PARTIE 1 : VÉRIFICATION (Vérifie si le logiciel tourne)
        public bool IsRunning(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return false;

            // On récupère TOUS les processus
            var processes = Process.GetProcesses();

            // On cherche une correspondance EXACTE (plus robuste que Contains)
            return processes.Any(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        }

        // PARTIE 2 : MISE À JOUR (Sauvegarde le nouveau nom dans le JSON)
        public void UpdateBusinessSoftware(ConsoleSettingsJson settings, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) return;

            settings.BusinessSoftware = newName.Trim();
            _settingsService.Save(settings); // Sauvegarde persistante
        }
    }
}

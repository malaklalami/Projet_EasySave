using EasyLibrary.ViewModels;
using EasySave.Core;
using EasySave.Models;

namespace EasyConsole;

public static class SettingsUI
{
    public static void LogDestination(MainViewModel vm)
    {
        Console.Clear();
        Console.WriteLine("=== CONFIGURATION DE LA DESTINATION ===");
        // On affiche le mode actuel pour que l'utilisateur sache où il en est
        Console.WriteLine($"Mode actuel : {vm.Settings.LogStrategy}");
        Console.WriteLine("---------------------------------------");
        Console.WriteLine("1. Local (PC uniquement)");
        Console.WriteLine("2. Remote (Console Déportée uniquement)");
        Console.WriteLine("3. Both (Local + Console Déportée)");
        Console.Write("\nVotre choix : ");

        if (int.TryParse(Console.ReadLine(), out int choice))
        {
            // On met à jour l'Enum (0=Local, 1=Remote, 2=Both)
            vm.Settings.LogStrategy = (LogTarget)(choice - 1);

            // Si l'utilisateur veut du réseau, on lui demande l'IP
            if (vm.Settings.LogStrategy != LogTarget.Local)
            {
                Console.Write($"IP du serveur [{vm.Settings.RemoteIp}] : ");
                string ip = Console.ReadLine() ?? "";
                if (!string.IsNullOrWhiteSpace(ip)) vm.Settings.RemoteIp = ip;
            }

            // On demande au ViewModel de sauvegarder dans le JSON
            vm.SaveSettings();
            Console.WriteLine("\n[OK] Paramètres enregistrés.");
        }
        else
        {
            Console.WriteLine("\n[ERREUR] Choix invalide.");
        }

        Console.WriteLine("\nAppuyez sur une touche pour revenir au menu...");
        Console.ReadKey();
    }
}
// Fournit une interface console pour configurer la stratégie d'envoi des logs (Local, Réseau ou les deux).
// Permet de modifier l'adresse IP distante et sauvegarde automatiquement les changements dans les paramètres globaux.
using System;
using EasyLibrary.ViewModels;
using EasyConsole; // Adapte selon ton namespace réel de ConsoleView

namespace EasyConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            ConsoleView view = new ConsoleView();

            // 1. On charge les données silencieusement (pas de menu ici !)
            view.ViewModel.LoadData();

            // 2. On lie la vue pour que le service puisse envoyer des messages
            view.ViewModel.Vue = view;

            if (args.Length > 0)
            {
                // MODE LIGNE DE COMMANDE
                Console.WriteLine($"--- Mode Automatique : Exécution de {args[0]} ---");

                view.ViewModel.ExecuteJobsFromArgs(args[0]);

                Console.WriteLine("\n[SUCCÈS] Exécution terminée. Appuyez sur une touche pour quitter.");
                Console.ReadKey();
            }
            else
            {
                // MODE INTERACTIF
                // On n'appelle Start que si on n'a pas d'arguments
                view.ViewModel.Start();
            }
        }
    }
    
}
// - extraire dans une classe la gestion des arguments
// - améliorer le systeme de trad pour ne plus avoir de if / else
// - extraire le système d'interpretation de 0-3;4 dans une classe dédiée qui renvoie une liste de job
// - éviter de propager la vue aux services
// - virer les méthodes de JobService qui ne sont pas de la copie
// - virer la pluspart des méthodes add/update/delete job car on a besoin que de save la liste au niveau du controller
// - faire la validation d'un job dans le controller
// - Transformer log format en enum
// - transformer backup type en enum
// -  SettingsJsonService devrait proposer un cache et BusinessSoftware devrait s'en servir
// - bizarre jobs.json et state.json qui contiennent la meme structure
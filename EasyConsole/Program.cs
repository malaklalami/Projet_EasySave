using System;
using EasyLibrary.ViewModels;


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
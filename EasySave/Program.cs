using System;
using EasySave.View;
using EasySave.ViewModel;
using EasySave.Model; // Ajoute bien cet using pour accéder à BackUpJob

namespace EasySave
{
    class Program
    {
        static void Main(string[] args)
        {
            MainViewModel viewModel = new MainViewModel();

            // TRÈS IMPORTANT : On charge les jobs AVANT tout le reste
            viewModel.LoadJobsConfig();

            // --- DONNÉES DE TEST (À remplacer par LoadJobs plus tard) ---
            // On ajoute 3 jobs manuellement pour tester la ligne de commande
            //viewModel.Jobs.Add(new BackUpJob("Sauv1", @"/Users/malaklalami/desktop/a", @"/Users/malaklalami/desktop/save1", "Full"));
            //viewModel.Jobs.Add(new BackUpJob("Sauv2", @"/Users/malaklalami/desktop/a", @"/Users/malaklalami/desktop/save2", "Full"));
            //viewModel.Jobs.Add(new BackUpJob("Sauv3", @"/Users/malaklalami/desktop/a", @"/Users/malaklalami/desktop/save3", "Full"));

            if (args.Length > 0)
            {
                // MODE LIGNE DE COMMANDE (Automatique)
                HandleCommandLine(args[0], viewModel);

                // Petite pause pour avoir le temps de lire le résultat avant que le terminal se ferme
                Console.WriteLine("\nExécution terminée. Appuyez sur une touche pour quitter.");
                Console.ReadKey();
            }
            else
            {
                // MODE INTERACTIF (Menu console classique)
                ConsoleView view = new ConsoleView(viewModel);
                view.Start();
            }
        }

        private static void HandleCommandLine(string input, MainViewModel viewModel)
        {
            if (input.Contains("-"))
            {
                string[] range = input.Split('-');
                if (int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                {
                    for (int i = start; i <= end; i++)
                    {
                        ExecuteWithCheck(i, viewModel);
                    }
                }
            }
            else if (input.Contains(";"))
            {
                string[] list = input.Split(';');
                foreach (string item in list)
                {
                    if (int.TryParse(item, out int id))
                    {
                        ExecuteWithCheck(id, viewModel);
                    }
                }
            }
            else if (int.TryParse(input, out int id))
            {
                ExecuteWithCheck(id, viewModel);
            }
        }

        private static void ExecuteWithCheck(int id, MainViewModel viewModel)
        {
            int index = id - 1;

            if (index >= 0 && index < viewModel.Jobs.Count)
            {
                Console.WriteLine($"\n[CMD] Lancement du travail {id} : {viewModel.Jobs[index].Name}...");
                viewModel.ExecuteJob(index);
            }
            else
            {
                Console.WriteLine($"Erreur : Le travail n°{id} n'existe pas (il n'y a que {viewModel.Jobs.Count} travaux chargés).");
            }
        }
    }
}
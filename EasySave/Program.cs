using System;
using EasySave.View;
using EasySave.ViewModel;
using EasySave.Model;

namespace EasySave
{
    /// <summary>
    /// Classe servant à lancer le logiciel.
    /// Gère l'initialisation du ViewModel
    /// et le choix du mode d'execution ( execution d'un job ou execution sequentielle)
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Initialisation : Création du ViewModel unique
            MainViewModel viewModel = new MainViewModel();

            // Chargement : Récupération des jobs sauvegardés dans le fichier JSON s'il existe
            viewModel.LoadJobsConfig();

            // --- DONNÉES DE TEST (À remplacer par LoadJobs plus tard) ---
            // On ajoute 3 jobs manuellement pour tester la ligne de commande
            //viewModel.Jobs.Add(new BackUpJob("Sauv1", @"/Users/malaklalami/desktop/a", @"/Users/malaklalami/desktop/save1", "Full"));
            //viewModel.Jobs.Add(new BackUpJob("Sauv2", @"/Users/malaklalami/desktop/a", @"/Users/malaklalami/desktop/save2", "Full"));
            //viewModel.Jobs.Add(new BackUpJob("Sauv3", @"/Users/malaklalami/desktop/a", @"/Users/malaklalami/desktop/save3", "Full"));

            // Choix entre mode ligne de commande ou mode Interactif
            if (args.Length > 0)
            {
                // Mode ligne de commande : Si des arguments sont passés au lancement
                // ex : ./EasySave 1-3
                HandleCommandLine(args[0], viewModel);

                // Permet à l'utilisateur de lire le résultat et de quitter
                Console.WriteLine("\nExécution terminée. Appuyez sur une touche pour quitter.");
                Console.ReadKey();
            }
            else
            {
                // Mode interractif (Menu console classique définit dans View)
                ConsoleView view = new ConsoleView(viewModel);
                view.Start();
            }
        }


        /// <summary>
        /// Méthode pour analyser l'argument reçu en ligne de commande pour lancer les jobs correspondants.
        /// Gère le format unique (ex : 1), le format ( 1-3) et (1;3)
        /// </summary>
        /// <param name="input">La saisie ( ex : 1 ou 1-3)</param>
        /// <param name="viewModel">L'instance du ViewModel pour accèder à la liste des jobs.</param>
        private static void HandleCommandLine(string input, MainViewModel viewModel)
        {
            // Cas 1 : Intervalle de travaux à executer ( ex : "1-3")
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
            // Cas 2 : Liste de travaux ( ex : "1;3)
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
            // Cas 3 : Travail unique
            else if (int.TryParse(input, out int id))
            {
                ExecuteWithCheck(id, viewModel);
            }
        }

        /// <summary>
        /// Méthode qui vérifie l'existence d'un job avant de lancer son exécution.
        /// </summary>
        /// <param name="index">L'identifiant du travail saisi par l'utlisateur</param>
        /// <param name="viewModel">Le ViewModel contenant la liste des travaux</param>
        private static void ExecuteWithCheck(int index, MainViewModel viewModel)
        {
            // On vérifie que l'index est bien compris entre 0 et le nombre de job

            if (index >= 0 && index < viewModel.Jobs.Count)
            {
                Console.WriteLine($"\n[CMD] Lancement du travail {index} : {viewModel.Jobs[index].Name}...");
                viewModel.ExecuteJob(index);
            }
            else
            {
                Console.WriteLine($"Erreur : Le travail n°{index} n'existe pas (il n'y a que {viewModel.Jobs.Count} travaux chargés).");
            }
        }
    }
}
using System;
using System.IO;
using EasySave.ViewModel;


namespace EasySave.View
{
    public class ConsoleView
    {
        private MainViewModel _viewModel;
        public ConsoleView(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void Start()
        {
            bool exit = false;
            while (!exit)
            {
                // On affiche le menu selon la langue choisie dans le VM
                ShowMenu();

                string choice = Console.ReadLine()!;
                switch (choice)
                {
                    case "1":
                        // Appelle une méthode pour saisir les infos du Job
                        CreateJobUI();
                        break;
                    case "2":
                        // Appelle une méthode pour choisir quel Job lancer
                        ExecuteJobUI();
                        break;
                    case "3":
                        _viewModel.SwitchLanguage();
                        Console.WriteLine(_viewModel.CurrentLanguage == "fr" ? "Langue changée !" : "Language changed!");
                        break;
                    case "4":
                        // On appelle la méthode de nettoyage du ViewModel
                        _viewModel.ClearAllJobs();

                        // Petit message de confirmation traduit
                        if (_viewModel.CurrentLanguage == "fr")
                            Console.WriteLine("Configuration supprimée. Au revoir !");
                        else
                            Console.WriteLine("Configuration deleted. Goodbye!");

                        exit = true; // On sort de la boucle pour fermer le programme
                        break;
                    case "q":
                        exit = true;
                        break;
                }
            }
        }
        private void CreateJobUI()
        {
            bool isFr = (_viewModel.CurrentLanguage == "fr");
            Console.WriteLine(isFr ? "\n--- Création d'un travail ---" : "\n--- Create a Backup Job ---");

            // 1. NOM
            Console.Write(isFr ? "Nom du travail : " : "Job Name: ");
            string name = Console.ReadLine()!;

            // 2. SOURCE (Doit exister physiquement)
            Console.Write(isFr ? "Chemin source : " : "Source Path: ");
            string source = Console.ReadLine()!;
            while (string.IsNullOrWhiteSpace(source) || !System.IO.Directory.Exists(source))
            {
                Console.WriteLine(isFr ? "Erreur : Ce dossier n'existe pas." : "Error: This directory does not exist.");
                Console.Write(isFr ? "Entrez un chemin source valide : " : "Enter a valid source path: ");
                source = Console.ReadLine()!;
            }

            // 3. DESTINATION (Doit avoir un format valide et un lecteur existant)
            Console.Write(isFr ? "Chemin destination : " : "Target Path: ");
            string target = Console.ReadLine()!;
            bool isTargetValid = false;

            while (!isTargetValid)
            {
                try
                {
                    string? root = System.IO.Path.GetPathRoot(target);
                    // Vérifie : Pas vide ET Chemin complet existe
                    if (!string.IsNullOrWhiteSpace(target) &&
                        System.IO.Path.IsPathRooted(target) &&
                        System.IO.Directory.Exists(root))
                    {
                        isTargetValid = true;
                    }
                    else
                    {
                        Console.WriteLine(isFr
                            ? "Erreur : La destination doit être un chemin complet sur un lecteur existant."
                            : "Error: Destination must be a full path on an existing drive.");
                        Console.Write(isFr ? "Destination : " : "Target: ");
                        target = Console.ReadLine()!;
                    }
                }
                catch
                {
                    Console.WriteLine(isFr ? "Erreur : Format de chemin invalide." : "Error: Invalid path format.");
                    Console.Write(isFr ? "Destination : " : "Target: ");
                    target = Console.ReadLine()!;
                }
            }

            // 4. TYPE
            Console.Write(isFr ? "Type (Complet/Différentiel) : " : "Type (Full/Differential): ");
            string type = Console.ReadLine()!;

            // 5. ENVOI AU VIEWMODEL
            _viewModel.AddJob(name, source, target, type);
            Console.WriteLine(isFr ? "Travail ajouté avec succès !" : "Job added successfully!");
        }
        private void ExecuteJobUI()
        {
            if (_viewModel.CurrentLanguage == "fr")
            {
                Console.Write("Entrez le numéro du travail à lancer (0 à 4) : ");
            }
            else
            {
                Console.Write("Enter the job number to run (0 to 4): ");
            }

            string input = Console.ReadLine()!;
            if (int.TryParse(input, out int index))
            {
                // On vérifie si l'index est valide (0-4) et si le job existe
                if (index >= 0 && index < _viewModel.Jobs.Count)
                {
                    // C'EST ICI QUE LA VIEW APPELLE LE VIEWMODEL
                    _viewModel.ExecuteJob(index);

                    Console.WriteLine(_viewModel.CurrentLanguage == "fr" ? "Exécution terminée." : "Execution finished.");
                }
                else
                {
                    Console.WriteLine("Index invalide / Invalid index.");
                }
            }
        }
        private void ShowJobsList()
        {
            Console.WriteLine(_viewModel.CurrentLanguage == "fr" ? "\n--- Liste des Travaux ---" : "\n--- Jobs List ---");

            if (_viewModel.Jobs.Count == 0)
            {
                Console.WriteLine(_viewModel.CurrentLanguage == "fr" ? "Aucun travail configuré." : "No jobs configured.");
                return;
            }

            for (int i = 0; i < _viewModel.Jobs.Count; i++)
            {
                var job = _viewModel.Jobs[i];
                // On affiche l'index, le nom et l'état
                Console.WriteLine($"[{i}] Nom: {job.Name} | Source: {job.SourceDir}");
            }
        }

        public void ShowMenu()
        {
            // On affiche la liste des travaux en haut du menu
            ShowJobsList();
            if (_viewModel.CurrentLanguage == "fr")
            {
                Console.WriteLine("\n--- Menu EasySave ---");
                Console.WriteLine("1. Créer un travail de sauvegarde");
                Console.WriteLine("2. Lancer une sauvegarde");
                Console.WriteLine("3. Changer la langue");
                Console.WriteLine("4. Nettoyer tout et Quitter");
                Console.WriteLine("q. Quitter");
            }
            else
            {
                Console.WriteLine("\n--- EasySave Menu ---");
                Console.WriteLine("1. Create a backup job");
                Console.WriteLine("2. Run a backup");
                Console.WriteLine("3. Switch language");
                Console.WriteLine("4. Clean all and Quit");
                Console.WriteLine("q. Quit");
            }
        }
    }
}



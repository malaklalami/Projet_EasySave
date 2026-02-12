using System;
using EasyLibrary.Models;
using EasySave.ViewModel;


namespace EasySave.View
{
    public class ConsoleView : IVue
    {
        public MainViewModel ViewModel { get; set; }

        public ConsoleView()
        {
            ViewModel = new MainViewModel();
            ViewModel.Vue = this;
        }

        public void JobExecutionError(BackUpJob job)
        {
            Console.WriteLine(ViewModel.CurrentLanguage == "fr"
                        ? $"[ERREUR] Le dossier source n'existe pas : {job.SourceDir}"
                        : $"[ERROR] Source directory not found: {job.SourceDir}");
        }

        public void JobExecutionError(BackUpJob job, Exception ex)
        {
            // Tes messages d'erreurs personnalisés sont conservés ici
            if (ViewModel.CurrentLanguage == "fr")
            {
                Console.WriteLine($"[ERREUR] Impossible d'exécuter {job.Name} Le chemin n'est pas valide ou vous tentez d'écrire dans une zone protégée");
                Console.WriteLine($"Détails techniques : {ex.Message}");
            }
            else
            {
                Console.WriteLine($"[ERROR] Could not execute {job.Name} Invalid path or attempt to write in a protected area.");
                Console.WriteLine($"Technical details : {ex.Message}");
            }
        }

        public void JobSucceeded(BackUpJob job)
        {
            Console.WriteLine(ViewModel.CurrentLanguage == "fr"
                     ? $"Succès : {job.Name} terminé."
                     : $"Success: {job.Name} finished.");

        }

        public void MaximumJobLimitReached()
        {
            Console.WriteLine(ViewModel.CurrentLanguage == "fr"
                   ? "[ERREUR] Limite de 5 travaux atteinte."
                   : "[ERROR] Limit of 5 jobs reached.");
        }

        private void CreateJobUI()
        {
            bool isFr = (ViewModel.CurrentLanguage == "fr");
            Console.WriteLine(isFr ? "\n--- Création d'un travail ---" : "\n--- Create a Backup Job ---");

            // 1. NOM
            Console.Write(isFr ? "Nom du travail : " : "Job Name: ");
            string name = Console.ReadLine();

            // 2. SOURCE (Doit exister physiquement)
            Console.Write(isFr ? "Chemin source : " : "Source Path: ");
            string source = Console.ReadLine();
            while (string.IsNullOrWhiteSpace(source) || !System.IO.Directory.Exists(source))
            {
                Console.WriteLine(isFr ? "Erreur : Ce dossier n'existe pas." : "Error: This directory does not exist.");
                Console.Write(isFr ? "Entrez un chemin source valide : " : "Enter a valid source path: ");
                source = Console.ReadLine();
            }

            // 3. DESTINATION (Doit avoir un format valide et un lecteur existant)
            Console.Write(isFr ? "Chemin destination : " : "Target Path: ");
            string target = Console.ReadLine();
            bool isTargetValid = false;

            while (!isTargetValid)
            {
                try
                {
                    string root = System.IO.Path.GetPathRoot(target);
                    // Vérifie : Pas vide ET Chemin complet ET Le disque (C:, D:, etc.) existe
                    if (!string.IsNullOrWhiteSpace(target) &&
                        System.IO.Path.IsPathRooted(target) &&
                        System.IO.Directory.Exists(root))
                    {
                        isTargetValid = true;
                    }
                    else
                    {
                        Console.WriteLine(isFr
                            ? "Erreur : La destination doit être un chemin complet sur un lecteur existant (ex: C:\\Save)."
                            : "Error: Destination must be a full path on an existing drive (e.g., C:\\Save).");
                        Console.Write(isFr ? "Destination : " : "Target: ");
                        target = Console.ReadLine();
                    }
                }
                catch
                {
                    Console.WriteLine(isFr ? "Erreur : Format de chemin invalide." : "Error: Invalid path format.");
                    Console.Write(isFr ? "Destination : " : "Target: ");
                    target = Console.ReadLine();
                }
            }

            // 4. TYPE
            Console.Write(isFr ? "Type (Complet/Différentiel) : " : "Type (Full/Differential): ");
            string type = Console.ReadLine();

            // 5. ENVOI AU VIEWMODEL
            ViewModel.AddJob(name, source, target, type);
            Console.WriteLine(isFr ? "Travail ajouté avec succès !" : "Job added successfully!");
        }

        private void ExecuteJobUI()
        {
            if (ViewModel.CurrentLanguage == "fr")
            {
                Console.Write("Entrez le numéro du travail à lancer (0 à 4) : ");
            }
            else
            {
                Console.Write("Enter the job number to run (0 to 4): ");
            }

            string input = Console.ReadLine();
            if (int.TryParse(input, out int index))
            {
                // On vérifie si l'index est valide (0-4) et si le job existe
                if (index >= 0 && index < ViewModel.Jobs.Count)
                {
                    // C'EST ICI QUE LA VIEW APPELLE LE VIEWMODEL
                    ViewModel.ExecuteJob(index);

                    Console.WriteLine(ViewModel.CurrentLanguage == "fr" ? "Exécution terminée." : "Execution finished.");
                }
                else
                {
                    Console.WriteLine("Index invalide / Invalid index.");
                }
            }
        }

        private void ShowJobsList()
        {
            Console.WriteLine(ViewModel.CurrentLanguage == "fr" ? "\n--- Liste des Travaux ---" : "\n--- Jobs List ---");

            if (ViewModel.Jobs.Count == 0)
            {
                Console.WriteLine(ViewModel.CurrentLanguage == "fr" ? "Aucun travail configuré." : "No jobs configured.");
                return;
            }

            for (int i = 0; i < ViewModel.Jobs.Count; i++)
            {
                var job = ViewModel.Jobs[i];
                // On affiche l'index, le nom et l'état
                Console.WriteLine($"[{i}] Nom: {job.Name} | Source: {job.SourceDir}");
            }
        }

        private void ShowMenu()
        {
            // On affiche la liste des travaux en haut du menu
            ShowJobsList();
            if (ViewModel.CurrentLanguage == "fr")
            {
                Console.WriteLine("\n--- Menu EasySave ---");
                Console.WriteLine("1. Créer un travail de sauvegarde");
                Console.WriteLine("2. Lancer une sauvegarde");
                Console.WriteLine("3. Changer la langue");
                Console.WriteLine("4. Changer le format des logs (Actuel : " + ViewModel.CurrentLogFormat + ")");
                Console.WriteLine("5. Effacer tous les travaux");
                Console.WriteLine("q. Quitter");
            }
            else
            {
                Console.WriteLine("\n--- EasySave Menu ---");
                Console.WriteLine("1. Create a backup job");
                Console.WriteLine("2. Run a backup");
                Console.WriteLine("3. Switch language");
                Console.WriteLine("4. Change log format (Current : " + ViewModel.CurrentLogFormat + ")");
                Console.WriteLine("5. Clear all jobs");
                Console.WriteLine("q. Quit");
            }
        }

        public void AfficheMenuPrincipal()
        {
            /*
           Menu m = ...;
           m.add("option A", ()=> { });
           m.choisis();
           */
            bool exit = false;
            while (!exit)
            {
                // On affiche le menu selon la langue choisie dans le VM
                ShowMenu();

                string choice = Console.ReadLine();
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
                        ViewModel.SwitchLanguage();
                        Console.WriteLine(ViewModel.CurrentLanguage == "fr" ? "Langue changée !" : "Language changed!");
                        break;
                    case "4":
                        ViewModel.SwitchLogFormat();
                        Console.WriteLine(ViewModel.CurrentLanguage == "fr"
                            ? "Format des logs changé : " + ViewModel.CurrentLogFormat
                            : "Log format changed : " + ViewModel.CurrentLogFormat);
                        break;
                    case "5":
                        ViewModel.ClearAllJobs();
                        Console.WriteLine(ViewModel.CurrentLanguage == "fr" ? ">>>> Tous les travaux ont été supprimés." : ">>>> All jobs have been deleted.");
                        break;
                    case "q":
                        exit = true;
                        break;
                }
            }

        }
    }
}



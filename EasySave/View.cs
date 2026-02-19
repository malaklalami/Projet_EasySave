using System;
using System.IO;
using EasySave.ViewModel;


namespace EasySave.View
{
    /// <summary>
    /// Classe qui gère l'interface utilisateur en mode console.
    /// Elle gère l'affichage et les saisies 
    /// et les communique au ViewModel pour traiter les données.
    /// </summary>
    public class ConsoleView
    {
        // Reference vers le ViewModel 
        private MainViewModel _viewModel;


        /// <summary>
        /// Constructeur de la vue.
        /// On reçoit le ViewModel par injection
        /// pour permettre la communication entre l'interface et la logique
        /// </summary>
        /// <param name="viewModel">L'instance du ViewModel principal de l'application</param>
        public ConsoleView(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        /// <summary>
        /// Méthode qui est la boucle principale qui permet de lancer l'interface.
        /// Elle gère l'affichage et dirige dans d'autres menus en fonction des choix utilisateur.
        /// </summary>
        public void Start()
        {
            bool exit = false;
            while (!exit)
            {
                // On affiche le menu selon la langue choisie, par défaut au démarrage,fr
                ShowMenu();

                string choice = Console.ReadLine()!;
                switch (choice)
                {
                    case "1":
                        // Appelle une méthode pour saisir les infos du Job et le créer
                        CreateJobUI();
                        break;
                    case "2":
                        // Appelle une méthode pour choisir quel Job lancer
                        ExecuteJobUI();
                        break;
                    case "3":
                        // Changement de langue (Français / Anglais)
                        _viewModel.SwitchLanguage();
                        Console.WriteLine(_viewModel.CurrentLanguage == "fr" ? "Langue changée !" : "Language changed!");
                        break;
                    case "4":
                        // On appelle la méthode de nettoyage du ViewModel
                        _viewModel.ClearAllJobs();

                        // Message de confirmation de suppression
                        if (_viewModel.CurrentLanguage == "fr")
                            Console.WriteLine("Configuration supprimée. Au revoir !");
                        else
                            Console.WriteLine("Configuration deleted. Goodbye!");

                        exit = true; // On sort de la boucle pour fermer le programme
                        break;
                    case "q": // on quitte sans faire de clean, si on a créé des jobs
                              // ils seront toujours dans la liste lors du prochain lancement
                        exit = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Méthode qui gère l'interface de saisie pour la création d'un travail
        /// Contient une vérification de la cohérence des chemins sources et destinations.
        /// </summary>
        private void CreateJobUI()
        {
            bool isFr = (_viewModel.CurrentLanguage == "fr");
            Console.WriteLine(isFr ? "\n--- Création d'un travail ---" : "\n--- Create a Backup Job ---");

            // 1. Saisie du nom
            Console.Write(isFr ? "Nom du travail : " : "Job Name: ");
            string name = Console.ReadLine()!;

            // 2. Saisie de la source (Doit exister physiquement)
            Console.Write(isFr ? "Chemin source : " : "Source Path: ");
            string source = Console.ReadLine()!;
            while (string.IsNullOrWhiteSpace(source) || !System.IO.Directory.Exists(source))
            {
                Console.WriteLine(isFr ? "Erreur : Ce dossier n'existe pas." : "Error: This directory does not exist.");
                Console.Write(isFr ? "Entrez un chemin source valide : " : "Enter a valid source path: ");
                source = Console.ReadLine()!;
            }

            // 3. Saisie de la destination (doit avoir un format valide et un lecteur existant)
            Console.Write(isFr ? "Chemin destination : " : "Target Path: ");
            string target = Console.ReadLine()!;
            bool isTargetValid = false;

            // Si c'est valide
            while (!isTargetValid)
            {
                try
                {   // On vérifie que root existe ( ex Users/)
                    string? root = System.IO.Path.GetPathRoot(target);
                    // Vérifiation : Pas vide ET Chemin complet existe
                    if (!string.IsNullOrWhiteSpace(target) &&
                        System.IO.Path.IsPathRooted(target) &&
                        System.IO.Directory.Exists(root))
                    {
                        isTargetValid = true;
                    }
                    else
                    // Gestion des erreurs
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

            // 4. Saisie du type
            Console.Write(isFr ? "Type (Complet/Différentiel) : " : "Type (Full/Differential): ");
            string type = Console.ReadLine()!;

            // 5. Envoi au ViewModel pour traitement et enregistement
            _viewModel.AddJob(name, source, target, type);
            Console.WriteLine(isFr ? "Travail ajouté avec succès !" : "Job added successfully!");
        }

        /// <summary>
        /// Méthode pour gérer l'interface de lancement de sauvegarde
        /// </summary>
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

            // Sécurisation de la saisie,pour ne pas avoir de crash au cas où l'utilsateur tape des lettres
            if (int.TryParse(input, out int index))
            {
                // On vérifie si l'index est valide (0-4) et si le job existe
                if (index >= 0 && index < _viewModel.Jobs.Count)
                {
                    // Lancement de la logique de copie du ViewModel
                    _viewModel.ExecuteJob(index);

                    Console.WriteLine(_viewModel.CurrentLanguage == "fr" ? "Exécution terminée." : "Execution finished.");
                }
                else
                {
                    Console.WriteLine("Index invalide / Invalid index.");
                }
            }
        }

        /// <summary>
        /// Méthode pour afficher la liste des jobs créés
        /// </summary>
        private void ShowJobsList()
        {
            Console.WriteLine(_viewModel.CurrentLanguage == "fr" ? "\n--- Liste des Travaux ---" : "\n--- Jobs List ---");

            if (_viewModel.Jobs.Count == 0)
            {
                Console.WriteLine(_viewModel.CurrentLanguage == "fr" ? "Aucun travail configuré." : "No jobs configured.");
                return;
            }
            // Parcours de la liste du ViewModel
            for (int i = 0; i < _viewModel.Jobs.Count; i++)
            {
                var job = _viewModel.Jobs[i];
                // On affiche l'index, le nom et la source
                Console.WriteLine($"[{i}] Nom: {job.Name} | Source: {job.SourceDir}");
            }
        }

        /// <summary>
        /// Méthode pour afficher les options du menu principal.
        /// Une structure condtionnelle permet d'avoir le menu en anglais ou en français
        /// </summary>
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



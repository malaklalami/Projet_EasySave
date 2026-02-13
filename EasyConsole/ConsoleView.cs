using System;
using EasyLibrary.Models;
using EasyLibrary.ViewModels;


namespace EasyConsole
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
            Console.WriteLine(ViewModel.CurrentSettings.Language == "fr"
                        ? $"[ERREUR] Le dossier source n'existe pas : {job.SourceDir}"
                        : $"[ERROR] Source directory not found: {job.SourceDir}");
        }

        public void JobExecutionError(BackUpJob job, Exception ex)
        {
            // Tes messages d'erreurs personnalisés sont conservés ici
            if (ViewModel.CurrentSettings.Language == "fr")
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
            Console.WriteLine(ViewModel.CurrentSettings.Language == "fr"
                     ? $"Succès : {job.Name} terminé."
                     : $"Success: {job.Name} finished.");

        }

        //public void MaximumJobLimitReached()
        //{
        //    Console.WriteLine(ViewModel.CurrentSettings.Language == "fr"
        //           ? "[ERREUR] Limite de 5 travaux atteinte."
        //           : "[ERROR] Limit of 5 jobs reached.");
        //}

        private void CreateJobUI()
        {
            bool isFr = (ViewModel.CurrentSettings.Language == "fr");
            Console.WriteLine(isFr ? "\n--- Création d'un travail ---" : "\n--- Create a Backup Job ---");

            Console.Write(isFr ? "Nom du travail : " : "Job Name: ");
            string name = Console.ReadLine() ?? "";

            // 2. SOURCE (Doit exister physiquement)
            Console.Write(isFr ? "Chemin source : " : "Source Path: ");
            string source = Console.ReadLine() ?? "";
            while (string.IsNullOrWhiteSpace(source) || !System.IO.Directory.Exists(source))
            {
                Console.WriteLine(isFr ? "Erreur : Ce dossier n'existe pas." : "Error: This directory does not exist.");
                Console.Write(isFr ? "Entrez un chemin source valide : " : "Enter a valid source path: ");
                source = Console.ReadLine() ?? "";
            }

            // 3. DESTINATION (Doit avoir un format valide et un lecteur existant)
            Console.Write(isFr ? "Chemin destination : " : "Target Path: ");
            string target = Console.ReadLine() ?? "";
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
                        target = Console.ReadLine() ?? "";
                    }
                }
                catch
                {
                    Console.WriteLine(isFr ? "Erreur : Format de chemin invalide." : "Error: Invalid path format.");
                    Console.Write(isFr ? "Destination : " : "Target: ");
                    target = Console.ReadLine() ?? "";
                }
            }

            // 4. TYPE
            Console.Write(isFr ? "Type (Complet/Différentiel) : " : "Type (Full/Differential): ");
            string type = Console.ReadLine() ?? "";

            // 5. ENVOI AU VIEWMODEL
            ViewModel.AddJob(name, source, target, type);
            Console.WriteLine(isFr ? "Travail ajouté avec succès !" : "Job added successfully!");
        }

        private void ExecuteJobUI()
        {
            bool isFr = (ViewModel.CurrentSettings.Language == "fr");

            // 1. Message d'instruction dynamique
            if (isFr)
            {
                Console.WriteLine("\n--- Lancer une sauvegarde ---");
                Console.Write("Entrez le(s) numéro(s) (ex: 1, 0-3, 1;3) : ");
            }
            else
            {
                Console.WriteLine("\n--- Run a backup ---");
                Console.Write("Enter number(s) (ex: 1, 0-3, 1;3) : ");
            }

            // 2. On récupère la saisie brute (ex: "0-2")
            string input = Console.ReadLine() ?? "";

            if (!string.IsNullOrWhiteSpace(input))
            {
                // 3. On appelle la même logique que la ligne de commande !
                ViewModel.ExecuteJobsFromArgs(input);

                Console.WriteLine(isFr ? "\nExécution terminée." : "\nExecution finished.");
            }
        }

        private void EditJobUI()
        {
            bool isFr = (ViewModel.CurrentSettings.Language == "fr");
            ShowJobsList();

            Console.Write(isFr ? "\nNuméro du travail à modifier : " : "\nJob number to edit: ");
            if (!int.TryParse(Console.ReadLine() ?? "", out int index) || index < 0 || index >= ViewModel.Jobs.Count)
            {
                Console.WriteLine(isFr ? " Index invalide." : " Invalid index.");
                return;
            }

            var job = ViewModel.Jobs[index];
            string oldName = job.Name;

            Console.WriteLine(isFr ? "--- Laissez vide pour garder la valeur actuelle ---" : "--- Leave empty to keep current value ---");

            // NOM
            Console.Write(isFr ? $"Nom [{job.Name}] : " : $"Name [{job.Name}]: ");
            string name = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(name)) name = job.Name;

            // SOURCE
            Console.Write(isFr ? $"Source [{job.SourceDir}] : " : $"Source [{job.SourceDir}]: ");
            string source = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(source)) source = job.SourceDir;

            // DESTINATION
            Console.Write(isFr ? $"Destination [{job.TargetDir}] : " : $"Target [{job.TargetDir}]: ");
            string target = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(target)) target = job.TargetDir;

            // --- LOGIQUE DE TRADUCTION POUR LE TYPE ---
            // On prépare l'affichage dynamique du type actuel selon la langue
            string displayType = job.BackUpType;
            if (!isFr) // Si on est en Anglais
            {
                if (job.BackUpType == "Complet") displayType = "Full";
                else if (job.BackUpType == "Différentiel") displayType = "Differential";
            }
            else // Si on est en Français
            {
                if (job.BackUpType == "Full") displayType = "Complet";
                else if (job.BackUpType == "Differential") displayType = "Différentiel";
            }

            // TYPE (On affiche displayType entre crochets, mais on garde job.BackUpType en mémoire)
            Console.Write(isFr ? $"Type (Complet/Différentiel) [{displayType}] : " : $"Type (Full/Differential) [{displayType}]: ");
            string type = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(type)) type = job.BackUpType;

            // Mise à jour via le ViewModel
            ViewModel.EditJob(index, name, source, target, type);

            Console.WriteLine(isFr
                ? $" Le travail n°{index} ({oldName}) a été mis à jour !"
                : $" Job #{index} ({oldName}) has been updated!");
        }

        private void DeleteJobUI()
        {
            bool isFr = (ViewModel.CurrentSettings.Language == "fr");
            ShowJobsList(); // On montre la liste pour que l'utilisateur voie les index

            Console.Write(isFr ? "\nEntrez le numéro du travail à supprimer : " : "\nEnter the job number to delete: ");

            if (int.TryParse(Console.ReadLine() ?? "", out int index))
            {
                if (index >= 0 && index < ViewModel.Jobs.Count)
                {
                    string jobName = ViewModel.Jobs[index].Name;
                    ViewModel.DeleteJob(index);
                    if (isFr)
                        Console.WriteLine($" Le travail n°{index} ({jobName}) a été supprimé avec succès !");
                    else
                        Console.WriteLine($" Job #{index} ({jobName}) has been successfully deleted!");
                }
                else
                {
                    Console.WriteLine(isFr ? " Index invalide." : " Invalid index.");
                }
            }
        }

        private void ShowJobsList()
        {
            Console.WriteLine(ViewModel.CurrentSettings.Language == "fr" ? "\n--- Liste des Travaux ---" : "\n--- Jobs List ---");

            if (ViewModel.Jobs.Count == 0)
            {
                Console.WriteLine(ViewModel.CurrentSettings.Language == "fr" ? "Aucun travail configuré." : "No jobs configured.");
                return;
            }

            for (int i = 0; i < ViewModel.Jobs.Count; i++)
            {
                var job = ViewModel.Jobs[i];
                string labelName = ViewModel.CurrentSettings.Language == "fr" ? "Nom" : "Name";
                string labelSource = ViewModel.CurrentSettings.Language == "fr" ? "Source" : "Source";
                // On affiche l'index, le nom et l'état
                Console.WriteLine($"[{i}] {labelName}: {job.Name} | {labelSource}: {job.SourceDir}");
            }
        }

        public void ShowMenu()
        {
            // On affiche la liste des travaux en haut du menu
            ShowJobsList();
            if (ViewModel.CurrentSettings.Language == "fr")
                {
                Console.WriteLine("\n--- Menu EasySave ---");
                Console.WriteLine("1. Créer un travail de sauvegarde");
                Console.WriteLine("2. Lancer une ou plusieurs sauvegardes (ex: 1 ou 1;3 ou 0-4)");
                Console.WriteLine("3. Changer la langue");
                Console.WriteLine("4. Changer le format des logs (Actuel : " + ViewModel.CurrentSettings.LogFormat + ")");
                Console.WriteLine("5. Modifier un travail de sauvegarde");
                Console.WriteLine("6. Supprimer un travail de sauvegarde");
                Console.WriteLine("7. Effacer tous les travaux");
                Console.WriteLine("8. Configurer les extensions à chiffrer (Actuel : " +string.Join(", ", ViewModel.CurrentSettings.EncryptionExtensions) + ")");
                Console.WriteLine("9. Configurer le logiciel métier (Actuel : " + ViewModel.CurrentSettings.BusinessSoftware + ")");
                Console.WriteLine("q. Quitter");
            }
            else
            {
                Console.WriteLine("\n--- EasySave Menu ---");
                Console.WriteLine("1. Create a backup job");
                Console.WriteLine("2. Run one or more backups (e.g.,1 or 1;3 or 0-4)");
                Console.WriteLine("3. Switch language");
                Console.WriteLine("4. Change log format (Current : " + ViewModel.CurrentSettings.LogFormat + ")");
                Console.WriteLine("5. Edit a backup job");
                Console.WriteLine("6. Delete one backupjob");
                Console.WriteLine("7. Clear all jobs");
                Console.WriteLine("8. Configure encryption extensions (Current : " + string.Join(", ", ViewModel.CurrentSettings.EncryptionExtensions) + ")");
                Console.WriteLine("9. Configure business software (Current : " + ViewModel.CurrentSettings.BusinessSoftware + ")");
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

                string choice = Console.ReadLine() ?? "";
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
                        Console.WriteLine(ViewModel.CurrentSettings.Language == "fr" ? "Langue changée !" : "Language changed!");
                        break;
                    case "4":
                        ViewModel.SwitchLogFormat();
                        Console.WriteLine(ViewModel.CurrentSettings.Language == "fr"
                            ? "Format des logs changé : " + ViewModel.CurrentSettings.LogFormat
                            : "Log format changed : " + ViewModel.CurrentSettings.LogFormat);
                        break;
                    case "5":
                        EditJobUI();
                        break;
                    case "6": DeleteJobUI(); break;
                    case "7":
                        ViewModel.ClearAllJobs();
                        Console.WriteLine(ViewModel.CurrentSettings.Language == "fr" ? ">>>> Tous les travaux ont été supprimés." : ">>>> All jobs have been deleted.");
                        break;
                    case "8": // Nouvelle option
                        Console.WriteLine("Entrez l'extension à chiffrer (ex: .txt, .pdf) :");
                        string ext = Console.ReadLine() ?? "";
                        ViewModel.AddEncryptionExtension(ext);
                        Console.WriteLine("Extension ajoutée avec succès !");
                        break;
                    case "9":
                        Console.Clear();
                        // Gestion de la langue pour l'affichage
                        if (ViewModel.CurrentSettings.Language == "fr")
                        {
                            Console.WriteLine("--- Paramètres du Logiciel Métier ---");
                            Console.WriteLine($"Logiciel actuellement surveillé : {ViewModel.CurrentSettings.BusinessSoftware}");
                            Console.WriteLine("Entrez le nouveau nom du processus (ex: Calculator) ou Entrée pour annuler :");
                        }
                        else
                        {
                            Console.WriteLine("--- Business Software Settings ---");
                            Console.WriteLine($"Currently monitored: {ViewModel.CurrentSettings.BusinessSoftware}");
                            Console.WriteLine("Enter new process name (e.g., Calculator) or Enter to cancel:");
                        }

                        string newBusinessSoft = Console.ReadLine() ?? "";

                        if (!string.IsNullOrWhiteSpace(newBusinessSoft))
                        {
                            // On appelle la méthode du ViewModel pour sauvegarder
                            ViewModel.SetBusinessSoftware(newBusinessSoft);

                            Console.WriteLine(ViewModel.CurrentSettings.Language == "fr"
                                ? " Mise à jour effectuée !"
                                : " Update successful!");
                        }
                        Console.WriteLine("\nAppuyez sur une touche pour revenir au menu...");
                        Console.ReadKey();
                        break;
                    case "q":
                        exit = true;
                        break;
                }
            }

        }
    }
}



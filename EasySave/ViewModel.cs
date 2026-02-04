using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using EasySave.Model;

namespace EasySave.ViewModel
{
    /// <summary> Chef d'orchestre du ViewModel
    /// qui contient toute la logique ( attributs, méthodes ) necessaire
    /// pour réaliser un travail de sauvegarde et fait le lien entre le Model et la View </summary>
    public class MainViewModel
    {

        /// <summary> Liste contenant les travaux de sauvegarde(max 5). </summary>
        public List<BackUpJob> Jobs { get; set; } // Cette liste permet de pouvoir afficher dans le menu les job créés

        /// <summary> Attribut pour stocker la langue (fr par défaut) </summary>
        public string CurrentLanguage { get; set; } = "fr";

        /// <summary>
        /// Constructeur du ViewModel.
        /// Initialise les structures de données (la liste des travaux) nécessaires au fonctionnement de l'application console.
        /// </summary>
        public MainViewModel()
        {
            // Initialisation de la liste au démarrage
            Jobs = new List<BackUpJob>();
        }

        /// <summary> Méthode pour changer la langue </summary>
        public void SwitchLanguage()
        {
            CurrentLanguage = (CurrentLanguage == "fr") ? "en" : "fr";
        }

        /// <summary>
        /// Le chemin où sera créé le fichier JSON (dans le même dossier que l'exe)
        /// </summary>
        private string configPath = "jobs_config.json";


        //--------------------------------------------------------------------------------------------
        // Partie sur la gestion de la liste des Jobs et le fichier JSON les contenant
        //--------------------------------------------------------------------------------------------

        // 1. On commence par charger au démarrage, avec le fichier json, les jobs créés précedemmennt
        // Si le fichier json est inexistant la liste des jobs sera vide et on pourra donc en créer des nouveaux lors de notre session


        /// <summary>
        /// Méthode pour charger les jobs au démarrage
        /// </summary>
        public void LoadJobsConfig()
        {
            // Vérifie si le fichier JSON existe physiquement
            if (File.Exists(configPath))
            {
                // Lit tout le contenu texte du fichier JSON
                string json = File.ReadAllText(configPath);

                // Convertit le texte JSON en une liste d'objets BackUpJob (Désérialisation)
                // Si le fichier est vide ou corrompu, on crée une nouvelle liste vide ( d'où le ??) 
                var loadedJobs = JsonSerializer.Deserialize<List<BackUpJob>>(json) ?? new List<BackUpJob>();

                if (loadedJobs != null)
                {
                    // Met à jour la liste principale du ViewModel avec les données chargées
                    this.Jobs = loadedJobs;
                }
            }
            // Si le fichier n'existe pas, la méthode ne fait rien et la liste reste vide (initialisée par le constructeur)
        }


        // 2. Ici c'est pour sauvegarder les jobs

        /// <summary>
        /// Méthode pour sauvegarder dans le fichier JSON
        /// les jobs créés dans la session active, ces jobs se trouvent dans la liste
        /// ainsi on va pouvoir les recupérer si on relance la console
        /// </summary>
        public void SaveJobsConfig()
        {
            // Pour rendre le fichier JSON lisible par un humain (indentation)
            var options = new JsonSerializerOptions { WriteIndented = true };

            // Sérialisation : transforme la liste 'Jobs' en string au format JSON
            string json = JsonSerializer.Serialize(this.Jobs, options);

            // Écriture physique de la chaîne JSON dans le fichier
            File.WriteAllText(configPath, json);
        }


        // 3. Effacer le fichier JSON contenant les jobs
        // Si on n'efface pas le fichier JSON avec la liste des jobs sera lu
        // lorsque l'on lance une nouvelle session console
        // On laisse donc le choix à l'utilisateur de pouvoir effacer le fichier JSON
        // Ou le garder pour une prochaine session

        /// <summary>
        /// Méthode pour effacer le fichier JSON contenant les jobs sauvegardés lors de la session.
        /// Cela permet une reinitialisation, pour une prochaine session, la liste sera vide.
        /// </summary>
        public void ClearAllJobs()
        {
            // On vide la liste
            this.Jobs.Clear();

            // On verifie si le fichier JSON existe sur notre disque
            if (File.Exists(configPath))
            {
                // S'il existe, on le supprime
                File.Delete(configPath);
            }
        }

        //--------------------------------------------------------------------------------------------
        // SECTION : GESTION ET EXÉCUTION DES TRAVAUX (Logique de sauvegarde)
        //--------------------------------------------------------------------------------------------

        /// <summary>
        /// Méthode pour ajoute un nouveau travail de sauvegarde à la liste
        /// </summary>
        /// <param name="name">Nom donné par l'utlisateur au travail de sauvegarde</param>
        /// <param name="source">Chemin du dossier source à copier</param>
        /// <param name="target">Chemin du dossier de destination</param>
        /// <param name="type">Type de sauvegarde (Complet ou Differentiel)</param>
        public void AddJob(string name, string source, string target, string type)
        {
            //Vérification de la contrainte de ne pas avoir plus de 5 jobs simultanés
            if (Jobs.Count < 5)
            {
                // Instancation d'un nouveau BackUpJob et ajout dans la liste
                Jobs.Add(new BackUpJob(name, source, target, type));

                //Mise à jour du JSON et ecriture du job dedans
                SaveJobsConfig();
            }
            else
            {
                // Message d'erreur si la lamite des 5 jobs est atteinte
                Console.WriteLine(CurrentLanguage == "fr"
                    ? "[ERREUR] Limite de 5 travaux atteinte."
                    : "[ERROR] Limit of 5 jobs reached.");
            }
        }

        /// <summary>
        /// Execution de la sauvegarde.
        /// Verifie la validité du dossier source
        /// Copie les fichiers selon le mode choisi
        /// </summary>
        /// <param name="index"></param>
        public void ExecuteJob(int index)
        {
            // Récupère l'objet BackUpJob à partir de son index indiqué par l'utilisateur ( l'index est l'ordre de creation des jobs)
            var job = Jobs[index];

            try
            {
                // Verification de l'existance du dossier source avant de commencer la sauvegarde
                if (!Directory.Exists(job.SourceDir))
                {
                    Console.WriteLine(CurrentLanguage == "fr"
                        ? $"[ERREUR] Le dossier source n'existe plus : {job.SourceDir}"
                        : $"[ERROR] Source directory not found: {job.SourceDir}");
                    return;
                }

                // Initialisation de l'état et de la progression, nécessaire pour les logs
                job.State = "Active";
                job.Progress = 0;

                // Récupération de tous les fichiers ceux présents à la racine et ceux dans des sous-dossiers
                string[] files = Directory.GetFiles(job.SourceDir, "*.*", SearchOption.AllDirectories);

                // Stockage du nombre total de fichiers, utile pour calculer la progression
                job.TotalFiles = files.Length;

                // Calcul de la taille totale des fichiers, également pour la progression
                long totalSize = 0;
                foreach (string f in files) { totalSize += new FileInfo(f).Length; }
                job.TotalSize = totalSize;

                // Boucle pour traiter fichier par fichier
                for (int i = 0; i < files.Length; i++)
                {
                    // 1. On extrait la structure interne (ex: "Documents\Projet1\file.txt") en retirant le chemin source
                    string relativePath = Path.GetRelativePath(job.SourceDir, files[i]);

                    // 2. On reconstruit ce même chemin dans le dossier de destination
                    // Cela permet d'avoir dans le dossier de destination la même organisation que dans le dossier source
                    string destFile = Path.Combine(job.TargetDir, relativePath);

                    // Création du dossier de destination s'il n'existe pas encore
                    string? destFolder = Path.GetDirectoryName(destFile);

                    if (!string.IsNullOrEmpty(destFolder) && !Directory.Exists(destFolder))
                    {
                        Directory.CreateDirectory(destFolder);
                    }

                     // --- TYPE DE SAUVEGARDE (COMPLET VS DIFFÉRENTIEL) ---
                     // --- DEBUT DE LA PARTIE SUR LA SAUVEGARDE DIFFERENTIELLE ---

                     // Variable qui permet d'autoriser la copie
                     bool canStartCopy = true;


                    // Si le type indiqué par l'utilisateur contient "diff" (insensible à la casse) on considère qu'il a demandé une sauvegarde différentielle
                    // On verifie si la copie est vraiment necessaire, on compare la source et la destination
                    if (job.BackUpType.Contains("diff", StringComparison.OrdinalIgnoreCase) ||
                        job.BackUpType.Contains("différentiel", StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(destFile))
                        {
                            // On compare la date de dernière modification
                            DateTime sourceTime = File.GetLastWriteTime(files[i]);
                            DateTime destTime = File.GetLastWriteTime(destFile);

                            // Si le fichier source n'est pas plus récent, on ne copie pas
                            if (sourceTime <= destTime)
                            {
                                canStartCopy = false;
                            }
                        }
                    }
                        // --- FIN DE LA PARTIE DIFFERENTIELLE ---

                        // Si le type est complet on ignore le if sur la partie différentielle et on passe directement à la copie
                        // Si on a toujours l'autorisation on réalise la copie 
                        if (canStartCopy)
                    {
                        File.Copy(files[i], destFile, true);
                    }

                        // Calcul du pourcentage de progression
                        // (i + 1) : Nombre de fichiers traités (on ajoute 1 car l'index 'i' commence à 0).
                        // * 100 pour avoir un pourcentage
                        // div par files.Length : nombre total de fichiers
                        job.Progress = (int)((i + 1) * 100 / files.Length);
                }

                Console.WriteLine(CurrentLanguage == "fr"
                    ? $"Succès : {job.Name} terminé."
                    : $"Success: {job.Name} finished.");
                }
            catch (Exception ex)
            {
                // Gestion des erreurs
                if (CurrentLanguage == "fr")
                    Console.WriteLine($"[ERREUR] Impossible d'exécuter {job.Name} : {ex.Message}");
                else
                    Console.WriteLine($"[ERROR] Could not execute {job.Name} : {ex.Message}");
            }
            finally
            {   // Remise à l'état inactif
                job.State = "Inactive";
                job.Progress = 100;
            }
    
        }
    }
}



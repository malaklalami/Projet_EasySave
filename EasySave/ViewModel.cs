using System;
using System.IO;
using System.Collections.Generic;
using EasySave.Model;

namespace EasySave.ViewModel
{
    public class MainViewModel
    {
        // Liste qui contient nos travaux de sauvegarde (max 5)
        public List<BackUpJob> Jobs { get; set; }

        // Attribut pour stocker la langue (fr par défaut)
        public string CurrentLanguage { get; set; } = "fr";

        public MainViewModel()
        {
            // Initialisation de la liste au démarrage
            Jobs = new List<BackUpJob>();
        }

        // Méthode pour changer la langue
        public void SwitchLanguage()
        {
            CurrentLanguage = (CurrentLanguage == "fr") ? "en" : "fr";
        }

        // Méthode pour ajouter un travail (View -> ViewModel)
        public void AddJob(string name, string source, string target, string type)
        {
            // Correction de la limite : < 5 pour ne pas dépasser 5 slots
            if (Jobs.Count < 5)
            {
                Jobs.Add(new BackUpJob(name, source, target, type));
            }
            else
            {
                Console.WriteLine(CurrentLanguage == "fr"
                    ? "[ERREUR] Limite de 5 travaux atteinte."
                    : "[ERROR] Limit of 5 jobs reached.");
            }
        }

        public void ExecuteJob(int index)
        {
            // 1. On récupère le travail concerné
            var job = Jobs[index];

            try
            {
                // Vérification de sécurité pour la source
                if (!Directory.Exists(job.SourceDir))
                {
                    Console.WriteLine(CurrentLanguage == "fr"
                        ? $"[ERREUR] Le dossier source n'existe plus : {job.SourceDir}"
                        : $"[ERROR] Source directory not found: {job.SourceDir}");
                    return;
                }

                // 2. Préparation : État Actif et progression à 0
                job.State = "Active";
                job.Progress = 0;

                // --- LA MODIFICATION EST ICI ---
                // On remplace GetFiles(path) par cette version qui va dans les sous-dossiers
                string[] files = Directory.GetFiles(job.SourceDir, "*.*", SearchOption.AllDirectories);
                job.TotalFiles = files.Length;

                // Calcul de la taille totale pour les futurs logs
                long totalSize = 0;
                foreach (string f in files) { totalSize += new FileInfo(f).Length; }
                job.TotalSize = totalSize;

                // 4. BOUCLE DE COPIE RÉELLE
                for (int i = 0; i < files.Length; i++)
                {
                    // On calcule le chemin relatif pour recréer les sous-dossiers
                    string relativePath = Path.GetRelativePath(job.SourceDir, files[i]);
                    string destFile = Path.Combine(job.TargetDir, relativePath);

                    // On crée le sous-dossier de destination si nécessaire
                    string destFolder = Path.GetDirectoryName(destFile);
                    if (!Directory.Exists(destFolder))
                    {
                        Directory.CreateDirectory(destFolder);
                    }

                    // Copie physique du fichier
                    File.Copy(files[i], destFile, true);

                    // Mise à jour de la progression (%)
                    job.Progress = (int)((i + 1) * 100 / files.Length);
                }
                // --- FIN DE LA MODIFICATION ---

                Console.WriteLine(CurrentLanguage == "fr"
                    ? $"Succès : {job.Name} terminé."
                    : $"Success: {job.Name} finished.");
            }
            catch (Exception ex)
            {
                // Tes messages d'erreurs personnalisés sont conservés ici
                if (CurrentLanguage == "fr")
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
            finally
            {
                // 5. Finalisation
                job.State = "Inactive";
                job.Progress = 100;
            }
        }
    }
}



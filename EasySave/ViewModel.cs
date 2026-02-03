using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
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
        // Le chemin où sera créé le fichier (dans le même dossier que l'exe)
        private string configPath = "jobs_config.json";

        // 1. Charger les jobs au démarrage
        public void LoadJobsConfig()
        {
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var loadedJobs = JsonSerializer.Deserialize<List<BackUpJob>>(json) ?? new List<BackUpJob>();
                if (loadedJobs != null)
                {
                    this.Jobs = loadedJobs;
                }
            }
        }

        // 2. Sauvegarder les jobs
        public void SaveJobsConfig()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(this.Jobs, options);
            File.WriteAllText(configPath, json);
        }
        // 3. Ton option "Clean" (Option 6)
        public void ClearAllJobs()
        {
            this.Jobs.Clear();
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }


            // Méthode pour ajouter un travail (View -> ViewModel)
        public void AddJob(string name, string source, string target, string type)
        {
            if (Jobs.Count < 5)
            {
                Jobs.Add(new BackUpJob(name, source, target, type));
                SaveJobsConfig();
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
            var job = Jobs[index];

            try
            {
                if (!Directory.Exists(job.SourceDir))
                {
                    Console.WriteLine(CurrentLanguage == "fr"
                        ? $"[ERREUR] Le dossier source n'existe plus : {job.SourceDir}"
                        : $"[ERROR] Source directory not found: {job.SourceDir}");
                    return;
                }

                job.State = "Active";
                job.Progress = 0;

                string[] files = Directory.GetFiles(job.SourceDir, "*.*", SearchOption.AllDirectories);
                job.TotalFiles = files.Length;

                long totalSize = 0;
                foreach (string f in files) { totalSize += new FileInfo(f).Length; }
                job.TotalSize = totalSize;

                for (int i = 0; i < files.Length; i++)
                {
                    string relativePath = Path.GetRelativePath(job.SourceDir, files[i]);
                    string destFile = Path.Combine(job.TargetDir, relativePath);

                    string? destFolder = Path.GetDirectoryName(destFile);

                    if (!string.IsNullOrEmpty(destFolder) && !Directory.Exists(destFolder))
                    {
                        Directory.CreateDirectory(destFolder);
                    }
                    // --- DEBUT DE LA LOGIQUE DIFFERENTIELLE ---
                    bool shouldCopy = true;

                    // Si le type contient "diff" (insensible à la casse)
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
                                shouldCopy = false;
                            }
                        }
                    }

                    if (shouldCopy)
                    {
                        File.Copy(files[i], destFile, true);
                    }
                    // --- FIN DE LA LOGIQUE DIFFERENTIELLE ---

                    job.Progress = (int)((i + 1) * 100 / files.Length);
                }

                Console.WriteLine(CurrentLanguage == "fr"
                    ? $"Succès : {job.Name} terminé."
                    : $"Success: {job.Name} finished.");
            }
            catch (Exception ex)
            {
                // ... tes messages d'erreurs restent identiques ...
                if (CurrentLanguage == "fr")
                    Console.WriteLine($"[ERREUR] Impossible d'exécuter {job.Name} : {ex.Message}");
                else
                    Console.WriteLine($"[ERROR] Could not execute {job.Name} : {ex.Message}");
            }
            finally
            {
                job.State = "Inactive";
                job.Progress = 100;
            }
    
        }
    }
}



using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using EasyLog;
using EasyLibrary.Models;
using EasyLibrary.Services;

namespace EasySave.ViewModel
{
    public class MainViewModel
    {

        public IVue Vue { get; set; }

        // Liste qui contient nos travaux de sauvegarde (max 5)
        public List<BackUpJob> Jobs { get; set; }

        // Attribut pour stocker la langue (fr par défaut)
        public string CurrentLanguage { get; set; } = "fr";

        // Instance du Logger de la DLL
        private LoggerService LoggerService = new LoggerService();

        // Pour lire ou changer le format des logs (v1.1)
        public string CurrentLogFormat
        {
            get => LoggerService.LogFormat;
            set => LoggerService.LogFormat = value;
        }

        // Instance pour gérer l'état en temps réel
        private StateService _stateService = new StateService();
        private JobManager jobManager = new JobManager();

        public void Start()
        {
            Jobs = jobManager.loadJobs("jobs.json");
            Vue.AfficheMenuPrincipal();
        }

        // Méthode pour changer la langue
        public void SwitchLanguage()
        {
            CurrentLanguage = (CurrentLanguage == "fr") ? "en" : "fr";
            //TODO écrire la config dans un fichier json pour la persistance
        }

        public void SwitchLogFormat()
        {
            if (CurrentLogFormat == "json")
            {
                CurrentLogFormat = "xml";
            }
            else
            {
                CurrentLogFormat = "json";
            }
        }

        // Méthode pour ajouter un travail (View -> ViewModel)
        public void AddJob(string name, string source, string target, string type)
        {
            // Correction de la limite : < 5 pour ne pas dépasser 5 slots
            if (Jobs.Count < 5)
            {
                Jobs.Add(new BackUpJob(name, source, target, type));
                jobManager.saveJobs(Jobs); // Sauvegarde après ajout
            }
            else
            {
                Vue.MaximumJobLimitReached();
            }
        }

        public void ExecuteJob(int index)
        {
            // 1. On récupère le travail concerné
            var job = Jobs[index];
            // Chrono pour le temps de transfert
            Stopwatch stopwatch = new Stopwatch();

            try
            {
                // Vérification de sécurité pour la source
                if (!Directory.Exists(job.SourceDir))
                {
                    Vue.JobExecutionError(job);
                    return;
                }

                // 2. Préparation : État Actif et progression à 0
                job.State = "Active";
                job.Progress = 0;

                // Mise à jour du fichier d'état au lancement
                _stateService.UpdateState(job);

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

                    FileInfo fileInfo = new FileInfo(files[i]);

                    stopwatch.Restart(); // On lance le chrono pour ce fichier
                    // Copie physique du fichier
                    File.Copy(files[i], destFile, true);
                    stopwatch.Stop(); // On arrête le chrono

                    // APPEL DE LA DLL LOGS
                    LoggerService.WriteLog(new LogEntry
                    {
                        JobName = job.Name,
                        SourcePath = files[i],
                        TargetPath = destFile,
                        FileSize = fileInfo.Length,
                        TransferTimeMs = stopwatch.Elapsed.TotalMilliseconds
                    });

                    // Mise à jour de la progression (%)
                    job.Progress = (int)((i + 1) * 100 / files.Length);

                    // Mise à jour du fichier d'état en temps réel
                    _stateService.UpdateState(job);
                }
                // --- FIN DE LA MODIFICATION ---
                Vue.JobSucceeded(job);
            }
            catch (Exception ex)
            {
                Vue.JobExecutionError(job, ex);
            }
            finally
            {
                // 5. Finalisation
                job.State = "Inactive";
                job.Progress = 100;

                // Dernier refresh du fichier d'état
                _stateService.UpdateState(job);
            }
        }
    }
}
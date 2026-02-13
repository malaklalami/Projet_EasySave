using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using EasyLibrary.Models;


namespace EasyLibrary.Services
// Le JobService est responsable de toute la logique métier liée à l'exécution des sauvegardes, à la gestion des jobs et à l'interaction avec les autres services.
{
    public class JobService
    {
        private readonly StateService _stateService;
        private readonly LoggerService _loggerService;
        private readonly CryptoService _cryptoService;
        private readonly SettingsJsonService _settingsService;
        private readonly JobManager _jobManager;
        private readonly BusinessSoftwareService _businessService = new BusinessSoftwareService();
        // Le constructeur reçoit toutes les dépendances nécessaires pour fonctionner
        public JobService(StateService stateService, LoggerService loggerService, CryptoService cryptoService, SettingsJsonService settingsService, JobManager jobManager)
        {
            _stateService = stateService;
            _loggerService = loggerService;
            _cryptoService = cryptoService;
            _settingsService = settingsService;
            _jobManager = jobManager;
        }

        // --- LOGIQUE D'EXÉCUTION ---
        public void ExecuteJob(BackUpJob job, List<string> encryptionExtensions, IVue vue, string businessSoftwareName)
        {
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                if (_businessService.IsRunning(businessSoftwareName))
                {
                    Console.WriteLine($"\n[AVERTISSEMENT] Logiciel métier '{businessSoftwareName}' détecté. Sauvegarde annulée.");
                    _loggerService.WriteLog(new LogEntry
                    {
                        JobName = job.Name,
                        SourcePath = "INTERRUPTION_LOGICIEL_METIER",
                        TargetPath = businessSoftwareName,
                        FileSize = 0,
                        TransferTimeMs = -1 // On met -1 pour indiquer une erreur/arrêt dans le log
                    });
                    return;
                }
                if (!Directory.Exists(job.SourceDir)) { vue?.JobExecutionError(job); return; }

                job.State = "Active";
                job.Progress = 0;
                _stateService.UpdateState(job);

                string[] files = Directory.GetFiles(job.SourceDir, "*.*", SearchOption.AllDirectories);
                job.TotalFiles = files.Length;

                for (int i = 0; i < files.Length; i++)
                {
                   
                    if (_businessService.IsRunning(businessSoftwareName))
                    {
                        Console.WriteLine($"\n[INTERRUPTION] {businessSoftwareName} détecté. Arrêt après le fichier actuel.");
                        // On consigne l'arrêt dans les logs avant de quitter
                      
                        _loggerService.WriteLog(new LogEntry
                        {
                            JobName = job.Name,
                            SourcePath = "INTERRUPTION_LOGICIEL_METIER",
                            TargetPath = businessSoftwareName,
                            FileSize = 0,
                            TransferTimeMs = -1 // On met -1 pour indiquer une erreur/arrêt dans le log
                        });

                        break;
                    }
                    string relativePath = Path.GetRelativePath(job.SourceDir, files[i]);
                    string destFile = Path.Combine(job.TargetDir, relativePath);
                    string destFolder = Path.GetDirectoryName(destFile);

                    if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                    FileInfo fileInfo = new FileInfo(files[i]);
                    stopwatch.Restart();

                    // 1. COPIE DU FICHIER
                    File.Copy(files[i], destFile, true);

                    // 2. CRYPTAGE ET CALCUL DU TEMPS (La consigne : 0, >0 ou <0)
                    long encryptionTime = 0; // Par défaut 0 (si pas de cryptage)

                    if (_cryptoService.ShouldEncrypt(destFile, encryptionExtensions))
                    {
                        // On appelle CryptoSoft et on récupère le temps (ou l'erreur)
                        encryptionTime = _cryptoService.Encrypt(destFile);
                    }

                    stopwatch.Stop();

                    // 3. ECRITURE DU LOG AVEC LA NOUVELLE INFO
                    _loggerService.WriteLog(new LogEntry
                    {
                        JobName = job.Name,
                        SourcePath = files[i],
                        TargetPath = destFile,
                        FileSize = fileInfo.Length,
                        TransferTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                        EncryptionTimeMs = encryptionTime // <--- L'évolution demandée est ici
                    });

                    // 4. MISE A JOUR DE LA PROGRESSION
                    job.Progress = (int)((i + 1) * 100 / files.Length);
                    _stateService.UpdateState(job);
                }
                if (job.Progress == 100)
                {
                    vue?.JobSucceeded(job);
                }
                else
                {
                    // Optionnel : avertir la vue que ça a été stoppé
                    Console.WriteLine("Sauvegarde incomplète suite à l'interruption.");
                }
            }
            catch (Exception ex) { vue?.JobExecutionError(job, ex); }
            finally { job.State = "Inactive"; _stateService.UpdateState(job); }
        }

        

        //----------------------------------------------ADDJOB--------------------------------------------
        public void AddJob(List<BackUpJob> jobs, string name, string source, string target, string type)
        {
            jobs.Add(new BackUpJob(name, source, target, type));
            _jobManager.saveJobs(jobs);
        }
        // ----------------------------------------------UPDATEJOB-----------------------------------------
        public void UpdateJob(List<BackUpJob> jobs, int index, string name, string source, string target, string type)
        {
            if (index >= 0 && index < jobs.Count)
            {
                // On met à jour les propriétés de l'objet existant
                jobs[index].Name = name;
                jobs[index].SourceDir = source;
                jobs[index].TargetDir = target;
                jobs[index].BackUpType = type;

                // On écrase le JSON pour sauvegarder les modifs
                _jobManager.saveJobs(jobs);
            }
        }
        // ----------------------------------------------CLEARJOBS-----------------------------------------
        public void ClearJobs(List<BackUpJob> jobs)
        {
            jobs?.Clear();
            _jobManager.clearJobs();
        }
        // ----------------------------------------------REMOVEJOB-----------------------------------------
        public void RemoveJob(List<BackUpJob> jobs, int index)
        {
            if (index >= 0 && index < jobs.Count)
            {
                jobs.RemoveAt(index);
                _jobManager.saveJobs(jobs); // On sauvegarde la liste mise à jour dans le JSON
            }
        }

        // ----------------------------------------------SWITCHLANGUAGE--------------------------------------
        public void SwitchLanguage(ConsoleSettingsJson settings)
        {
            settings.Language = (settings.Language == "fr") ? "en" : "fr";
            _settingsService.Save(settings);
        }
        //------------------------------SWITCHLOGFORMAT--------------------------------------
        public void SwitchLogFormat(ConsoleSettingsJson settings)
        {
            settings.LogFormat = (settings.LogFormat == "json") ? "xml" : "json";
            _loggerService.LogFormat = settings.LogFormat;
            _settingsService.Save(settings);
        }
        //-----------------------------------------------ADDEXTENSION----------
        public void AddExtension(ConsoleSettingsJson settings, string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return;
            string cleanExt = extension.Trim().ToLower();
            if (!cleanExt.StartsWith(".")) cleanExt = "." + cleanExt;

            if (!settings.EncryptionExtensions.Contains(cleanExt))
            {
                settings.EncryptionExtensions.Add(cleanExt);
                _settingsService.Save(settings);
            }
        }
        
        public void ExecuteFromCommandLine(string input, List<BackUpJob> jobs, ConsoleSettingsJson settings, IVue vue)
        {
            if (jobs == null || settings == null || vue == null)
            {
                Console.WriteLine("[ERREUR] Initialisation incomplète. Vérifiez les fichiers de config.");
                return;
            }

            var indices = new List<int>();

            // 1. Parsing de la chaîne (0-2 ou 1;3 ou 1)
            if (input.Contains("-"))
            {
                string[] range = input.Split('-');
                if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                {
                    for (int i = start; i <= end; i++) indices.Add(i);
                }
            }
            else if (input.Contains(";"))
            {
                string[] list = input.Split(';');
                foreach (var item in list)
                {
                    if (int.TryParse(item, out int id)) indices.Add(id);
                }
            }
            else if (int.TryParse(input, out int id))
            {
                indices.Add(id);
            }

            // 2. Exécution séquentielle avec vérification
            foreach (int index in indices.Distinct())
            {
                if (index >= 0 && index < jobs.Count)
                {
                    Console.WriteLine($"\n[CMD] Lancement du travail {index} : {jobs[index].Name}...");
                    // On appelle l'exécution réelle que tu as déjà codée
                    ExecuteJob(jobs[index], settings.EncryptionExtensions, vue, settings.BusinessSoftware);
                }
                else
                {
                    Console.WriteLine($"[ERREUR] Le job n°{index} n'existe pas.");
                }
            }
        }
    }
}
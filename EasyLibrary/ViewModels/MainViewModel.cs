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

        // Déclarer le service de configuration
        private SettingsJsonService _settingsService = new SettingsJsonService();

        // Déclarer l'objet qui contiendra les réglages (pour éviter l'erreur dans Start)
        public ConsoleSettingsJson CurrentSettings { get; set; }

        // On définit le chemin de base (Universel)
        private static string BasePath = AppDomain.CurrentDomain.BaseDirectory;

        // 2. On détermine le nom du fichier selon l'OS (Mac/Linux vs Windows)
        private static string CryptoFileName = OperatingSystem.IsWindows() ? "CryptoSoft.exe" : "CryptoSoft";

        // 3. On initialise le CryptoService avec le bon nom de fichier
        private CryptoService _cryptoService = new CryptoService(
            Path.Combine(BasePath, CryptoFileName),
            "MA_CLE_XOR_123");

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
            // CHARGEMENT DE LA CONFIG (Langue + Extensions)
            // C'est ici qu'on appelle  SettingsJsonService
            CurrentSettings = _settingsService.Load();
            LoggerService.LogFormat = CurrentSettings.LogFormat;
            Jobs = jobManager.loadJobs("jobs.json");
            Vue.AfficheMenuPrincipal();
        }

        // Méthode pour changer la langue
        public void SwitchLanguage()
        {
            // On utilise CurrentSettings au lieu de CurrentLanguage
            CurrentSettings.Language = (CurrentSettings.Language == "fr") ? "en" : "fr";

            // On sauvegarde le changement dans le JSON !
            _settingsService.Save(CurrentSettings);
        }

        public void SwitchLogFormat()
        {   // On change la valeur dans le ettings.json
            if (CurrentSettings.LogFormat == "json")
            {
                CurrentSettings.LogFormat = "xml";
            }
            else
            {
                CurrentSettings.LogFormat = "json";
            }
            // On met à jour le settings.json pour qu'il change de format immédiatement
            LoggerService.LogFormat = CurrentSettings.LogFormat;

            // On sauvegarde le tout dans le fichier settings.json
            _settingsService.Save(CurrentSettings);
        }

        public void AddEncryptionExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return;

            // Nettoyage de l'entrée
            string cleanExt = extension.Trim().ToLower();
            if (!cleanExt.StartsWith(".")) cleanExt = "." + cleanExt;

            // Mise à jour du Modèle
            if (!CurrentSettings.EncryptionExtensions.Contains(cleanExt))
            {
                CurrentSettings.EncryptionExtensions.Add(cleanExt);

                // Persistance via le Service
                _settingsService.Save(CurrentSettings);
            }
        }
        // Méthode pour ajouter un travail (View -> ViewModel)
        public void AddJob(string name, string source, string target, string type)
        {

            //if (Jobs.Count < 5)
            //{
                Jobs.Add(new BackUpJob(name, source, target, type));
                jobManager.saveJobs(Jobs); // Sauvegarde après ajout
            //}
            //else
            //{
            //    Vue.MaximumJobLimitReached();
            //}
        }

        public void ClearAllJobs()
        {
            // On vide la liste en mémoire (l'affichage se videra)
            if (Jobs != null)
            {
                Jobs.Clear();
            }

            // On demande au manager de supprimer le fichier JSON
            jobManager.clearJobs();
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

                    Console.WriteLine("Vérification pour : " + destFile);

                    if (_cryptoService.ShouldEncrypt(destFile, CurrentSettings.EncryptionExtensions))
                    {
                        Console.WriteLine("LANCEMENT DU CHIFFREMENT...");
                        _cryptoService.Encrypt(destFile);
                    }
                    else
                    {
                        Console.WriteLine("Chiffrement ignoré (Extension non trouvée)");
                    }

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
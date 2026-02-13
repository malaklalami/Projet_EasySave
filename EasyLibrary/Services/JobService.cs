using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using EasyLibrary.Models;
using EasyLog;

namespace EasyLibrary.Services
{
    public class JobService
    {
        private readonly StateService _stateService;
        private readonly LoggerService _loggerService;
        private readonly CryptoService _cryptoService;
        private readonly SettingsJsonService _settingsService;
        private readonly JobManager _jobManager;

        public JobService(StateService stateService, LoggerService loggerService, CryptoService cryptoService, SettingsJsonService settingsService, JobManager jobManager)
        {
            _stateService = stateService;
            _loggerService = loggerService;
            _cryptoService = cryptoService;
            _settingsService = settingsService;
            _jobManager = jobManager;
        }

        // --- LOGIQUE D'EXÉCUTION ---
        public void ExecuteJob(BackUpJob job, List<string> encryptionExtensions, IVue vue)
        {
            Stopwatch stopwatch = new Stopwatch();
            try
            {
                if (!Directory.Exists(job.SourceDir)) { vue?.JobExecutionError(job); return; }

                job.State = "Active";
                job.Progress = 0;
                _stateService.UpdateState(job);

                string[] files = Directory.GetFiles(job.SourceDir, "*.*", SearchOption.AllDirectories);
                job.TotalFiles = files.Length;

                for (int i = 0; i < files.Length; i++)
                {
                    string relativePath = Path.GetRelativePath(job.SourceDir, files[i]);
                    string destFile = Path.Combine(job.TargetDir, relativePath);
                    string destFolder = Path.GetDirectoryName(destFile);
                    if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                    FileInfo fileInfo = new FileInfo(files[i]);
                    stopwatch.Restart();
                    File.Copy(files[i], destFile, true);

                    if (_cryptoService.ShouldEncrypt(destFile, encryptionExtensions))
                        _cryptoService.Encrypt(destFile);

                    stopwatch.Stop();

                    _loggerService.WriteLog(new LogEntry
                    {
                        JobName = job.Name,
                        SourcePath = files[i],
                        TargetPath = destFile,
                        FileSize = fileInfo.Length,
                        TransferTimeMs = stopwatch.Elapsed.TotalMilliseconds
                    });

                    job.Progress = (int)((i + 1) * 100 / files.Length);
                    _stateService.UpdateState(job);
                }
                vue?.JobSucceeded(job);
            }
            catch (Exception ex) { vue?.JobExecutionError(job, ex); }
            finally { job.State = "Inactive"; _stateService.UpdateState(job); }
        }

        // --- LOGIQUE DE GESTION DES JOBS ---
        public void AddJob(List<BackUpJob> jobs, string name, string source, string target, string type)
        {
            jobs.Add(new BackUpJob(name, source, target, type));
            _jobManager.saveJobs(jobs);
        }

        public void ClearJobs(List<BackUpJob> jobs)
        {
            jobs?.Clear();
            _jobManager.clearJobs();
        }

        // --- LOGIQUE DE CONFIGURATION ---
        public void SwitchLanguage(ConsoleSettingsJson settings)
        {
            settings.Language = (settings.Language == "fr") ? "en" : "fr";
            _settingsService.Save(settings);
        }

        public void SwitchLogFormat(ConsoleSettingsJson settings)
        {
            settings.LogFormat = (settings.LogFormat == "json") ? "xml" : "json";
            _loggerService.LogFormat = settings.LogFormat;
            _settingsService.Save(settings);
        }

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
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using EasyLibrary.Models;
using EasyLibrary.Services;

namespace EasySave.ViewModel
{
    public class MainViewModel
    {
        public IVue Vue { get; set; }
        public List<BackUpJob> Jobs { get; set; }
        public ConsoleSettingsJson CurrentSettings { get; set; }

        private readonly JobService _jobService;
        private readonly SettingsJsonService _settingsService = new SettingsJsonService();
        private readonly JobManager _jobManager = new JobManager();

        public MainViewModel()
        {
            // Initialisation des dépendances
            var stateService = new StateService();
            var loggerService = new EasyLog.LoggerService();
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            string cryptoFileName = OperatingSystem.IsWindows() ? "CryptoSoft.exe" : "CryptoSoft";
            var cryptoService = new CryptoService(Path.Combine(basePath, cryptoFileName), "MA_CLE_XOR_123");

            // Le JobService reçoit tout ce dont il a besoin pour travailler
            _jobService = new JobService(stateService, loggerService, cryptoService, _settingsService, _jobManager);
        }

        public void Start()
        {
            CurrentSettings = _settingsService.Load();
            Jobs = _jobManager.loadJobs("jobs.json");
            Vue?.AfficheMenuPrincipal();
        }

        // Appels directs au service
        public void ExecuteJob(int index) => _jobService.ExecuteJob(Jobs[index], CurrentSettings.EncryptionExtensions, Vue);

        public void AddJob(string n, string s, string t, string ty) => _jobService.AddJob(Jobs, n, s, t, ty);

        public void ClearAllJobs() => _jobService.ClearJobs(Jobs);

        public void SwitchLanguage() => _jobService.SwitchLanguage(CurrentSettings);

        public void SwitchLogFormat() => _jobService.SwitchLogFormat(CurrentSettings);

        public void AddEncryptionExtension(string ext) => _jobService.AddExtension(CurrentSettings, ext);
    }
}
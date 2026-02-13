using System;
using System.Collections.Generic;
using System.IO;
using EasyLibrary.Models;
using EasyLibrary.Services;

namespace EasyLibrary.ViewModels
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
            var loggerService = new LoggerService();
            string basePath = AppDomain.CurrentDomain.BaseDirectory;


            // Le JobService reçoit tout ce dont il a besoin pour travailler
            _jobService = new JobService(stateService, loggerService, _settingsService, _jobManager);
        }

        public void LoadData()
        {
            CurrentSettings = _settingsService.Load();
            Jobs = _jobManager.loadJobs("jobs.json");

        }

        public void Start()
        {
            LoadData(); // On charge les données
            Vue?.AfficheMenuPrincipal(); // On affiche le menu
        }

        // Appels directs au service
        public void ExecuteJob(int index) => _jobService.ExecuteJob(Jobs[index], Vue);

        public void AddJob(string n, string s, string t, string ty) => _jobService.AddJob(Jobs, n, s, t, ty);

        public void EditJob(int index, string name, string source, string target, string type)
        {
            _jobService.UpdateJob(Jobs, index, name, source, target, type);
        }

        public void ClearAllJobs() => _jobService.ClearJobs(Jobs);

        public void DeleteJob(int index) => _jobService.RemoveJob(Jobs, index);

        public void SwitchLanguage() => _jobService.SwitchLanguage(CurrentSettings);

        public void SwitchLogFormat() => _jobService.SwitchLogFormat(CurrentSettings);



        public void ExecuteJobsFromArgs(string input)
        {
            _jobService.ExecuteFromCommandLine(input, Jobs, CurrentSettings, Vue);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks; // Ajouté pour le Task
using EasySave.Models;
using EasySave.Services;
using EasySave.Core;
using static System.Reflection.Metadata.BlobBuilder;

namespace EasyLibrary.ViewModels;

public class MainViewModel
{
    private readonly ConfigService _config = new();
    private readonly JobManager _manager = new();
    private readonly BackupService _backup;
    private readonly BusinessSoftwareWatcher _watcher;

    public Action<string>? DisplayMessage { get; set; }

    public LanguageService Language { get; } = new();

    // Propriété utilisée par SettingsUI
    public Settings Settings => _config.Current;

    public ObservableCollection<BackupJob> Jobs { get; }

    public MainViewModel()
    {
        _config.Load();
        Language.Load(Settings.Language);
        Jobs = new ObservableCollection<BackupJob>(_manager.Load());

        var crypto = new CryptoService("CryptoSoft.exe", "MY_KEY");
        _backup = new BackupService(_config, crypto);

        _watcher = new BusinessSoftwareWatcher(_config);
        InitializeWatcher();
    }
    private void InitializeWatcher()
    {
        _watcher.OnSoftwareDetected = () =>
        {
            _backup.PauseAll();
            DisplayMessage?.Invoke(Language.Get("Software_Detected"));
        };
        _watcher.OnSoftwareClosed = () =>
        {
            _backup.ResumeAll();
            DisplayMessage?.Invoke(Language.Get("Software_Closed"));
        };
        _watcher.Start();
    }

    public void PauseJob(string jobName)
    {
        var job = Jobs.FirstOrDefault(j => j.Name.Equals(jobName, StringComparison.OrdinalIgnoreCase));
        if (job != null) _backup.PauseJob(job);
    }

    public void ResumeJob(string jobName)
    {
        var job = Jobs.FirstOrDefault(j => j.Name.Equals(jobName, StringComparison.OrdinalIgnoreCase));
        if (job != null) _backup.ResumeJob(job);
    }

    public void StopJob(string jobName)
    {
        var job = Jobs.FirstOrDefault(j => j.Name.Equals(jobName, StringComparison.OrdinalIgnoreCase));
        if (job != null) _backup.StopJob(job);
    }

    // --- MÉTHODES DE PILOTAGE GLOBAL ---
    public void PauseAllJobs() => _backup.PauseAll();
    public void ResumeAllJobs() => _backup.ResumeAll();
    public void StopAllJobs() => _backup.StopAll();

    // Méthode appelée par SettingsUI
    public void SaveSettings()
    {
        _config.Save();
    }

    public void AddJob(string n, string s, string t, BackupType ty)
    {
        if (string.IsNullOrWhiteSpace(n) || !System.IO.Directory.Exists(s)) return;
        var job = new BackupJob { Name = n, SourceDir = s, TargetDir = t, Type = ty };
        Jobs.Add(job);
        _manager.Save(Jobs.ToList());
    }

    public void DeleteJob(int index)
    {
        if (index >= 0 && index < Jobs.Count)
        {
            Jobs.RemoveAt(index);
            _manager.Save(Jobs.ToList());
        }
    }

    public void ClearAllJobs()
    {
        Jobs.Clear();
        _manager.Save(Jobs.ToList());
    }

    public void SetLanguage(string langCode)
    {
        Settings.Language = langCode;
        _config.Save();
        Language.Load(langCode);
    }

    public void SwitchLogFormat()
    {
        Settings.LogFormat = (Settings.LogFormat == LogFormat.Json)
            ? LogFormat.Xml
            : LogFormat.Json;
        _config.Save();
    }

    // Passé en async Task pour supporter le logger TCP persistant
    public async Task Execute(string input)
    {
        var selected = JobParser.ParseSelection(input, Jobs.Count);

        if (!selected.Any()) return;

        var jobsToRun = selected.Select(i => Jobs[i]).ToList();

        //ancienne logique
        //foreach (var i in selected)
        //{
        
        await _backup.Execute(jobsToRun, state => {
                // Progression
        });
        
    }

    public void ManageEncryptionExtensions(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return;

        extension = extension.Trim().ToLower();
        if (!extension.StartsWith(".")) extension = "." + extension;

        
        if (!Settings.EncryptionExtensions.Contains(extension))
        {
            Settings.EncryptionExtensions.Add(extension);
            _config.Save();
        }
    }

    public void UpdateBusinessSoftware(string name)
    {
        Settings.BusinessSoftware = name;
        _config.Save();
    }
} 
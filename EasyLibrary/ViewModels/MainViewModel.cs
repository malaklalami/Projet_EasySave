using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
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
    public Action<BackupState>? OnProgressUpdate { get; set; }
    public ObservableCollection<BackupJob> Jobs { get; }

    public MainViewModel()
    {
        _config.Load();
        Language.Load(Settings.Language);
        Jobs = new ObservableCollection<BackupJob>(_manager.Load());

        string cryptoExe = OperatingSystem.IsWindows() ? "CryptoSoft.exe" : "CryptoSoft";
        var crypto = new CryptoService(cryptoExe, "MY_KEY");
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

    private BackupJob? FindJob(string input)
    {
        if (int.TryParse(input, out int index))
        {
            return (index >= 0 && index < Jobs.Count) ? Jobs[index] : null;
        }
        return Jobs.FirstOrDefault(j => j.Name.Equals(input, StringComparison.OrdinalIgnoreCase));
    }

    public void PauseJob(string input)
    {
        var job = FindJob(input);
        if (job != null) _backup.PauseJob(job);
    }

    public void ResumeJob(string input)
    {
        var job = FindJob(input);
        if (job != null) _backup.ResumeJob(job);
    }

    public void StopJob(string input)
    {
        var job = FindJob(input);
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


   

    public async Task Execute(string input)
    {
        // On transforme le texte en liste de travaux (Étape 1 du BackupService)
        var selected = JobParser.ParseSelection(input, Jobs.Count);
        if (!selected.Any()) return;
        var jobsToRun = selected.Select(i => Jobs[i]).ToList();

        // ON PASSE LE RELAIS AU BACKUPSERVICE !
        // - On lui donne la liste (jobsToRun)
        // - On lui donne la fonction pour qu'il nous renvoie l'état en direct
        await _backup.Execute(jobsToRun, (BackupState state) =>
        {
            OnProgressUpdate?.Invoke(state);
        });
    }
} 



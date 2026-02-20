using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks; // Ajouté pour le Task
using EasySave.Models;
using EasySave.Services;
using EasySave.Core;

namespace EasyLibrary.ViewModels;

public class MainViewModel
{
    private readonly ConfigService _config = new();
    private readonly JobManager _manager = new();
    private readonly BackupService _backup;

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
    }

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
        foreach (var i in selected)
        {
            // On ajoute 'await' ici
            await _backup.Execute(Jobs[i], state => {
                // Progression
            });
        }
    }

    public void ManageEncryptionExtensions(string extension)
    {
        if (!extension.StartsWith(".")) extension = "." + extension;
        extension = extension.ToLower();

        if (Settings.EncryptionExtensions.Contains(extension))
        {
            Settings.EncryptionExtensions.Remove(extension);
        }
        else
        {
            Settings.EncryptionExtensions.Add(extension);
        }
        _config.Save();
    }

    public void UpdateBusinessSoftware(string name)
    {
        Settings.BusinessSoftware = name;
        _config.Save();
    }
}
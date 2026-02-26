using EasyLibrary.Core;
using EasySave.Core;
using EasySave.Models;
using EasySave.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EasySave.ViewModels;

public class MainViewModel
{
    private readonly ConfigService _config = new();
    private readonly JobManager _manager = new();
    private readonly BackupService _backup;
    private readonly BusinessSoftwareWatcher _watcher;

    public Settings Settings => _config.Current;
    public ObservableCollection<BackupJob> Jobs { get; }
    public Action<BackupState>? OnProgressUpdate { get; set; }

    public MainViewModel()
    {
        // 1. Chargement
        _config.Load();
        Jobs = new ObservableCollection<BackupJob>(_manager.Load());

        // 2. Setup CryptoSoft
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string cryptoExe = OperatingSystem.IsWindows() ? "CryptoSoft.exe" : "CryptoSoft";
        var cryptoService = new CryptoService(Path.Combine(baseDir, cryptoExe), "MY_KEY");

        // 3. Setup Moteur de Backup
        _backup = new BackupService(_config, cryptoService);
        _backup.OnProgress += (state) => OnProgressUpdate?.Invoke(state);

        // 4. Setup Surveillance Logiciel Métier
        _watcher = new BusinessSoftwareWatcher(_config);
        _watcher.Start();
    }

    // --- PILOTAGE (Lien direct avec tes boutons UI) ---

    public async Task ExecuteSelection(string input)
    {
        // On récupère les jobs demandés (ex: "1-3")
        var selectedIndices = JobParser.ParseSelection(input, Jobs.Count);
        var jobsToRun = selectedIndices.Select(i => Jobs[i]).ToList();

        if (jobsToRun.Any())
        {
            await _backup.Execute(jobsToRun);
        }
    }

    // Méthodes Hyper Simples pour les boutons Pause/Play/Stop
    public void PauseAll() => JobControlService.Pause();
    public void ResumeAll() => JobControlService.Resume();
    public void StopAll() => JobControlService.Stop();

    // --- GESTION DES JOBS ---

    public void AddJob(string name, string src, string dest, BackupType type)
    {
        int nextId = Jobs.Count > 0 ? Jobs.Max(j => j.Id) + 1 : 1;
        var job = new BackupJob { Id = nextId, Name = name, SourceDir = src, TargetDir = dest, Type = type };
        Jobs.Add(job);
        _manager.Save(Jobs.ToList());
    }

    public void DeleteJob(BackupJob job)
    {
        Jobs.Remove(job);
        _manager.Save(Jobs.ToList());
    }

    // --- CONFIGURATION ---

    public void SaveSettings() => _config.Save();

    public void UpdateLanguage(string lang)
    {
        Settings.Language = lang;
        _config.Save();
    }
}
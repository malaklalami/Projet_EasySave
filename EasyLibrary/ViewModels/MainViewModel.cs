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

// Fait le lien entre l'interface utilisateur et la logique métier en coordonnant tous les services.

public class MainViewModel
{
    private readonly ConfigService _config = new();
    private readonly JobManager _manager = new();
    private readonly BackupService _backup;
    private readonly BusinessSoftwareWatcher _watcher;

    public Action<string>? DisplayMessage { get; set; }

    public LanguageService LanguageService { get; } = new();
    public ErrorService ErrorService { get; private set; }

    // Propriété utilisée par SettingsUI
    public Settings Settings => _config.Current;
    public Action<BackupState>? OnProgressUpdate { get; set; }
    public ObservableCollection<BackupJob> Jobs { get; }

    public double ProgressPercentage { get; private set; }

    public Action<Action> UIWrapper { get; set; } = (a) => { a(); };

    public MainViewModel()
    {
        // 1. Chargement de la configuration et de la langue
        _config.Load();
        LanguageService.Load(Settings.Language);
        ErrorService = new(LanguageService);
        Jobs = new ObservableCollection<BackupJob>(_manager.Load());

        // 2. Préparation du chemin vers CryptoSoft 
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string cryptoExeName = OperatingSystem.IsWindows() ? "CryptoSoft.exe" : "CryptoSoft";
        string cryptoFullPath = Path.Combine(baseDir, cryptoExeName);

        // 3. Initialisation UNIQUE du service de cryptage
        var cryptoServiceInstance = new CryptoService(cryptoFullPath, "MY_KEY");

        // 4. Initialisation du service de backup avec l'instance de cryptage
        _backup = new BackupService(_config, cryptoServiceInstance);
        _backup.OnProgress += (state) =>
        {
            UIWrapper(() =>
            {
                ProgressPercentage = state.Progress;
            });
        };

        // 5. Lancement du watcher de logiciel métier
        _watcher = new BusinessSoftwareWatcher(_config);
        InitializeWatcher();
    }


    private void InitializeWatcher()
    {
        _watcher.OnSoftwareDetected = () =>
        {
            // 1. On bloque la sauvegarde
            _backup.PauseAll();

            // 2. On utilise ErrorService pour FORCER l'affichage du popup
            // On passe le type "BusinessSoftwareActive"
            ErrorService.Report(ErrorType.BusinessSoftwareActive, Settings.BusinessSoftware);
        };

        _watcher.OnSoftwareClosed = () =>
        {
            // 1. On reprend la sauvegarde
            _backup.ResumeAll();

            // 2. Simple info de reprise (pas forcément un popup bloquant)
            DisplayMessage?.Invoke(LanguageService.Get("Software_Closed"));
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
        LanguageService.Load(langCode);
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

    public void UpdateJob(int index, string n, string s, string t, BackupType ty)
    {
        // 1. On demande au service de modifier l'objet dans la liste
        _backup.UpdateJobInList(Jobs.ToList(), index, n, s, t, ty);

        // 2. On force la mise à jour visuelle (au cas où)
        var updatedJob = Jobs[index];
        // Optionnel : on peut notifier ici si nécessaire

        // 3. On enregistre les modifications dans le fichier JSON
        _manager.Save(Jobs.ToList());
    }



    public async Task Execute(string input)
    {
        // On transforme le texte en liste de travaux (Étape 1 du BackupService)
        var selected = JobParser.ParseSelection(input, Jobs.Count);
        if (!selected.Any()) return;
        var jobsToRun = selected.Select(i => Jobs[i]).ToList();

        // ON PASSE LE RELAIS AU BACKUPSERVICE 
        // - On lui donne la liste (jobsToRun)
        // - On lui donne la fonction pour qu'il nous renvoie l'état en direct
        await _backup.Execute(jobsToRun, (BackupState state) =>
        {
            OnProgressUpdate?.Invoke(state);
        });
    }
}



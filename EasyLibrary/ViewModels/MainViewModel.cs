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
    // --- Services Privés ---
    private readonly ConfigService _config = new();
    private readonly JobManager _manager = new();
    private readonly BackupService _backup;
    private readonly BusinessSoftwareMonitor _monitor;

    public Action<bool>? OnSoftwareDetectionEvent { get; set; }

    private readonly LoggerService _logger = new();
    private readonly StateService _state = new();
    private readonly BackupReportingService _reporter;

    // Événement pour notifier les UI quand la langue change
    public Action? OnLanguageChanged { get; set; }

    // --- Propriétés Publiques ---
    public LanguageService LanguageService { get; } = new();
    public Settings Settings => _config.Current;
    public StateService State => _state;
    public ObservableCollection<BackupJob> Jobs { get; }

    // Événement pour que l'UI (Console/WPF) reçoive la progression
    public Action<BackupState>? OnProgressUpdate { get; set; }

    public MainViewModel()
    {
        // 1. Chargement de la configuration globale
        _config.Load();

        // 2. Initialisation de la langue (basée sur les settings)
        LanguageService.Load(Settings.Language);

        // 3. Initialisation du moniteur de logiciel métier
        _monitor = new BusinessSoftwareMonitor(_config);

        // On dit au ViewModel : "Dès que le moniteur détecte un changement, préviens l'UI"
        _monitor.OnSoftwareDetectionChanged += (detected) => OnSoftwareDetectionEvent?.Invoke(detected);

        // 4. Setup du service de chiffrement (CryptoSoft)
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string cryptoExe = OperatingSystem.IsWindows() ? "CryptoSoft.exe" : "CryptoSoft";
        string cryptoPath = Path.Combine(baseDir, cryptoExe);
        
        // Debug: vérifier si CryptoSoft.exe existe
        System.Diagnostics.Debug.WriteLine($"[DEBUG] CryptoSoft recherché à: {cryptoPath}");
        System.Diagnostics.Debug.WriteLine($"[DEBUG] CryptoSoft existe: {File.Exists(cryptoPath)}");
        
        // Si CryptoSoft n'existe pas au chemin principal, chercher dans les dossiers bin/Release/net8.0
        if (!File.Exists(cryptoPath))
        {
            // Chercher dans: ...\CryptoSoft\bin\Release\net8.0\win-x64\CryptoSoft.exe
            string alternativePath = Path.Combine(baseDir, "..", "..", "CryptoSoft", "bin", "Release", "net8.0", "win-x64", cryptoExe);
            if (File.Exists(alternativePath))
            {
                cryptoPath = alternativePath;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] CryptoSoft trouvé à: {cryptoPath}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] CryptoSoft INTROUVABLE!");
            }
        }

        // On utilise la clé définie dans les settings (ou une clé fixe "MY_KEY")
        var cryptoService = new CryptoService(cryptoPath, "MY_KEY");

        // 5. Initialisation du moteur de backup (Injection des 3 dépendances)
        _backup = new BackupService(_config, cryptoService, _monitor);

        // On connecte le reporter au moteur pour qu'il écrive les logs et le state.json !
        _reporter = new BackupReportingService(_backup, _logger, _state, _config);

        // On s'abonne aux mises à jour du moteur pour les renvoyer à l'UI
        _backup.OnProgress += (state) => OnProgressUpdate?.Invoke(state);

        // 6. Chargement de la liste des travaux
        Jobs = new ObservableCollection<BackupJob>(_manager.Load());
    }

    // --- MÉTHODES D'EXÉCUTION ---

    public async Task ExecuteSelection(string input)
    {
        // On transforme la saisie (ex: "1-3") en liste d'index
        var selectedIndices = JobParser.ParseSelection(input, Jobs.Count);

        var jobsToRun = selectedIndices.Select(i => Jobs[i]).ToList();

        if (jobsToRun.Any())
        {
            // On réinitialise les signaux de contrôle avant de partir
            JobControlService.Reset();

            // On utilise AddJobs au lieu de Execute
            _backup.AddJobs(jobsToRun);
            await Task.CompletedTask;
        }
    }

    // --- MÉTHODES DE CONTRÔLE (V3) ---

    public void PauseAll() => JobControlService.PauseAll();
    public void ResumeAll() => JobControlService.ResumeAll(Jobs);

    public void StopAll()
    {
        JobControlService.StopAll();
        _backup.ForcePulse();
    }

    public void PauseJob(BackupJob job) => JobControlService.Pause(job);
    public void ResumeJob(BackupJob job) => JobControlService.Resume(job);
    public void StopJob(BackupJob job) => JobControlService.Stop(job);
    // --- GESTION DES TRAVAUX (CRUD) ---

    public void AddJob(string name, string src, string dest, BackupType type)
    {
        // Calcul automatique du prochain ID
        int nextId = Jobs.Count > 0 ? Jobs.Max(j => j.Id) + 1 : 1;

        var job = new BackupJob
        {
            Id = nextId,
            Name = name,
            SourceDir = src,
            TargetDir = dest,
            Type = type,
            IsPaused = false
        };

        Jobs.Add(job);
        _manager.Save(Jobs.ToList());
    }

    public void EditJob(BackupJob jobToEdit, string newName, string newSrc, string newDest, BackupType newType)
    {
        if (jobToEdit == null) return;

        // 1. On met à jour les propriétés de l'objet existant
        jobToEdit.Name = newName;
        jobToEdit.SourceDir = newSrc;
        jobToEdit.TargetDir = newDest;
        jobToEdit.Type = newType;

        // 2. On sauvegarde la liste complète mise à jour dans le fichier (ex: jobs.json)
        _manager.Save(Jobs.ToList());

        // Note : Si l'UI (WPF) ne se rafraîchit pas toute seule quand on modifie un job, 
        // on peut forcer le rafraîchissement en remplaçant l'objet dans la liste :
        // int index = Jobs.IndexOf(jobToEdit);
        // Jobs[index] = jobToEdit; 
    }
    public void UpdateLogTarget(int choice)
    {
        // On mappe le choix numérique de la console vers l'Enum
        Settings.LogTarget = choice switch
        {
            1 => LogTarget.Local,
            2 => LogTarget.Remote,
            3 => LogTarget.Both,
            _ => LogTarget.Local
        };

        _config.Save(); // On sauvegarde dans settings.json
    }

    public void DeleteJob(BackupJob job)
    {
        Jobs.Remove(job);
        _manager.Save(Jobs.ToList());
    }

    public void ClearAllJobs()
    {
        Jobs.Clear();
        _manager.Save(new List<BackupJob>());
    }

    // --- CONFIGURATION ---

    public void SaveSettings() => _config.Save();

    public void UpdateLanguage(string lang)
    {
        Settings.Language = lang;
        _config.Save();
        LanguageService.Load(lang); // Recharge immédiatement les fichiers JSON
        OnLanguageChanged?.Invoke(); // Notifie les UI de mettre à jour les traductions
    }

    public void SwitchLogFormat()
    {
        Settings.LogFormat = (Settings.LogFormat == LogFormat.Json) ? LogFormat.Xml : LogFormat.Json;
        _config.Save();
    }

    public void ManageEncryptionExtensions(string ext) => _config.ManageEncryptionExtension(ext);


    public void UpdateBusinessSoftware(string name) => _config.UpdateBusinessSoftware(name);

}
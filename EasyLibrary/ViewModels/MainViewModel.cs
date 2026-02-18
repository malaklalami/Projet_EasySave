using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EasySave.Models;
using EasySave.Services;
using EasySave.Core;

namespace EasyLibrary.ViewModels;

public class MainViewModel
{
    private readonly ConfigService _config = new();
    private readonly JobManager _manager = new();
    private readonly BackupService _backup;

    // Cette propriété manquait (nécessaire pour la console)
    public Settings CurrentSettings => _config.Current;

    public ObservableCollection<BackupJob> Jobs { get; }

    public MainViewModel()
    {
        _config.Load();
        Jobs = new ObservableCollection<BackupJob>(_manager.Load());
        // On passe le ConfigService au BackupService pour le cache logiciel métier
        var crypto = new CryptoService("CryptoSoft.exe", "MY_KEY");
        _backup = new BackupService(_config, crypto);
    }

    public void AddJob(string n, string s, string t, BackupType ty)
    {
        if (string.IsNullOrWhiteSpace(n) || !System.IO.Directory.Exists(s)) return;
        var job = new BackupJob { Name = n, SourceDir = s, TargetDir = t, Type = ty };
        Jobs.Add(job);
        _manager.Save(Jobs.ToList());
    }

    // Cette méthode manquait
    public void DeleteJob(int index)
    {
        if (index >= 0 && index < Jobs.Count)
        {
            Jobs.RemoveAt(index);
            _manager.Save(Jobs.ToList());
        }
    }

    // Cette méthode manquait
    public void SwitchLanguage()
    {
        _config.Current.Language = (_config.Current.Language == "fr") ? "en" : "fr";
        _config.Save();
    }

    public void Execute(string input)
    {
        var selected = JobParser.ParseSelection(input, Jobs.Count);
        foreach (var i in selected)
        {
            _backup.Execute(Jobs[i], state => {
                // Optionnel : Console.WriteLine($"Progression {state.JobName}: {state.Progress}%");
            });
        }
    }

    public void ManageEncryptionExtensions(string extension)
    {
        // On normalise (ex: "txt" devient ".txt")
        if (!extension.StartsWith(".")) extension = "." + extension;
        extension = extension.ToLower();

        if (_config.Current.EncryptionExtensions.Contains(extension))
        {
            _config.Current.EncryptionExtensions.Remove(extension);
            Console.WriteLine($"[LOG] {extension} retiré de la liste de cryptage.");
        }
        else
        {
            _config.Current.EncryptionExtensions.Add(extension);
            Console.WriteLine($"[LOG] {extension} ajouté à la liste de cryptage.");
        }
        _config.Save(); // On enregistre dans settings.json
    }
}
//Il valide si les données sont correctes (dossiers existants, noms valides)

//Il utilise le JobParser pour comprendre la sélection

//Il lance le BackupService

//Il met à jour l'interface via le système de notification (Action ou Binding)
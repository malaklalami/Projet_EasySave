using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EasyAvalonia.ViewModels;
using EasySave.ViewModels;
using EasySave.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EasyAvalonia.Views;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public MainViewModel BackendVM { get; set; }
    public ObservableCollection<JobDisplayModel> DisplayJobs { get; set; } = new();

    public new event PropertyChangedEventHandler? PropertyChanged;

    private bool _isAnyJobRunning;
    public bool IsAnyJobRunning
    {
        get => _isAnyJobRunning;
        set { _isAnyJobRunning = value; NotifyPropertyChanged(nameof(IsAnyJobRunning)); }
    }

    public MainWindow()
    {
        InitializeComponent();

        // 1. Initialisation du ViewModel
        BackendVM = new MainViewModel();

        // 2. Branchement des mises à jour de progression
        BackendVM.OnProgressUpdate = (state) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var targetJob = BackendVM.Jobs.FirstOrDefault(j => j.Id == state.JobId);
                if (targetJob == null) return;

                var targetModel = DisplayJobs.FirstOrDefault(j => j.Job.Id == state.JobId);
                if (targetModel != null)
                {
                    double progress = state.TotalFilesCount > 0 
                        ? (100.0 * (state.TotalFilesCount - state.FilesToCopy.Count) / state.TotalFilesCount)
                        : 0;
                    
                    targetModel.Progress = Math.Min(progress, 100);
                    targetModel.IsPaused = (state.Status == JobState.Paused);
                    targetModel.Status = state.Status.ToString();
                    UpdateGlobalRunningStatus();
                }
            });
        };

        // 3. Branchement de la détection de logiciel métier
        BackendVM.OnSoftwareDetectionEvent += (isDetected) =>
        {
            Dispatcher.UIThread.Post(async () =>
            {
                string msg = isDetected
                    ? "Logiciel métier détecté"
                    : "Logiciel métier fermé";
                await ShowInfoPopup(msg);
            });
        };

        // 4. Remplissage initial de la liste et DataContext
        RefreshJobList();
        DataContext = this;
    }

    private async Task ShowInfoPopup(string message)
    {
        var messageBox = new Window
        {
            Title = BackendVM.LanguageService.Get("Title_Alert") ?? "EasySave",
            Content = new TextBlock { Text = message, Margin = new Avalonia.Thickness(20), TextWrapping = Avalonia.Media.TextWrapping.Wrap },
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            MinWidth = 300
        };
        await messageBox.ShowDialog(this);
    }

    private void RefreshJobList()
    {
        DisplayJobs.Clear();
        
        // Charger les états sauvegardés du fichier state.json
        var savedStates = BackendVM.State.ReadStates();
        System.Diagnostics.Debug.WriteLine($"[DEBUG] Nombre d'états chargés: {savedStates.Count}");
        
        foreach (var job in BackendVM.Jobs)
        {
            var displayModel = new JobDisplayModel { Job = job };
            
            // Chercher l'état sauvegardé pour ce job
            var savedState = savedStates.FirstOrDefault(s => s.JobId == job.Id);
            if (savedState != null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] État trouvé pour Job {job.Name} (ID {job.Id}): Status={savedState.Status}, FilesToCopy={savedState.FilesToCopy.Count}");
                
                // Calculer la progression basée sur les fichiers restants
                double progress = savedState.TotalFilesCount > 0 
                    ? (100.0 * (savedState.TotalFilesCount - savedState.FilesToCopy.Count) / savedState.TotalFilesCount)
                    : 0;
                
                displayModel.Progress = Math.Min(progress, 100);
                displayModel.Status = savedState.Status.ToString();
                displayModel.IsPaused = (savedState.Status == JobState.Paused);
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Progress calculée: {progress:F1}%");
                
                // Indiquer que c'est une reprise si la progression > 0 et que ce n'est pas terminé
                if (progress > 0 && progress < 100 && savedState.Status != JobState.Inactive)
                {
                    displayModel.IsResuming = true;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] Aucun état trouvé pour Job {job.Name} (ID {job.Id})");
            }
            
            DisplayJobs.Add(displayModel);
        }
        UpdateGlobalRunningStatus();
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void UpdateGlobalRunningStatus()
    {
        IsAnyJobRunning = DisplayJobs.Any(j => j.IsRunning);
    }

    private async void RunSelected_Click(object sender, RoutedEventArgs e)
    {
        var selectedIndices = DisplayJobs
             .Select((model, index) => new { model, index })
             .Where(x => x.model.IsSelected)
             .Select(x => x.index.ToString());

        string inputString = string.Join(",", selectedIndices);
        if (string.IsNullOrEmpty(inputString))
        {
            await ShowInfoPopup("Veuillez selectionner au moins un travail");
            return;
        }

        // Verification que les repertoires source existent
        var selectedJobs = DisplayJobs
            .Where(x => x.IsSelected)
            .Select(x => x.Job)
            .ToList();

        var invalidJobs = selectedJobs
            .Where(j => !System.IO.Directory.Exists(j.SourceDir))
            .ToList();

        if (invalidJobs.Any())
        {
            string invalidNames = string.Join(", ", invalidJobs.Select(j => j.Name));
            await ShowInfoPopup($"Erreur: Le(s) repertoire(s) source n'existe(nt) pas:\n{invalidNames}");
            return;
        }

        // Verification que les repertoires cible existent ou peuvent etre crees
        var creationFailures = new List<string>();
        foreach (var job in selectedJobs)
        {
            try
            {
                System.IO.Directory.CreateDirectory(job.TargetDir);
            }
            catch
            {
                creationFailures.Add(job.Name);
            }
        }

        if (creationFailures.Any())
        {
            string failedNames = string.Join(", ", creationFailures);
            await ShowInfoPopup($"Erreur: Impossible de creer les repertoires cibles:\n{failedNames}");
            return;
        }

        await BackendVM.ExecuteSelection(inputString);
    }

    private async void EditJob_Click(object sender, RoutedEventArgs e)
    {
        // Récupérer le job à partir du DataContext du bouton
        JobDisplayModel target = null;
        
        if (sender is Button btn && btn.DataContext is JobDisplayModel m)
        {
            target = m;
        }
        else
        {
            // Fallback: utiliser la sélection (pour rester compatible)
            target = DisplayJobs.FirstOrDefault(x => x.IsSelected);
        }
        
        if (target == null) return;

        var dialog = new CreateJobWindow();
        dialog.LoadJobData(target.Job.Name, target.Job.SourceDir, target.Job.TargetDir, target.Job.Type);

        await dialog.ShowDialog(this);

        if (dialog.IsConfirmed)
        {
            BackendVM.EditJob(target.Job, dialog.JobName, dialog.Source, dialog.Target, dialog.Type);
            RefreshJobList();
        }
    }

    private void DeleteJob_Click(object sender, RoutedEventArgs e)
    {
        var target = DisplayJobs.FirstOrDefault(x => x.IsSelected);
        if (target != null)
        {
            BackendVM.DeleteJob(target.Job);
            RefreshJobList();
        }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        BackendVM.ClearAllJobs();
        RefreshJobList();
    }

    private async void CreateJob_Click(object sender, RoutedEventArgs e)
    {
        var d = new CreateJobWindow();
        await d.ShowDialog(this);
        if (d.IsConfirmed)
        {
            BackendVM.AddJob(d.JobName, d.Source, d.Target, d.Type);
            RefreshJobList();
        }
    }

    private async void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        await new SettingsWindow(BackendVM).ShowDialog(this);
        RefreshJobList();
    }

    private void PauseAll_Click(object sender, RoutedEventArgs e) => BackendVM.PauseAll();
    private void ResumeAll_Click(object sender, RoutedEventArgs e) => BackendVM.ResumeAll();
    
    private void StopAll_Click(object sender, RoutedEventArgs e)
    {
        BackendVM.StopAll();
        // Reset la progression de tous les jobs
        foreach (var displayJob in DisplayJobs)
        {
            displayJob.Progress = 0;
            displayJob.Status = JobState.Inactive.ToString();
        }
        UpdateGlobalRunningStatus();
    }

    private void PauseJob_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is JobDisplayModel m)
            BackendVM.PauseJob(m.Job);
    }

    private void ResumeJob_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is JobDisplayModel m)
            BackendVM.ResumeJob(m.Job);
    }

    private void StopJob_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is JobDisplayModel m)
        {
            BackendVM.StopJob(m.Job);
            // Reset la progression du job arrêté
            m.Progress = 0;
            m.Status = JobState.Inactive.ToString();
            UpdateGlobalRunningStatus();
        }
    }
}
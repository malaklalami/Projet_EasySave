using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EasyAvalonia.ViewModels;
using EasyLibrary.ViewModels;
using EasySave.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace EasyAvalonia.Views;

// Fenêtre principale de l'interface graphique assurant le pilotage global des sauvegardes et l'affichage des alertes.
// Synchronise le moteur de sauvegarde avec l'affichage via le thread UI pour mettre à jour la progression sans bloquer l'interface.

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

        // 2. Branchement unique des erreurs (centralisé)
        ErrorService.OnErrorDetected += (s, e) =>
        {
            // Dispatcher.UIThread.Post est la clé pour ne pas freezer l'interface
            Dispatcher.UIThread.Post(async () =>
            {
                await ShowErrorPopup(e.Message);
            });
        };

        // 3. Branchement du logger / messages système
        BackendVM.DisplayMessage = (msg) =>
        {
            Dispatcher.UIThread.Post(async () =>
            {
                // On affiche aussi les messages système (Logiciel détecté, etc.) en popup
                await ShowErrorPopup(msg);
            });
        };

        // 4. Remplissage initial de la liste et DataContext
        RefreshJobList();
        DataContext = this;
    }

    // Méthode utilitaire pour afficher un popup sans bloquer le thread principal
    private async Task ShowErrorPopup(string message)
    {
        var messageBox = new Window
        {
            Title = BackendVM.Language.Get("Title_Alert") ?? "EasySave Message",
            Content = new TextBlock { Text = message, Margin = new Avalonia.Thickness(20), TextWrapping = Avalonia.Media.TextWrapping.Wrap },
            SizeToContent = SizeToContent.WidthAndHeight,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            MinWidth = 300
        };
        await messageBox.ShowDialog(this);
    }

    // --- LOGIQUE DE MISE À JOUR DE L'INTERFACE ---

    private void RefreshJobList()
    {
        DisplayJobs.Clear();
        foreach (var job in BackendVM.Jobs)
        {
            DisplayJobs.Add(new JobDisplayModel { Job = job });
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

    // --- ACTIONS DES BOUTONS ---

    private async void RunSelected_Click(object sender, RoutedEventArgs e)
    {
        var selectedIndices = DisplayJobs
             .Select((model, index) => new { model, index })
             .Where(x => x.model.IsSelected)
             .Select(x => x.index.ToString());

        string inputString = string.Join(";", selectedIndices);
        if (string.IsNullOrEmpty(inputString)) return;

        BackendVM.OnProgressUpdate = (state) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var targetModel = DisplayJobs.FirstOrDefault(j => j.Job.Name == state.JobName);
                if (targetModel != null)
                {
                    targetModel.Progress = state.Progress;
                    targetModel.IsPaused = (state.Status == EasySave.Core.JobState.Paused);

                    if (state.Progress < 100 && state.Progress > 0)
                        targetModel.CurrentActionText = $"⚡ {state.CurrentFile}";
                    else if (state.Progress >= 100)
                        targetModel.CurrentActionText = "✅ Terminé";

                    targetModel.Status = state.Status.ToString();
                    UpdateGlobalRunningStatus();
                }
            });
        };

        await BackendVM.Execute(inputString);
    }

    private async void EditJob_Click(object sender, RoutedEventArgs e)
    {
        var target = DisplayJobs.FirstOrDefault(x => x.IsSelected);
        if (target == null) return;

        var dialog = new CreateJobWindow();
        dialog.LoadJobData(target.Job.Name, target.Job.SourceDir, target.Job.TargetDir, target.Job.Type);

        await dialog.ShowDialog(this);

        if (dialog.IsConfirmed)
        {
            int index = BackendVM.Jobs.IndexOf(target.Job);
            BackendVM.UpdateJob(index, dialog.JobName, dialog.Source, dialog.Target, dialog.Type);
            RefreshJobList();
        }
    }

    private void DeleteJob_Click(object sender, RoutedEventArgs e)
    {
        var target = DisplayJobs.FirstOrDefault(x => x.IsSelected);
        if (target != null)
        {
            BackendVM.DeleteJob(BackendVM.Jobs.IndexOf(target.Job));
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
    }

    // --- PILOTAGE GLOBAL ---
    private void PauseAll_Click(object sender, RoutedEventArgs e) => BackendVM.PauseAllJobs();
    private void ResumeAll_Click(object sender, RoutedEventArgs e) => BackendVM.ResumeAllJobs();
    private void StopAll_Click(object sender, RoutedEventArgs e) => BackendVM.StopAllJobs();

    // --- PILOTAGE INDIVIDUEL ---
    private void PauseJob_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is JobDisplayModel m)
            BackendVM.PauseJob(m.Job.Name);
    }

    private void ResumeJob_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is JobDisplayModel m)
            BackendVM.ResumeJob(m.Job.Name);
    }

    private void StopJob_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is JobDisplayModel m)
            BackendVM.StopJob(m.Job.Name);
    }
}
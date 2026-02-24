using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EasyLibrary.ViewModels;
using EasyAvalonia.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasyAvalonia.Views;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public MainViewModel BackendVM { get; set; }
    public ObservableCollection<JobDisplayModel> DisplayJobs { get; set; } = new();

    // Notification pour l'interface graphique
    public new event PropertyChangedEventHandler? PropertyChanged;

    private bool _isAnyJobRunning;
    public bool IsAnyJobRunning
    {
        get => _isAnyJobRunning;
        set
        {
            _isAnyJobRunning = value;
            NotifyPropertyChanged(nameof(IsAnyJobRunning));
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        BackendVM = new MainViewModel();
        RefreshJobList();
        DataContext = this;
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void RefreshJobList()
    {
        DisplayJobs.Clear();
        foreach (var job in BackendVM.Jobs)
        {
            DisplayJobs.Add(new JobDisplayModel { Job = job });
        }
    }

    private void UpdateGlobalRunningStatus()
    {
        // On vérifie si au moins un travail est en cours d'exécution
        IsAnyJobRunning = DisplayJobs.Any(j => j.IsRunning);
    }

    private async void RunSelected_Click(object sender, RoutedEventArgs e)
    {
        var selectedIndices = DisplayJobs
             .Select((model, index) => new { model, index })
             .Where(x => x.model.IsSelected)
             .Select(x => x.index.ToString());

        string inputString = string.Join(";", selectedIndices);
        if (string.IsNullOrEmpty(inputString)) return;

        // Configuration du retour visuel vers l'interface
        BackendVM.OnProgressUpdate = (state) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var targetModel = DisplayJobs.FirstOrDefault(j => j.Job.Name == state.JobName);
                if (targetModel != null)
                {
                    targetModel.Progress = state.Progress;

                    // Comparaison avec l'Enum JobState de la librairie Core
                    targetModel.IsPaused = (state.Status == EasySave.Core.JobState.Paused);

                    if (state.Progress < 100 && state.Progress > 0)
                        targetModel.CurrentActionText = $"⚡ Copie en cours : {state.CurrentFile}";
                    else if (state.Progress >= 100)
                        targetModel.CurrentActionText = "✅ Sauvegarde terminée";

                    targetModel.Status = state.Status.ToString();

                    // Mise à jour de la visibilité des boutons globaux
                    UpdateGlobalRunningStatus();
                }
            });
        };

        // Lancement de l'exécution
        await BackendVM.Execute(inputString);
    }

    // --- CONTRÔLES GLOBAUX ---
    private void PauseAll_Click(object sender, RoutedEventArgs e) => BackendVM.PauseAllJobs();
    private void ResumeAll_Click(object sender, RoutedEventArgs e) => BackendVM.ResumeAllJobs();
    private void StopAll_Click(object sender, RoutedEventArgs e) => BackendVM.StopAllJobs();

    // --- CONTRÔLES INDIVIDUELS ---
    private void PauseJob_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is JobDisplayModel model)
            BackendVM.PauseJob(model.Job.Name);
    }

    private void ResumeJob_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is JobDisplayModel model)
            BackendVM.ResumeJob(model.Job.Name);
    }

    private void StopJob_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is JobDisplayModel model)
            BackendVM.StopJob(model.Job.Name);
    }

    // --- GESTION DES TRAVAUX ---

    private async void EditJob_Click(object sender, RoutedEventArgs e)
    {
        var target = DisplayJobs.FirstOrDefault(x => x.IsSelected);
        if (target == null) return;

        var dialog = new CreateJobWindow();

        // Utilisation des bons noms : SourceDir et TargetDir
        dialog.LoadJobData(target.Job.Name, target.Job.SourceDir, target.Job.TargetDir, target.Job.Type);

        await dialog.ShowDialog(this);

        if (dialog.IsConfirmed)
        {
            // 4. PERSISTANCE : On remplace l'ancien par le nouveau dans la librairie
            int index = BackendVM.Jobs.IndexOf(target.Job);
            if (index != -1)
            {
                // On supprime l'ancien et on ajoute le nouveau avec les modifs
                BackendVM.DeleteJob(index);
                BackendVM.AddJob(dialog.JobName, dialog.Source, dialog.Target, dialog.Type);

                // On sauvegarde et on rafraîchit
                RefreshJobList();
            }
        }
    }

    private void DeleteJob_Click(object sender, RoutedEventArgs e)
    {
        var target = DisplayJobs.FirstOrDefault(x => x.IsSelected);
        if (target != null)
        {
            int index = BackendVM.Jobs.IndexOf(target.Job);
            if (index != -1)
            {
                BackendVM.DeleteJob(index);
                RefreshJobList();
            }
        }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        BackendVM.ClearAllJobs();
        RefreshJobList();
    }

    private async void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(BackendVM);
        await settingsWindow.ShowDialog(this);
    }

    private async void CreateJob_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new CreateJobWindow();
        await dialog.ShowDialog(this);

        if (dialog.IsConfirmed)
        {
            BackendVM.AddJob(dialog.JobName, dialog.Source, dialog.Target, dialog.Type);
            RefreshJobList();
        }
    }
}
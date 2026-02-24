using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EasyLibrary.ViewModels;
using EasyAvalonia.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks; // Ajout pour la gestion des Tasks

namespace EasyAvalonia.Views;

public partial class MainWindow : Window
{
    public MainViewModel BackendVM { get; set; }
    public ObservableCollection<JobDisplayModel> DisplayJobs { get; set; } = new();

    public MainWindow()
    {
        InitializeComponent();
        BackendVM = new MainViewModel();
        RefreshJobList();
        DataContext = this;
    }

    private void RefreshJobList()
    {
        DisplayJobs.Clear();
        foreach (var job in BackendVM.Jobs)
        {
            DisplayJobs.Add(new JobDisplayModel { Job = job });
        }
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

                    // Comparaison avec l'Enum JobState de ta librairie
                    targetModel.IsPaused = (state.Status == EasySave.Core.JobState.Paused);

                    if (state.Progress < 100 && state.Progress > 0)
                        targetModel.CurrentActionText = $"⚡ Copie en cours : {state.CurrentFile}";
                    else if (state.Progress >= 100)
                        targetModel.CurrentActionText = "✅ Sauvegarde terminée";

                    targetModel.Status = state.Status.ToString();
                }
            });
        };

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

    // CORRECTION : Ajout du mot-clé 'async' ici !
    private async void EditJob_Click(object sender, RoutedEventArgs e)
    {
        var target = DisplayJobs.FirstOrDefault(x => x.IsSelected);
        if (target == null) return;

        var dialog = new CreateJobWindow();
        // On attend l'interaction utilisateur
        await dialog.ShowDialog(this);

        if (dialog.IsConfirmed)
        {
            int index = BackendVM.Jobs.IndexOf(target.Job);
            BackendVM.DeleteJob(index);
            BackendVM.AddJob(dialog.JobName, dialog.Source, dialog.Target, dialog.Type);

            RefreshJobList();
        }
    }

    private void DeleteJob_Click(object sender, RoutedEventArgs e)
    {
        var target = DisplayJobs.FirstOrDefault(x => x.IsSelected);
        if (target != null)
        {
            // On trouve l'index réel dans la liste du backend
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
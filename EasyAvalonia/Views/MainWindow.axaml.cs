using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using EasyLibrary.ViewModels;
using EasyAvalonia.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;

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

        BackendVM.OnProgressUpdate = (state) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                var targetModel = DisplayJobs.FirstOrDefault(j => j.Job.Name == state.JobName);
                if (targetModel != null)
                {
                    targetModel.Progress = state.Progress;
                    targetModel.Status = $"[{state.Status}] {state.CurrentFile}";
                }
            });
        };

        await BackendVM.Execute(inputString);
    }

    // --- CONTRÔLES GLOBAUX ---
    private void PauseAll_Click(object sender, RoutedEventArgs e) => BackendVM.PauseAllJobs();
    private void ResumeAll_Click(object sender, RoutedEventArgs e) => BackendVM.ResumeAllJobs();
    private void StopAll_Click(object sender, RoutedEventArgs e) => BackendVM.StopAllJobs();

    // --- CONTRÔLES INDIVIDUELS (Corrigés pour la liste) ---
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
 

    private void EditJob_Click(object sender, RoutedEventArgs e) { }

    private void DeleteJob_Click(object sender, RoutedEventArgs e)
    {
        var target = DisplayJobs.Select((model, index) => new { model, index }).FirstOrDefault(x => x.model.IsSelected);
        if (target != null)
        {
            BackendVM.DeleteJob(target.index);
            RefreshJobList();
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
            // On utilise les vraies données saisies !
            BackendVM.AddJob(dialog.JobName, dialog.Source, dialog.Target, dialog.Type);

            // On rafraîchit la liste visuelle
            RefreshJobList();
        }
    }
}
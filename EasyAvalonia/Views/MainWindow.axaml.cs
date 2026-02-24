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
        // Utilisation des noms exacts de ton BackupService : SourceDir et TargetDir
        dialog.LoadJobData(target.Job.Name, target.Job.SourceDir, target.Job.TargetDir, target.Job.Type);

        await dialog.ShowDialog(this);

        if (dialog.IsConfirmed)
        {
            int index = BackendVM.Jobs.IndexOf(target.Job);
            BackendVM.UpdateJob(index, dialog.JobName, dialog.Source, dialog.Target, dialog.Type);
            RefreshJobList();
        }
    }

    // --- BUTTONS BINDINGS ---
    private void PauseAll_Click(object sender, RoutedEventArgs e) => BackendVM.PauseAllJobs();
    private void ResumeAll_Click(object sender, RoutedEventArgs e) => BackendVM.ResumeAllJobs();
    private void StopAll_Click(object sender, RoutedEventArgs e) => BackendVM.StopAllJobs();
    private void PauseJob_Click(object sender, RoutedEventArgs e) { if (sender is Button btn && btn.DataContext is JobDisplayModel m) BackendVM.PauseJob(m.Job.Name); }
    private void ResumeJob_Click(object sender, RoutedEventArgs e) { if (sender is Button btn && btn.DataContext is JobDisplayModel m) BackendVM.ResumeJob(m.Job.Name); }
    private void StopJob_Click(object sender, RoutedEventArgs e) { if (sender is Button btn && btn.DataContext is JobDisplayModel m) BackendVM.StopJob(m.Job.Name); }
    private void DeleteJob_Click(object sender, RoutedEventArgs e) { 
        var target = DisplayJobs.FirstOrDefault(x => x.IsSelected);
        if (target != null) { BackendVM.DeleteJob(BackendVM.Jobs.IndexOf(target.Job)); RefreshJobList(); }
    }
    private void ClearAll_Click(object sender, RoutedEventArgs e) { BackendVM.ClearAllJobs(); RefreshJobList(); }
    private async void OpenSettings_Click(object sender, RoutedEventArgs e) { await new SettingsWindow(BackendVM).ShowDialog(this); }
    private async void CreateJob_Click(object sender, RoutedEventArgs e) {
        var d = new CreateJobWindow();
        await d.ShowDialog(this);
        if (d.IsConfirmed) { BackendVM.AddJob(d.JobName, d.Source, d.Target, d.Type); RefreshJobList(); }
    }
}
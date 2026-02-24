using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage; // Indispensable pour la sélection de dossiers
using EasySave.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EasyAvalonia.Views;

public partial class CreateJobWindow : Window
{
    public string JobName { get; private set; } = "";
    public string Source { get; private set; } = "";
    public string Target { get; private set; } = "";
    public BackupType Type { get; private set; }
    public bool IsConfirmed { get; private set; }

    public CreateJobWindow()
    {
        InitializeComponent();
    }

    private async void SelectSource_Click(object sender, RoutedEventArgs e)
    {
        var folder = await SelectFolder();
        if (folder != null) SourceInput.Text = folder;
    }

    private async void SelectTarget_Click(object sender, RoutedEventArgs e)
    {
        var folder = await SelectFolder();
        if (folder != null) TargetInput.Text = folder;
    }

    private async Task<string?> SelectFolder()
    {
        // Nouvelle méthode Avalonia pour choisir un dossier
        var folders = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Choisir un répertoire",
            AllowMultiple = false
        });

        return folders.Count >= 1 ? folders[0].Path.LocalPath : null;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        JobName = NameInput.Text ?? "";
        Source = SourceInput.Text ?? "";
        Target = TargetInput.Text ?? "";
        Type = TypeCombo.SelectedIndex == 0 ? BackupType.Full : BackupType.Differential;

        if (!string.IsNullOrWhiteSpace(JobName) && !string.IsNullOrWhiteSpace(Source))
        {
            IsConfirmed = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
}
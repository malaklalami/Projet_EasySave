using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

    // Cette méthode permet de pré-remplir les champs lors d'une modification
    public void LoadJobData(string name, string source, string target, BackupType type)
    {
        NameInput.Text = name;
        SourceInput.Text = source;
        TargetInput.Text = target;

        // 0 = Complet (Full), 1 = Différentiel
        TypeCombo.SelectedIndex = (type == BackupType.Full) ? 0 : 1;
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

// Gère la fenêtre de dialogue pour la création et l'édition des travaux de sauvegarde via une interface graphique.
// Intègre des sélecteurs de dossiers natifs et valide les saisies utilisateur (nom, chemins, type) avant confirmation.
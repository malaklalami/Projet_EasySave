using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using EasyLibrary.ViewModels;

namespace EasyAvalonia;

public partial class MainWindow : Window
{
    private MainViewModel _viewModel = new MainViewModel();

    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = _viewModel;

        // Sécurité : On ne lance la logique que si le ViewModel existe
        if (!Avalonia.Controls.Design.IsDesignMode && _viewModel != null)
        {
            _viewModel.Start();
        }
    }

    // NOUVEAU : Sauvegarde des paramètres sans crash
    public void OnSaveParamsClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.CurrentSettings == null) return;

        // On récupère le nom du logiciel métier
        var softBox = this.FindControl<TextBox>("TxtBusinessSoft");
        if (softBox != null) _viewModel.CurrentSettings.BusinessSoftware = softBox.Text;

        // On gère le format de log (JSON/XML)
        var combo = this.FindControl<ComboBox>("ComboLogFormat");
        if (combo != null)
        {
            // Simple bascule si nécessaire
            _viewModel.SwitchLogFormat();
        }

        System.Diagnostics.Debug.WriteLine("Paramètres mis à jour.");
    }

    // Tes méthodes existantes (sécurisées avec des ?)
    public void OnLanguageClick(object sender, RoutedEventArgs e)
    {
        _viewModel?.SwitchLanguage();
        System.Diagnostics.Debug.WriteLine("Action : Changer Langue");
    }

    public void OnBackupClick(object sender, RoutedEventArgs e)
    {
        _viewModel?.ExecuteJob(0);
        System.Diagnostics.Debug.WriteLine("Action : Lancer un Job");
    }

    public void OnSettingsClick(object sender, RoutedEventArgs e) => System.Diagnostics.Debug.WriteLine("Action : Modifier Job");

    public void OnDeleteClick(object sender, RoutedEventArgs e) => System.Diagnostics.Debug.WriteLine("Action : Supprimer Job");

    public void OnAddJobClick(object sender, RoutedEventArgs e) => System.Diagnostics.Debug.WriteLine("Action : Ajouter Job");

    public void OnBackupAllClick(object sender, RoutedEventArgs e) => System.Diagnostics.Debug.WriteLine("Action : Lancer tout");
}
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace EasyAvalonia;

public partial class MainWindow : Window
{
    // On garde notre instance de ViewModel
    private MainViewModel _viewModel = new MainViewModel();

    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = _viewModel;

        if (!Avalonia.Controls.Design.IsDesignMode)
        {
            // _viewModel.Start(); // À décommenter quand ta logique sera prête
        }
    }

    // 1. Bouton "Changer Langue"
    public void OnLanguageClick(object sender, RoutedEventArgs e)
    {
        // Appel de la fonction de changement de langue
        // _viewModel.ChangeLanguage();
        System.Diagnostics.Debug.WriteLine("Action : Changer Langue");
    }

    // 2. Bouton "Lancer" (pour un seul job)
    public void OnBackupClick(object sender, RoutedEventArgs e)
    {
        // Appel de la fonction pour lancer un job spécifique
        // _viewModel.ExecuteJob(0); 
        System.Diagnostics.Debug.WriteLine("Action : Lancer un Job");
    }

    // 3. Bouton "⚙️" (Paramètres/Modifier)
    public void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        // Appel de la fonction pour modifier les paramètres du job
        // _viewModel.OpenJobSettings(0);
        System.Diagnostics.Debug.WriteLine("Action : Modifier Job");
    }

    // 4. Bouton "🗑️" (Supprimer)
    public void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        // Appel de la fonction pour supprimer un job
        // _viewModel.DeleteJob(0);
        System.Diagnostics.Debug.WriteLine("Action : Supprimer Job");
    }

    // 5. Bouton "+ Nouveau Travail"
    public void OnAddJobClick(object sender, RoutedEventArgs e)
    {
        // Appel de la fonction pour créer un nouveau job
        // _viewModel.AddNewJob();
        System.Diagnostics.Debug.WriteLine("Action : Ajouter un Job");
    }

    // 6. Bouton "Lancer tout"
    public void OnBackupAllClick(object sender, RoutedEventArgs e)
    {
        // Appel de la fonction pour lancer tous les jobs en séquence
        // _viewModel.ExecuteAllJobs();
        System.Diagnostics.Debug.WriteLine("Action : Lancer TOUS les Jobs");
    }
}
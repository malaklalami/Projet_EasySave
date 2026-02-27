using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EasyAvalonia.Views;

namespace EasyAvalonia;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
// Point d'entrée principal de l'application graphique utilisant le framework Avalonia.
// Initialise les ressources XAML et définit la fenêtre MainWindow comme interface de démarrage pour l'environnement de bureau.
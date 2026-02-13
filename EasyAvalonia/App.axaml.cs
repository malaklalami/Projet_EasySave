using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

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
            //DataContext = new MainViewModel()   is not needed because of the use of the ViewModelLocator in the XAML file, which automatically resolves the MainViewModel for the MainWindow.
        }

        base.OnFrameworkInitializationCompleted();
    }
}
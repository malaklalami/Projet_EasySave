using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Reactive; // Ajoutez cette ligne si vous utilisez ReactiveUI
using EasyAvalonia;
using System;

namespace EasyAvalonia;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() // <-- Vérifie bien qu'il y a écrit AppBuilder ici
    => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}
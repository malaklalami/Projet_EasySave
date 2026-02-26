using EasySave.Core;
using EasySave.ViewModels;

namespace EasyConsole;

public static class SettingsUI
{
    public static void LogDestination(MainViewModel vm)
    {
        Console.Clear();
        Console.WriteLine("=== LOG TARGET CONFIGURATION ===");
        Console.WriteLine("1. Local | 2. Remote | 3. Both");
        Console.Write("\nChoice: ");

        if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= 3)
        {
            vm.Settings.LogStrategy = (LogTarget)(choice - 1);

            if (vm.Settings.LogStrategy != LogTarget.Local)
            {
                Console.Write($"Server IP [{vm.Settings.RemoteIp}] : ");
                string ip = Console.ReadLine() ?? "";
                if (!string.IsNullOrWhiteSpace(ip)) vm.Settings.RemoteIp = ip;
            }

            vm.SaveSettings();
            Console.WriteLine("\n[OK] Settings saved.");
        }
        Thread.Sleep(1000);
    }
}
using EasySave.ViewModels;
using System;
using System.Threading.Tasks;

namespace EasyConsole;

class Program
{
    static async Task Main(string[] args)
    {
        // Initialisation du ViewModel (charge les jobs et la langue)
        MainViewModel viewModel = new MainViewModel();

        if (args.Length > 0)
        {
            // Mode Automatique (ex: EasySave.exe 1-3)
            Console.WriteLine($"--- Mode Automatique : {args[0]} ---");
            await viewModel.ExecuteSelection(args[0]);
            Console.WriteLine("\nSauvegarde lancée en arrière-plan...");
            Console.WriteLine("\nAppuyez sur une touche pour quitter.");
            Console.ReadKey();
            viewModel.StopAll();
        }
        else
        {
            // Mode Interactif (Menu)
            MenuHandler menu = new MenuHandler(viewModel);
            menu.Run();
        }
    }
}
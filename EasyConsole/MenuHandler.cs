using EasySave.Core;
using EasyLibrary.ViewModels;
using EasyLibrary.ViewModels;

namespace EasyConsole;

public class MenuHandler
{
    private readonly MainViewModel _vm;
    private readonly JobUI _jobUI;

    public MenuHandler(MainViewModel vm)
    {
        _vm = vm;
        _jobUI = new JobUI(vm);
    }

    public void Run()
    {
        bool exit = false;
        while (!exit)
        {
            ShowMenu();
            string choice = Console.ReadLine() ?? "";
            switch (choice)
            {
                case "1": _jobUI.CreateJob(); break;
                case "2": _jobUI.ExecuteSelection(); break;
                case "3": _vm.SwitchLanguage(); break;
                case "4": _jobUI.EditJob(); break;
                case "5": _jobUI.DeleteJob(); break;
                case "q": exit = true; break;
                default: Console.WriteLine("Choix invalide."); break;
            }
        }
    }

    private void ShowMenu()
    {
        bool isFr = _vm.CurrentSettings.Language == "fr";
        Console.WriteLine(isFr ? "\n--- Menu EasySave ---" : "\n--- EasySave Menu ---");

        // On affiche les jobs actuels
        for (int i = 0; i < _vm.Jobs.Count; i++)
        {
            Console.WriteLine($"[{i}] {_vm.Jobs[i].Name}");
        }

        if (isFr)
        {
            Console.WriteLine("1. Créer | 2. Lancer | 3. Langue | 4. Modifier | 5. Supprimer | q. Quitter");
        }
        else
        {
            Console.WriteLine("1. Create | 2. Run | 3. Language | 4. Edit | 5. Delete | q. Quit");
        }
    }
}
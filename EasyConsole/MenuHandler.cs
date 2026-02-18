using EasySave.Core;
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
            Console.Write("\n> ");
            string choice = Console.ReadLine() ?? "";
            switch (choice)
            {
                case "1": _jobUI.CreateJob(); break;
                case "2": _jobUI.ExecuteSelection(); break;
                case "3": ChangeLanguageMenu(); break;
                case "4": _vm.SwitchLogFormat(); break;
                case "5": _jobUI.EditJob(); break;
                case "6": _jobUI.DeleteJob(); break;
                case "7": _vm.ClearAllJobs();break;


                case "8":
                    Console.WriteLine(_vm.Language.Get("Menu_Option8"));
                    Console.Write("> ");
                    string ext = Console.ReadLine() ?? "";
                    _vm.ManageEncryptionExtensions(ext);
                    break;

                case "9":
                    Console.WriteLine(_vm.Language.Get("Menu_Option9"));
                    Console.Write("> ");
                    string soft = Console.ReadLine() ?? "";
                    _vm.UpdateBusinessSoftware(soft);
                    break;

                case "q": exit = true; break;
                default: Console.WriteLine(_vm.Language.Get("Invalid_Choice")); break;
            }
        }
    }

    private void ChangeLanguageMenu()
    {
        Console.Clear();
        Console.WriteLine("--- Language / Langue ---");


        Console.WriteLine("1. English");
        Console.WriteLine("2. Français");


        Console.WriteLine("b. " + _vm.Language.Get("Lang_Back"));

        Console.Write("\n> ");
        string choice = Console.ReadLine() ?? "";

        switch (choice)
        {
            case "1":
                _vm.SetLanguage("en");
                break;
            case "2":
                _vm.SetLanguage("fr");
                break;
            case "b":
                return;
            default:
                Console.WriteLine("Invalid choice / Choix invalide.");
                Thread.Sleep(1000);
                break;
        }

    }

    private void ShowMenu()
    {
        Console.Clear();
        Console.WriteLine($"\n{_vm.Language.Get("Menu_MainTitle")}");

        Console.WriteLine($"--- {_vm.Language.Get("Job_List_Title")} ---");

        if (_vm.Jobs.Count == 0)
        {
            Console.WriteLine(_vm.Language.Get("Job_List_Empty"));
        }
        else
        {

            for (int i = 0; i < _vm.Jobs.Count; i++)
            {
                Console.WriteLine($"[{i}] {_vm.Jobs[i].Name}-> {_vm.Jobs[i].SourceDir}");
            }

        }
        Console.WriteLine("-----------------------------------");

        // --- Affichage du menu via les clés JSON ---
        Console.WriteLine(_vm.Language.Get("Menu_Option1"));
        Console.WriteLine(_vm.Language.Get("Menu_Option2"));
        Console.WriteLine(_vm.Language.Get("Menu_Option3"));
        Console.WriteLine(_vm.Language.Get("Menu_Option4"));
        Console.WriteLine(_vm.Language.Get("Menu_Option5"));
        Console.WriteLine(_vm.Language.Get("Menu_Option6"));
        Console.WriteLine(_vm.Language.Get("Menu_Option7"));
        Console.WriteLine(_vm.Language.Get("Menu_Option8"));
        Console.WriteLine(_vm.Language.Get("Menu_Option9"));
        Console.WriteLine(_vm.Language.Get("Menu_Quit"));
    }
}
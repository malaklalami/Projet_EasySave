using EasyLibrary.ViewModels;
using EasySave.Core;
using EasySave.Models;

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

    public static (string Action, string Target) ParseControlCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return ("", "");

        var parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        string action = parts[0].ToLower();
        string target = parts.Length > 1 ? parts[1] : "all";

        return (action, target);
    }

    public void Run()
    {
        bool exit = false;
        while (!exit)
        {


            ShowMenu();
            Console.Write("\n> ");
            string choice = Console.ReadLine() ?? "";
            var (action, target) = ParseControlCommand(choice);
            switch (action)
            {
                case "1": _jobUI.CreateJob(); break;
                case "2": _jobUI.ExecuteSelection(); break;
                case "3": ChangeLanguageMenu(); break;
                case "4": _vm.SwitchLogFormat(); break;
                case "5": _jobUI.EditJob(); break;
                case "6": _jobUI.DeleteJob(); break;
                case "7": _vm.ClearAllJobs(); break;


                case "8":EncryptionExtensionsMenu();break;

                case "9":BusinessSoftwareMenu(); break;

                case "10":
                    SettingsUI.LogDestination(_vm);
                    break;
                case "pause":
                    if (target == "all") _vm.PauseAllJobs();
                    else _vm.PauseJob(target);
                    break;

                case "resume":
                    if (target == "all") _vm.ResumeAllJobs();
                    else _vm.ResumeJob(target);
                    break;

                case "stop":
                    if (target == "all") _vm.StopAllJobs();
                    else _vm.StopJob(target);
                    break;

                case "q": exit = true; break;
                default: Console.WriteLine(_vm.Language.Get("Invalid_Choice")); break;
            }
        }
    }

    private void ChangeLanguageMenu()
    {
        Console.Clear();
        Console.WriteLine(_vm.Language.Get("Lang_Menu_Title"));
        Console.WriteLine(_vm.Language.Get("Lang_Option_En"));
        Console.WriteLine(_vm.Language.Get("Lang_Option_Fr"));
        Console.WriteLine(_vm.Language.Get("Lang_Back"));


        Console.Write("\n> ");
        string choice = Console.ReadLine() ?? "";

        switch (choice)
        {
            case "1":
                _vm.SetLanguage("en");
                Console.WriteLine(_vm.Language.Get("Lang_Confirm_En"));
                Thread.Sleep(1000); // Pour laisser le temps de lire la confirmation
                break;
            case "2":
                _vm.SetLanguage("fr");
                Console.WriteLine(_vm.Language.Get("Lang_Confirm_Fr"));
                Thread.Sleep(1000);
                break;
            case "b":
                return;
            default:
                Console.WriteLine(_vm.Language.Get("Invalid_Choice"));
                Thread.Sleep(1000);
                break;
        }

    }

    private void EncryptionExtensionsMenu()
    {
        bool back = false;
        while (!back)
        {
            Console.Clear();
            Console.WriteLine(_vm.Language.Get("Ext_Menu_Title"));

           
            var current = _vm.Settings.EncryptionExtensions;
            string list = current.Any() ? string.Join(", ", current) : _vm.Language.Get("No_Extensions");
            Console.WriteLine($"{string.Format(_vm.Language.Get("Current_Extensions"), list)}\n");

            Console.WriteLine(_vm.Language.Get("Ext_Option_Add"));
            Console.WriteLine(_vm.Language.Get("Ext_Option_Clear"));
            Console.WriteLine(_vm.Language.Get("Lang_Back"));

            Console.Write("\n> ");
            string choice = Console.ReadLine() ?? "";

            
            switch (choice)
            {
                case "1":
                    Console.Write(_vm.Language.Get("Manage_Instructions_Ext") + " ");
                    string ext = Console.ReadLine() ?? "";
                    _vm.ManageEncryptionExtensions(ext);
                    Console.WriteLine(_vm.Language.Get("Action_Success"));
                    Thread.Sleep(800);
                    break;
                case "2":
                    _vm.Settings.EncryptionExtensions.Clear();
                    _vm.SaveSettings();
                    Console.WriteLine(_vm.Language.Get("Action_Success"));
                    Thread.Sleep(800);
                    break;
                case "b":
                    back = true;
                    break;
                default:
                    Console.WriteLine(_vm.Language.Get("Invalid_Choice"));
                    Thread.Sleep(800);
                    break;
            }
        }
    }

    private void BusinessSoftwareMenu()
    {
        Console.Clear();
        Console.WriteLine(_vm.Language.Get("Soft_Menu_Title"));

        string current = _vm.Settings.BusinessSoftware;
        string softDisplay = !string.IsNullOrEmpty(current) ? current : _vm.Language.Get("No_Software");
        Console.WriteLine($"{string.Format(_vm.Language.Get("Current_Software"), softDisplay)}\n");

        Console.WriteLine(_vm.Language.Get("Soft_Option_Update"));
        Console.WriteLine(_vm.Language.Get("Soft_Option_Delete"));
        Console.WriteLine(_vm.Language.Get("Lang_Back"));

        Console.Write("\n> ");
        string choice = Console.ReadLine() ?? "";

        
        switch (choice)
        {
            case "1":
                Console.Write(_vm.Language.Get("Manage_Instructions_Soft") + " ");
                string soft = Console.ReadLine() ?? "";
                _vm.UpdateBusinessSoftware(soft);
                Console.WriteLine(_vm.Language.Get("Action_Success"));
                Thread.Sleep(1000);
                break;
            case "2":
                _vm.UpdateBusinessSoftware("");
                Console.WriteLine(_vm.Language.Get("Action_Success"));
                Thread.Sleep(1000);
                break;
            case "b":
                return;
            default:
                Console.WriteLine(_vm.Language.Get("Invalid_Choice"));
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
        Console.WriteLine($"{_vm.Language.Get("Menu_Option4")} [{_vm.Settings.LogFormat}]");
        Console.WriteLine(_vm.Language.Get("Menu_Option5"));
        Console.WriteLine(_vm.Language.Get("Menu_Option6"));
        Console.WriteLine(_vm.Language.Get("Menu_Option7"));
        Console.WriteLine($"{_vm.Language.Get("Menu_Option8")} [ {(_vm.Settings.EncryptionExtensions.Any() ? string.Join(", ", _vm.Settings.EncryptionExtensions) : _vm.Language.Get("No_Extensions"))} ]");
        Console.WriteLine($"{_vm.Language.Get("Menu_Option9")} [ {(!string.IsNullOrEmpty(_vm.Settings.BusinessSoftware) ? _vm.Settings.BusinessSoftware : _vm.Language.Get("No_Software"))} ]");
        Console.WriteLine(_vm.Language.Get("Menu_Option10"));
        Console.WriteLine(_vm.Language.Get("Menu_Option_Pause"));
        Console.WriteLine(_vm.Language.Get("Menu_Option_Resume"));
        Console.WriteLine(_vm.Language.Get("Menu_Option_Stop"));
        Console.WriteLine(_vm.Language.Get("Menu_Quit"));

    }
}
using EasySave.Core;
using EasySave.ViewModels;

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
        while (true)
        {
            ShowMenu();
            Console.Write("\n> ");
            string input = Console.ReadLine()?.ToLower() ?? "";

            // Gestion des commandes de contrôle direct
            if (input.StartsWith("pause")) 
            { 
                HandleControlCommand(input, "pause"); // <--- Appel d'une méthode d'aide
                continue; 
            }
            if (input.StartsWith("resume")) 
            { 
                HandleControlCommand(input, "resume"); // <--- Appel d'une méthode d'aide
                continue; 
            }
            if (input.StartsWith("stop")) 
            { 
                HandleControlCommand(input, "stop"); // <--- Appel d'une méthode d'aide
                continue; 
            }

            switch (input)
            {
                case "1": _jobUI.CreateJob(); break;
                case "2": _jobUI.ExecuteSelection(); break;
                case "3": ChangeLanguageMenu(); break;
                case "4": _vm.SwitchLogFormat(); break;
                case "5": _jobUI.EditJob(); break;
                case "6": _jobUI.DeleteJob(); break;
                case "7": _vm.ClearAllJobs(); break;
                case "8": EncryptionExtensionsMenu(); break;
                case "9": BusinessSoftwareMenu(); break;
                case "10": SettingsUI.LogDestination(_vm); break;
                case "q": return;
                default:
                    Console.WriteLine(_vm.LanguageService.Get("Invalid_Choice"));
                    Thread.Sleep(1000);
                    break;
            }
        }
    }

    private void ShowMenu()
    {
        Console.Clear();
        Console.WriteLine(_vm.LanguageService.Get("Menu_MainTitle"));

        // Liste des Jobs existants
        Console.WriteLine($"\n{_vm.LanguageService.Get("Job_List_Title")}");
        if (_vm.Jobs.Count == 0) Console.WriteLine(_vm.LanguageService.Get("Job_List_Empty"));
        for (int i = 0; i < _vm.Jobs.Count; i++)
            Console.WriteLine($"[{i}] {_vm.Jobs[i].Name} -> {_vm.Jobs[i].SourceDir}");

        for (int i = 1; i <= 10; i++)
        {
            string label = _vm.LanguageService.Get($"Menu_Option{i}");

            if (i == 4) // Format des Logs
            {
                Console.WriteLine($"{label} [{_vm.Settings.LogFormat}]");
            }
            else if (i == 8) // Extensions cryptées
            {
                string ext = _vm.Settings.EncryptionExtensions.Any()
                    ? string.Join(", ", _vm.Settings.EncryptionExtensions)
                    : _vm.LanguageService.Get("No_Extensions");
                Console.WriteLine($"{label} [ {ext} ]");
            }
            else if (i == 9) // Logiciel métier
            {
                string soft = !string.IsNullOrEmpty(_vm.Settings.BusinessSoftware)
                    ? _vm.Settings.BusinessSoftware
                    : _vm.LanguageService.Get("No_Software");
                Console.WriteLine($"{label} [ {soft} ]");
            }
            else
            {
                // Options standards (1, 2, 3, 5, 6, 7, 10)
                Console.WriteLine(label);
            }
        }

        Console.WriteLine(_vm.LanguageService.Get("Menu_Option_Pause"));
        Console.WriteLine(_vm.LanguageService.Get("Menu_Option_Resume"));
        Console.WriteLine(_vm.LanguageService.Get("Menu_Option_Stop"));
        Console.WriteLine(_vm.LanguageService.Get("Menu_Quit"));
    }

    private void ChangeLanguageMenu()
    {
        Console.WriteLine($"\n{_vm.LanguageService.Get("Lang_Menu_Title")}");
        Console.WriteLine("1. English / 2. Français");
        string choice = Console.ReadLine() ?? "";
        _vm.UpdateLanguage(choice == "2" ? "fr" : "en");
        Console.WriteLine(_vm.LanguageService.Get(choice == "2" ? "Lang_Confirm_Fr" : "Lang_Confirm_En"));
        Thread.Sleep(1000);
    }

    private void EncryptionExtensionsMenu()
    {
        Console.Write(_vm.LanguageService.Get("Manage_Instructions_Ext") + " ");
        string ext = Console.ReadLine() ?? "";
        _vm.ManageEncryptionExtensions(ext);
        Console.WriteLine(_vm.LanguageService.Get("Action_Success"));
        Thread.Sleep(1000);
    }

    private void BusinessSoftwareMenu()
    {
        Console.Write(_vm.LanguageService.Get("Manage_Instructions_Soft") + " ");
        string soft = Console.ReadLine() ?? "";
        _vm.UpdateBusinessSoftware(soft);
        Console.WriteLine(_vm.LanguageService.Get("Action_Success"));
        Thread.Sleep(1000);
    }
    private void HandleControlCommand(string input, string type)
    {
        var parts = input.Split(' ');

        // 1. CAS GLOBAL : Commande "pause", "resume" ou "stop" toute seule
        if (parts.Length == 1)
        {
            if (type == "pause")
            {
                _vm.PauseAll();
                Console.WriteLine(_vm.LanguageService.Get("All_Jobs_Paused")); // <--- Ta clé exacte
            }
            else if (type == "resume")
            {
                _vm.ResumeAll();
                Console.WriteLine(_vm.LanguageService.Get("All_Jobs_Resumed")); // <--- Ta clé exacte
            }
            else if (type == "stop")
            {
                _vm.StopAll();
                Console.WriteLine(_vm.LanguageService.Get("All_Jobs_Stopped")); // <--- Ta clé exacte
            }
        }
        // 2. CAS INDIVIDUEL : Commande "pause 1", etc.
        else if (parts.Length == 2 && int.TryParse(parts[1], out int id) && id >= 0 && id < _vm.Jobs.Count)
        {
            var job = _vm.Jobs[id];
            if (type == "pause")
            {
                _vm.PauseJob(job);
                Console.WriteLine($"{_vm.LanguageService.Get("Job_Paused")} : {job.Name}");
            }
            else if (type == "resume")
            {
                _vm.ResumeJob(job);
                Console.WriteLine($"{_vm.LanguageService.Get("Job_Resumed")} : {job.Name}");
            }
            else if (type == "stop")
            {
                _vm.StopJob(job);
                Console.WriteLine($"{_vm.LanguageService.Get("Job_Stopped")} : {job.Name}");
            }
        }
        else
        {
            Console.WriteLine(_vm.LanguageService.Get("Invalid_Choice"));
        }

        Thread.Sleep(1000);
    }
}
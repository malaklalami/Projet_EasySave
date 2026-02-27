
using EasySave.Core;
using EasySave.Models;
using EasySave.ViewModels;

namespace EasyConsole;

public class JobUI
{
    private readonly MainViewModel _vm;
    public JobUI(MainViewModel vm) => _vm = vm;

    public void CreateJob()
    {
        Console.WriteLine($"\n{_vm.LanguageService.Get("Create_Title")}");

        Console.Write(_vm.LanguageService.Get("Input_JobName"));
        string name = Console.ReadLine() ?? "";

        Console.Write(_vm.LanguageService.Get("Input_SourcePath"));
        string source = Console.ReadLine() ?? "";

        Console.Write(_vm.LanguageService.Get("Input_TargetPath"));
        string target = Console.ReadLine() ?? "";

        Console.Write($"{_vm.LanguageService.Get("Input_Type")} {_vm.LanguageService.Get("Input_Type_Options")} : ");
        BackupType type = (Console.ReadLine() == "1") ? BackupType.Differential : BackupType.Full;

        _vm.AddJob(name, source, target, type);
        Console.WriteLine(_vm.LanguageService.Get("Create_Success"));
        Thread.Sleep(1000);
    }

    public void ExecuteSelection()
    {
        Console.WriteLine($"\n{_vm.LanguageService.Get("Run_Title")}");
        Console.Write(_vm.LanguageService.Get("Run_Input"));
        string input = Console.ReadLine() ?? "";

        Console.WriteLine($"\n>>> {_vm.LanguageService.Get("Run_Started")}");
         _vm.ExecuteSelection(input);
        Console.WriteLine($"\n<<< {_vm.LanguageService.Get("Run_Finished")}");

        Console.WriteLine("\nAppuyez sur une touche pour revenir au menu...");
        Console.ReadKey();

    }

    public void DeleteJob()
    {
        Console.Write(_vm.LanguageService.Get("Delete_Select"));
        if (int.TryParse(Console.ReadLine(), out int i) && i >= 0 && i < _vm.Jobs.Count)
        {
            var job = _vm.Jobs[i];
            _vm.DeleteJob(job);
            Console.WriteLine(string.Format(_vm.LanguageService.Get("Delete_Success"), i, job.Name));
        }
        Thread.Sleep(1000);
    }

    public void EditJob()
    {
        Console.Write(_vm.LanguageService.Get("Edit_Select"));

        if (int.TryParse(Console.ReadLine(), out int i) && i >= 0 && i < _vm.Jobs.Count)
        {
            var job = _vm.Jobs[i];
            Console.WriteLine(_vm.LanguageService.Get("Edit_Title"));

            // 1. Modification du NOM
            Console.Write($"{_vm.LanguageService.Get("Input_JobName")} [{job.Name}] : ");
            string inputName = Console.ReadLine() ?? "";
            string newName = string.IsNullOrWhiteSpace(inputName) ? job.Name : inputName;

            // 2. Modification de la SOURCE
            Console.Write($"Source [{job.SourceDir}] : ");
            string inputSrc = Console.ReadLine() ?? "";
            string newSrc = string.IsNullOrWhiteSpace(inputSrc) ? job.SourceDir : inputSrc;

            // 3. Modification de la DESTINATION
            Console.Write($"Destination [{job.TargetDir}] : ");
            string inputDest = Console.ReadLine() ?? "";
            string newDest = string.IsNullOrWhiteSpace(inputDest) ? job.TargetDir : inputDest;

            // 4. Modification du TYPE (0 = Complet, 1 = Différentiel)
            Console.Write($"Type (0=Complet, 1=Différentiel) [{(int)job.Type}] : ");
            string inputType = Console.ReadLine() ?? "";
            BackupType newType = job.Type; // Par défaut, on garde l'ancien

            if (!string.IsNullOrWhiteSpace(inputType))
            {
                if (inputType == "0") newType = BackupType.Full;
                else if (inputType == "1") newType = BackupType.Differential;
            }

            // 5. ON APPELLE LE VIEWMODEL SANS DÉTRUIRE LE JOB !
            _vm.EditJob(job, newName, newSrc, newDest, newType);

            Console.WriteLine(string.Format(_vm.LanguageService.Get("Edit_Success"), i, newName));
        }
        else
        {
            Console.WriteLine("Index invalide !"); // Petit message d'erreur si on tape n'importe quoi
        }

        Thread.Sleep(1000);
    }
}
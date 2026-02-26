
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

        Task.Run(async () => {
            await _vm.ExecuteSelection(input);
            Console.WriteLine($"\n{_vm.LanguageService.Get("Run_Finished")}");
            Console.Write("\n> ");
        });
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

            Console.Write($"{_vm.LanguageService.Get("Input_JobName")} [{job.Name}] : ");
            string n = Console.ReadLine() ?? "";

            // ... (Ici tu peux ajouter les autres champs source/target)

            _vm.DeleteJob(job);
            _vm.AddJob(string.IsNullOrWhiteSpace(n) ? job.Name : n, job.SourceDir, job.TargetDir, job.Type);
            Console.WriteLine(string.Format(_vm.LanguageService.Get("Edit_Success"), i, n));
        }
        Thread.Sleep(1000);
    }
}
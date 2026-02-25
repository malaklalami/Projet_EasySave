using EasySave.Core;
using EasyLibrary.ViewModels;
using EasySave.Core;


namespace EasyConsole;

// Gère les interactions utilisateur liées à la création, l'édition et la suppression des travaux de sauvegarde.
// Permet le lancement asynchrone des sauvegardes et le contrôle individuel ou global de leur état via la console.

public class JobUI
{
    private readonly MainViewModel _vm;

    public JobUI(MainViewModel vm) => _vm = vm;

    public void CreateJob()
    {
        Console.WriteLine($"\n{_vm.Language.Get("Create_Title")}");


        Console.Write(_vm.Language.Get("Input_JobName"));
        string name = Console.ReadLine() ?? "";

        Console.Write(_vm.Language.Get("Input_SourcePath"));
        string source = Console.ReadLine() ?? "";
        if (!Directory.Exists(source) && !string.IsNullOrWhiteSpace(source))
            Console.WriteLine(_vm.Language.Get("Error_DirNotExists"));

        Console.Write(_vm.Language.Get("Input_TargetPath"));
        string target = Console.ReadLine() ?? "";

        Console.Write(_vm.Language.Get("Input_Type"));
        Enum.TryParse(Console.ReadLine(), out BackupType type);

        try
        {
            _vm.AddJob(name, source, target, type);
            Console.WriteLine(_vm.Language.Get("Create_Success"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public void ExecuteSelection()
    {
        Console.WriteLine($"\n{_vm.Language.Get("Run_Title")}");
        Console.Write(_vm.Language.Get("Run_Input"));
        string input = Console.ReadLine() ?? "";

        // On lance la tâche en arrière-plan
        Task.Run(async () =>
        {
            await _vm.Execute(input);

            Console.WriteLine($"\n>{_vm.Language.Get("Run_Finished")}");
            Console.Write("\n> "); // On réaffiche le prompt pour le menu
        });

    }

    public void ControlJob(string action, string target)
    {
        if (target == "all")
        {
            if (action == "pause") _vm.PauseAllJobs();
            else if (action == "resume") _vm.ResumeAllJobs();
            else if (action == "stop") _vm.StopAllJobs();

            Console.WriteLine($"\n[OK] {_vm.Language.Get("All_Jobs_Label")} : {action}");
        }
        else
        {
            // On appelle le VM pour l'action technique
            if (action == "pause") _vm.PauseJob(target);
            else if (action == "resume") _vm.ResumeJob(target);
            else if (action == "stop") _vm.StopJob(target);

            // On affiche la confirmation ici !
            string statusKey = action == "pause" ? "Job_Paused" : (action == "resume" ? "Job_Resumed" : "Job_Stopped");
            Console.WriteLine($"\n[OK] {_vm.Language.Get(statusKey)}: {target}");
        }
    }

    public void DeleteJob()
    {
        Console.Write(_vm.Language.Get("Delete_Select"));
        if (int.TryParse(Console.ReadLine(), out int index) && index >= 0 && index < _vm.Jobs.Count)
        {
            _vm.DeleteJob(index);
            Console.WriteLine(_vm.Language.Get("Delete_Success"));
        }
        else
        {
            Console.WriteLine(_vm.Language.Get("Invalid_Choice"));
        }
    }

    public void EditJob()
    {
        Console.WriteLine($"\n{_vm.Language.Get("Edit_Title")}");
        Console.Write(_vm.Language.Get("Edit_Select"));


        if (int.TryParse(Console.ReadLine(), out int index) && index >= 0 && index < _vm.Jobs.Count)
        {
            var job = _vm.Jobs[index];

            Console.Write($"{_vm.Language.Get("Input_JobName")} [{job.Name}] : ");
            string name = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(name)) name = job.Name;

            Console.Write($"{_vm.Language.Get("Input_SourcePath")} [{job.SourceDir}] : ");
            string source = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(source)) source = job.SourceDir;


            Console.Write($"{_vm.Language.Get("Input_TargetPath")} [{job.TargetDir}] : ");
            string target = Console.ReadLine() ?? "";
            if (string.IsNullOrWhiteSpace(target)) target = job.TargetDir;

            Console.Write($"{_vm.Language.Get("Input_Type")} [{(int)job.Type}] : ");
            string typeInput = Console.ReadLine() ?? "";
            BackupType type = string.IsNullOrWhiteSpace(typeInput)
                ? job.Type
                : (typeInput == "1" ? BackupType.Differential : BackupType.Full);


            _vm.DeleteJob(index);
            _vm.AddJob(name, source, target, type);


            Console.WriteLine(string.Format(_vm.Language.Get("Edit_Success"), index, name));
        }
        else
        {
            
            Console.WriteLine(_vm.Language.Get("Invalid_Choice"));
        }
    }
}
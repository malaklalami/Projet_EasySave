using EasySave.Core;
using EasyLibrary.ViewModels;
using EasySave.Core;


namespace EasyConsole;

public class JobUI
{
    private readonly MainViewModel _vm;

    public JobUI(MainViewModel vm) => _vm = vm;

    public void CreateJob()
    {
        bool isFr = _vm.CurrentSettings.Language == "fr";
        Console.Write(isFr ? "Nom : " : "Name: ");
        string name = Console.ReadLine() ?? "";

        Console.Write(isFr ? "Source : " : "Source: ");
        string source = Console.ReadLine() ?? "";

        Console.Write(isFr ? "Cible : " : "Target: ");
        string target = Console.ReadLine() ?? "";

        Console.Write(isFr ? "Type (0: Complet, 1: Diff) : " : "Type (0: Full, 1: Diff): ");
        Enum.TryParse(Console.ReadLine(), out BackupType type);

        try
        {
            _vm.AddJob(name, source, target, type);
            Console.WriteLine(isFr ? "Succès !" : "Success!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    public void ExecuteSelection()
    {
        bool isFr = _vm.CurrentSettings.Language == "fr";
        Console.Write(isFr ? "Sélection (ex: 1-3;5) : " : "Selection (e.g. 1-3;5): ");
        string input = Console.ReadLine() ?? "";

        // On lance l'exécution via le contrôleur
        _vm.Execute(input);
    }

    public void DeleteJob()
    {
        Console.Write("Index : ");
        if (int.TryParse(Console.ReadLine(), out int index))
        {
            _vm.DeleteJob(index);
        }
    }

    public void EditJob()
    {
        // Même logique que Delete mais appelle ViewModel.UpdateJob
       
    }
}
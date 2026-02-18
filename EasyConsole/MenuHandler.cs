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
            // IMPORTANT : On définit isFr ici pour l'utiliser dans le switch
            bool isFr = _vm.CurrentSettings.Language == "fr";

            ShowMenu();

            string choice = Console.ReadLine() ?? "";
            switch (choice)
            {
                case "1": _jobUI.CreateJob(); break;
                case "2": _jobUI.ExecuteSelection(); break;
                case "3": _vm.SwitchLanguage(); break;
                case "4": _jobUI.EditJob(); break;
                case "5": _jobUI.DeleteJob(); break;

                case "6":
                    Console.Write(isFr ? "Extension à ajouter/retirer (ex: .txt) : " : "Extension to add/remove (e.g., .txt): ");
                    string ext = Console.ReadLine() ?? "";
                    _vm.ManageEncryptionExtensions(ext);
                    break;

                case "7":
                    Console.Write(isFr ? "Nom du logiciel métier (ex: Calculator) : " : "Business software name (e.g., Calculator): ");
                    string soft = Console.ReadLine() ?? "";
                    _vm.UpdateBusinessSoftware(soft);
                    break;

                case "q": exit = true; break;
                default: Console.WriteLine(isFr ? "Choix invalide." : "Invalid choice."); break;
            }
        }
    }

    private void ShowMenu()
    {
        bool isFr = _vm.CurrentSettings.Language == "fr";
        Console.WriteLine(isFr ? "\n--- Menu EasySave ---" : "\n--- EasySave Menu ---");

        for (int i = 0; i < _vm.Jobs.Count; i++)
        {
            Console.WriteLine($"[{i}] {_vm.Jobs[i].Name}");
        }

        if (isFr)
        {
            Console.WriteLine("1. Créer | 2. Lancer | 3. Langue | 4. Modifier | 5. Supprimer");
            Console.WriteLine("6. Extensions Cryptage | 7. Logiciel Métier | q. Quitter");
        }
        else
        {
            Console.WriteLine("1. Create | 2. Run | 3. Language | 4. Edit | 5. Delete");
            Console.WriteLine("6. Encryption Ext | 7. Business Soft | q. Quit");
        }
    }
}
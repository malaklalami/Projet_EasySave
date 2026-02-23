using EasyLibrary.ViewModels;


namespace EasyConsole;

class Program
{
    static async Task Main(string[] args)
    {
        // 1. On crée le contrôleur (MainViewModel)
        // Il charge automatiquement les jobs et settings au démarrage grâce à son constructeur.
        MainViewModel viewModel = new MainViewModel();

        // 2. On vérifie s'il y a des arguments (Mode ligne de commande : EasySave.exe 1-3)
        if (args.Length > 0)
        {
            Console.WriteLine($"--- Mode Automatique : Exécution de {args[0]} ---");
            // On appelle directement la méthode du contrôleur
            await viewModel.Execute(args[0]);

            Console.WriteLine("\n[TERMINÉ] Appuyez sur une touche pour quitter.");
            Console.ReadKey();
        }
        else
        {
            // 3. MODE INTERACTIF (Menu)
            // On délègue toute la gestion du menu à la classe dédiée
            MenuHandler menu = new MenuHandler(viewModel);
            menu.Run();
        }
    }
}
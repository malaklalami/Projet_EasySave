using System;
using EasySave.View;

namespace EasySave
{
    class Program
    {
        static void Main(string[] args)
        {
            // On crée l'instance de la vue
            ConsoleView view = new ConsoleView();

            // On lance la boucle infinie du menu
            view.Start();
        }
    }
}
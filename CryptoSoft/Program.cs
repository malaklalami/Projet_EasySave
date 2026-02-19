using System;
using System.Threading;

namespace CryptoSoft;

public static class Program
{
    // On déclare le Mutex au niveau de la classe
    private static Mutex? _mutex;

    public static void Main(string[] args)
    {
        // Nom unique pour identifier l'instance de CryptoSoft sur Windows
        const string mutexName = @"Global\ProSoft_CryptoSoft_SingleInstance";
        bool createdNew;

        // Tentative d'acquisition du Mutex
        _mutex = new Mutex(true, mutexName, out createdNew);

        if (!createdNew)
        {
            // Si le mutex existe déjà, on quitte avec un code erreur spécifique.
            //-3 signifie "Occupé, réessaie plus tard".
            Environment.Exit(-3);
            return;
        }

        try
        {
            if (args.Length < 2)
            {
                Environment.Exit(-1);
                return;
            }

            
            var fileManager = new FileManager(args[0], args[1]);
            int elapsedTime = fileManager.TransformFile();

            // On renvoie le temps d'exécution 
            Environment.Exit(elapsedTime);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Environment.Exit(-99);
        }
        finally
        {
            // On libère impérativement le Mutex pour que le prochain fichier puisse être traité
            if (createdNew && _mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }
        }
    }
}
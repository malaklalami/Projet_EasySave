using System;
using System.Diagnostics;
using System.IO;
using System.Threading; // Indispensable pour Thread.Sleep

namespace EasySave.Services
{
    public class CryptoService
    {
        // --- DÉCLARATION DES VARIABLES ---
        private readonly string _path;
        private readonly string _key;

        // --- CONSTRUCTEUR ---
        public CryptoService(string path, string key)
        {
            _path = path;
            _key = key;
        }

        // --- MÉTHODE ENCRYPT  ---
        public long Encrypt(string file)
        {
            if (!File.Exists(_path)) return -1;

            int maxAttempts = 50;
            int currentAttempt = 0;

            while (currentAttempt < maxAttempts)
            {
                try
                {
                    var start = new ProcessStartInfo
                    {
                        FileName = _path,
                        Arguments = $"\"{file}\" \"{_key}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };

                    using var p = Process.Start(start);
                    p?.WaitForExit();

                    // CAS 1 : Succès (Le Mutex n'était pas bloqué)
                    // On vérifie si l'ExitCode est positif (temps en ms)
                    if (p != null && p.ExitCode >= 0)
                    {
                        return p.ExitCode;
                    }

                    // CAS 2 : Le Mutex est occupé (Code -3 défini dans ton CryptoSoft)
                    if (p != null && p.ExitCode == -3)
                    {
                        currentAttempt++;
                        Thread.Sleep(100); // On attend 100ms avant de retenter
                        continue;
                    }

                    return -1; // Autre erreur
                }
                catch
                {
                    return -2;
                }
            }

            return -1; // Échec après trop de tentatives
        }
    }
}
//On a ajouté une boucle d'attente. Si CryptoService voit que CryptoSoft est occupé (code -3),
//il ne panique pas : il attend 100 millisecondes et réessaie automatiquement. Il fait ça jusqu'à ce que la place se libère
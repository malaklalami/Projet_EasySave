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
            if (!File.Exists(_path)) return -10;

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
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true

                    };

                    using var p = Process.Start(start);
                    p?.WaitForExit();

                    // CAS 1 : Succès (Le Mutex n'était pas bloqué)
                    // On vérifie si l'ExitCode est positif (temps en ms)
                    if (p != null && p.ExitCode >= 0)
                    {
                        return p.ExitCode;
                    }

                    // CAS 2 : Le Mutex est occupé (Code -3 défini dans  CryptoSoft)
                    if (p != null && p.ExitCode == -3)
                    {
                        currentAttempt++;
                        Thread.Sleep(200); // On attend 100ms avant de retenter
                        continue;
                    }

                    return -20; // Autre erreur
                }
                catch
                {
                    return -30;
                }
            }

            return -40; // Échec après trop de tentatives
        }
    }
}
//On a ajouté une boucle d'attente. Si CryptoService voit que CryptoSoft est occupé (code -3),
//il ne panique pas : il attend 100 millisecondes et réessaie automatiquement. Il fait ça jusqu'à ce que la place se libère
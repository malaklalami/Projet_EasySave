using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace EasySave.Services
{
    public class CryptoService
    {
        private readonly string _path;
        private readonly string _key;

        public CryptoService(string path, string key)
        {
            _path = path;
            _key = key;
        }

        public long Encrypt(string file)
        {
            if (!File.Exists(_path))
            {
                Debug.WriteLine($"[CRYPTO] CryptoSoft.exe NOT FOUND at: {_path}");
                return -1;
            }

            Debug.WriteLine($"[CRYPTO] Starting encryption: {file}");
            Debug.WriteLine($"[CRYPTO] CryptoSoft path: {_path}");

            int maxAttempts = 50;
            int currentAttempt = 0;

            while (currentAttempt < maxAttempts)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = _path,
                        Arguments = $"\"{file}\" \"{_key}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        
                        WorkingDirectory = Path.GetDirectoryName(_path),
                        
                        RedirectStandardOutput = true, // On désactive pour éviter les blocages de buffer
                        RedirectStandardError = false
                    };

                    using (Process p = Process.Start(startInfo))
                    {
                        if (p != null)
                        {
                            p.WaitForExit();
                            Debug.WriteLine($"[CRYPTO] CryptoSoft exited with code: {p.ExitCode}");

                            // CAS 1 : Succès (ExitCode >= 0)
                            if (p.ExitCode >= 0)
                            {
                                Debug.WriteLine($"[CRYPTO] Encryption SUCCESS: {p.ExitCode}ms");
                                return p.ExitCode;
                            }

                            // CAS 2 : Le Mutex est occupé (Code -3)
                            if (p.ExitCode == -3)
                            {
                                Debug.WriteLine($"[CRYPTO] Mutex busy, retrying... ({currentAttempt + 1}/{maxAttempts})");
                                currentAttempt++;
                                Thread.Sleep(100);
                                continue;
                            }

                            // Si code d'erreur fatal (ex: -1), on ne boucle pas
                            Debug.WriteLine($"[CRYPTO] Fatal error: {p.ExitCode}");
                            return -1;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CRYPTO ERROR] {ex.Message}");
                    return -1;
                }
            }
            Debug.WriteLine($"[CRYPTO] Max attempts reached, giving up");
            return -1;
        }
    }
}

//Pilote l'exécution de CryptoSoft en mode invisible avec gestion des accès concurrents.
//Implémente une boucle de 50 tentatives pour résoudre les conflits liés au Mutex (Code -3).
//Assure la sécurité des chemins via des arguments formatés et retourne le temps de traitement en ms.
using System;
using System.Diagnostics;
using System.IO;

namespace EasyLibrary.Services;

public class CryptoService
{
    private readonly string _path;
    private readonly string _key;

    public CryptoService(string path, string key)
    {
        _path = path;
        _key = key;
    }

    public bool ShouldEncrypt(string filePath, List<string> allowedExtensions)
    {
        // On récupère l'extension du fichier (ex: ".txt")
        string extension = Path.GetExtension(filePath).ToLower();

        // On vérifie si cette extension est dans la liste autorisée
        return allowedExtensions.Contains(extension);
    }

    public long Encrypt(string file)
    {
        if (!OperatingSystem.IsWindows())
        {
            Process.Start("chmod", $"+x \"{_path}\"").WaitForExit();
        }

        Stopwatch sw = Stopwatch.StartNew();
        
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _path,
                Arguments = $"\"{file}\" \"{_key}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                sw.Stop();// arrêt du chrono
                         
                if (process.ExitCode != 0) return -1;   // Si CryptoSoft renvoie une erreur (différent de 0), on retourne -1
                return sw.ElapsedMilliseconds; // Succès : on retourne le temps (>0)
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erreur lors de l'appel CryptoSoft : {ex.Message}");
            return -2; // Erreur système : on retourne -2 (<0)
        }
    }
}

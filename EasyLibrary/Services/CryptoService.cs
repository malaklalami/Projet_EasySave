using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;

namespace EasySave.Services;

public class CryptoService
{
    private readonly string _path;
    private readonly string _key;

    public CryptoService(string path, string key) { _path = path; _key = key; }

    public long Encrypt(string file)
    {
        if (!File.Exists(_path)) return -1;
        var sw = Stopwatch.StartNew();
        try
        {
            var start = new ProcessStartInfo { FileName = _path, Arguments = $"\"{file}\" \"_key\"", CreateNoWindow = true, UseShellExecute = false };
            using var p = Process.Start(start);
            p?.WaitForExit();
            return p?.ExitCode == 0 ? sw.ElapsedMilliseconds : -1;
        }
        catch { return -2; }
    }
}//Pilote l'exécutable externe CryptoSoft. Il lui envoie les fichiers et récupère le temps de chiffrement
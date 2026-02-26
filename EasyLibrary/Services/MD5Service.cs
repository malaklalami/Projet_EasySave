using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace EasySave.Services;

public static class Md5Service
{
    private static readonly string _cachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "md5_cache.json");
    private static Dictionary<string, Dictionary<string, string>> _cache = new();

    // Initialise le cache au démarrage
    public static void LoadCache()
    {
        if (File.Exists(_cachePath))
        {
            try { _cache = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(_cachePath)) ?? new(); }
            catch { _cache = new(); }
        }
    }

    // Sauvegarde le cache à la fin
    public static void SaveCache() => File.WriteAllText(_cachePath, JsonSerializer.Serialize(_cache));

    // Calcule le hash d'un fichier
    public static string GetHash(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    // Vérifie si le fichier a changé par rapport au cache
    public static bool HasChanged(string jobName, string filePath, string currentHash)
    {
        if (!_cache.ContainsKey(jobName)) _cache[jobName] = new();

        bool changed = !_cache[jobName].TryGetValue(filePath, out string? oldHash) || oldHash != currentHash;

        // On met à jour le cache directement
        if (changed) _cache[jobName][filePath] = currentHash;

        return changed;
    }
}
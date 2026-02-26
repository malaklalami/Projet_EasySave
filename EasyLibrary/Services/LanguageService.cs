using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EasySave.Services;

public class LanguageService
{
    private Dictionary<string, string> _translations = new();

    public void Load(string langCode)
    {
        // On cherche maintenant dans le sous-dossier "Resources" du répertoire d'exécution
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", $"{langCode}.json");

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                _translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
            catch (Exception ex)
            {
                // Debug : Affiche l'erreur si le JSON est mal formé
                Console.WriteLine($"[ERROR] JSON Corrompu : {ex.Message}");
                _translations = new Dictionary<string, string>();
            }
        }
        else
        {
            // Debug : Affiche où l'application cherche REELLEMENT les fichiers
            Console.WriteLine($"[DEBUG] Fichier introuvable à : {path}");
        }
    }
    

    public string Get(string key)
    {
        // Renvoie la traduction ou la clé si introuvable (pour débugger facilement)
        return _translations.TryGetValue(key, out string? value) ? value : $"[{key}]";
    }
}
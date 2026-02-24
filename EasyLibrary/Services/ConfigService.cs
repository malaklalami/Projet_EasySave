using EasySave.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization; // AJOUT INDISPENSABLE POUR LES ENUMS

namespace EasySave.Services;

public class ConfigService
{
    private readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    private Settings? _cache;

    // 1. On centralise les options pour qu'elles soient identiques en lecture et écriture
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true, // Pour que le fichier soit beau dans Notepad
        Converters = { new JsonStringEnumConverter() } // LE SECRET EST ICI : Transforme les chiffres en texte (ex: "Json")
    };

    public Settings Current => _cache ?? Load();

    public Settings Load()
    {
        if (!File.Exists(_path))
        {
            _cache = new Settings();
        }
        else
        {
            try
            {
                // 2. On utilise les options pour lire
                _cache = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path), _jsonOptions) ?? new Settings();
            }
            catch
            {
                // 3. Sécurité : Si le fichier JSON est corrompu/illisible, on évite le crash et on met les valeurs par défaut
                _cache = new Settings();
            }
        }
        return _cache;
    }

    public void Save()
    {
        // 4. On utilise les mêmes options pour sauvegarder !
        File.WriteAllText(_path, JsonSerializer.Serialize(_cache, _jsonOptions));
    }
}
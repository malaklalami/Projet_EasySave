using EasySave.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasySave.Services;

public class ConfigService
{
    private readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    private Settings? _cache;

    // PISTE ACTIVÉE : Permet de prévenir l'UI ou le moteur quand on change un réglage
    public event Action? OnSettingsChanged;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
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
                _cache = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path), _jsonOptions) ?? new Settings();
            }
            catch
            {
                _cache = new Settings(); // Sécurité anti-crash si le JSON est mal écrit
            }
        }
        return _cache;
    }

    public void Save()
    {
        // Sauvegarde physique sur le disque
        File.WriteAllText(_path, JsonSerializer.Serialize(_cache, _jsonOptions));

        // ON PRÉVIENT LES AUTRES : 
        // Si quelqu'un écoute (l'UI par exemple), on lance l'alerte
        OnSettingsChanged?.Invoke();
    }
}
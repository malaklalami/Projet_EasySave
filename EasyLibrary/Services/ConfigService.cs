using EasySave.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;

namespace EasySave.Services;

public class ConfigService
{
    private readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    private Settings? _cache;

    public Settings Current => _cache ?? Load();

    public Settings Load()
    {
        if (!File.Exists(_path)) _cache = new Settings();
        else _cache = JsonSerializer.Deserialize<Settings>(File.ReadAllText(_path)) ?? new Settings();
        return _cache;
    }

    public void Save() => File.WriteAllText(_path, JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true }));
}
//Charge settings.json. Il contient le Cache : au lieu de relire le fichier sur le disque à chaque fois, il garde les réglages en ram
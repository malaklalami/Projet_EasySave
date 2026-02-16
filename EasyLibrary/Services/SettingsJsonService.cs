//SettingsJsonService.cs
using System;
using System.Text.Json;
using System.IO;
using EasyLibrary.Models;

namespace EasyLibrary.Services
{
	public class SettingsJsonService
	{
        private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        // On remplace "ConsoleSettings" par ton vrai nom de classe "ConsoleSettingsJson"
        public void Save(ConsoleSettingsJson settings)
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        // Ici aussi, le type de retour doit être "ConsoleSettingsJson"
        public ConsoleSettingsJson Load()
        {
            if (!File.Exists(_filePath)) return new ConsoleSettingsJson();

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<ConsoleSettingsJson>(json) ?? new ConsoleSettingsJson();
        }
    }
}


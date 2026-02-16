using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasySave.Models;

namespace EasySave.Services;

public class LoggerService
{
    public void Write(LogEntry entry, bool isJson)
    {
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd}.{(isJson ? "json" : "xml")}");
        entry.Timestamp = DateTime.Now.ToString("G");

        lock (this)
        {
            if (isJson)
            {
                var logs = File.Exists(path) ? JsonSerializer.Deserialize<List<LogEntry>>(File.ReadAllText(path)) : new List<LogEntry>();
                logs!.Add(entry);
                File.WriteAllText(path, JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true }));
            }
            // on doit ajouter la logique pour le XMl ici
        }
    }//Écrit physiquement les logs sur le disque. Il gère le choix entre JSON et XML de manière isolée
}
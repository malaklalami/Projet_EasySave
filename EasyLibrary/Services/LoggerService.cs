using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using EasySave.Models;

namespace EasySave.Services;


public class LoggerService
{
    // L'objet qui sert de verrou (unique pour toute l'application)
    private static readonly object _fileLock = new object();

    public void Write(LogEntry entry, bool isJson)
    {
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd}.{(isJson ? "json" : "xml")}");
        entry.Timestamp = DateTime.Now.ToString("G");

        lock (_fileLock)
        {
            if (isJson)
            {
                var logs = File.Exists(path) ? JsonSerializer.Deserialize<List<LogEntry>>(File.ReadAllText(path)) : new List<LogEntry>();
                logs!.Add(entry);
                File.WriteAllText(path, JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true }));
            }
            else
            {
                // Logique XML
                XDocument doc = File.Exists(path) ? XDocument.Load(path) : new XDocument(new XElement("Logs"));
                doc.Root?.Add(new XElement("LogEntry",
                    new XElement("JobName", entry.JobName),
                    new XElement("Source", entry.Source),
                    new XElement("Target", entry.Target),
                    new XElement("Timestamp", DateTime.Now.ToString("G"))
                ));
                doc.Save(path);
            }
            
        }
    }
}
//Écrit physiquement les logs sur le disque. Il gère le choix entre JSON et XML de manière isolée
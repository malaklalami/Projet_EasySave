using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using EasySave.Models;

namespace EasySave.Services;


public class LoggerService
{
    // Verrou statique pour éviter les conflits d'accès fichier
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
                try
                {
                    var logs = File.Exists(path) ? JsonSerializer.Deserialize<List<LogEntry>>(File.ReadAllText(path)) : new List<LogEntry>();
                    logs!.Add(entry);
                    
                    // Écriture atomique via fichier temporaire
                    string tempPath = path + ".tmp";
                    File.WriteAllText(tempPath, JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true }));
                    File.Delete(path);
                    File.Move(tempPath, path);
                }
                catch
                {
                    // Si fichier corrompu ou autre erreur, on réinitialise
                    var logs = new List<LogEntry> { entry };
                    File.WriteAllText(path, JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            else
            {
                try
                {
                    // Charge le doc XML existant ou en crée un nouveau
                    XDocument doc = File.Exists(path) ? XDocument.Load(path) : new XDocument(new XElement("Logs"));
                    doc.Root?.Add(new XElement("LogEntry",
                        new XElement("JobName", entry.JobName),
                        new XElement("Source", entry.Source),
                        new XElement("Target", entry.Target),
                        new XElement("Timestamp", DateTime.Now.ToString("G"))
                    ));
                    
                    // Écriture atomique (temp → final)
                    string tempPath = path + ".tmp";
                    doc.Save(tempPath);
                    File.Delete(path);
                    File.Move(tempPath, path);
                }
                catch
                {
                    // Fichier XML corrompu : création d'un nouveau avec l'entry actuelle
                    var newDoc = new XDocument(new XElement("Logs",
                        new XElement("LogEntry",
                            new XElement("JobName", entry.JobName),
                            new XElement("Source", entry.Source),
                            new XElement("Target", entry.Target),
                            new XElement("Timestamp", DateTime.Now.ToString("G"))
                        )
                    ));
                    newDoc.Save(path);
                }
            }
        }
    }
}
// Gère l'écriture des logs en JSON ou XML
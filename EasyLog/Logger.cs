using System;
using System.IO;
using System.Text.Json;

namespace EasyLog;

public class Logger
{
    // Dossier Logs à côté de l'exécutable
    private readonly string _logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

    // Langue courante (par défaut "fr"). Peut être modifiée depuis le ViewModel.
    public string CurrentLanguage { get; set; } = "fr";

    public Logger(string currentLanguage = "fr")
    {
        CurrentLanguage = currentLanguage;
    }

    public void WriteLog(LogEntry entry)
    {
        // Message d'entrée selon la langue
        Console.WriteLine(CurrentLanguage == "fr"
            ? ">>>> écriture des logs : " + _logFolder
            : ">>>> writing logs : " + _logFolder);

        if (!Directory.Exists(_logFolder)) Directory.CreateDirectory(_logFolder);

        string fileName = $"{DateTime.Now:yyyy-MM-dd}.json";
        string filePath = Path.Combine(_logFolder, fileName);

        entry.Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        string jsonText = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });

        File.AppendAllText(filePath, jsonText + Environment.NewLine);

        // Message de confirmation selon la langue
        Console.WriteLine(CurrentLanguage == "fr"
            ? ">>>> FICHIER CREE AVEC SUCCES : " + filePath
            : ">>>> FILE CREATED SUCCESSFULLY : " + filePath);
    }
}
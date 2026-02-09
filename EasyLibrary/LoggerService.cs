using System.Text.Json;
using System.IO;
using EasyLibrary.Models;

namespace EasyLog;

public class LoggerService
{
    // On change le chemin pour qu'il crée un dossier "Logs" là où est ton projet
    // AppDomain.CurrentDomain.BaseDirectory = le dossier où ton .exe est exécuté
    private readonly string _logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

    public void WriteLog(LogEntry entry)
    {
        // 1. CE MESSAGE DOIT S'AFFICHER DANS TA CONSOLE NOIRE
        Console.WriteLine(">>>> TENTATIVE D'ECRITURE DU LOG DANS : " + _logFolder);

        if (!Directory.Exists(_logFolder)) Directory.CreateDirectory(_logFolder);

        string fileName = $"{DateTime.Now:yyyy-MM-dd}.json";
        string filePath = Path.Combine(_logFolder, fileName);

        entry.Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        string jsonText = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });

        File.AppendAllText(filePath, jsonText + Environment.NewLine);

        // 2. CE MESSAGE CONFIRME QUE LE FICHIER EST CREE
        Console.WriteLine(">>>> FICHIER CREE AVEC SUCCES : " + filePath);
    }
}
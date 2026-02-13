using System.Text.Json;
using System.IO;
using EasyLibrary.Models;
using System.Xml.Serialization;

namespace EasyLibrary.Services;

public class LoggerService

{
    public string LogFormat { get; set; } = "json";
    // On change le chemin pour qu'il crée un dossier "Logs" là où est ton projet
    // AppDomain.CurrentDomain.BaseDirectory = le dossier où ton .exe est exécuté
    private readonly string _logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

    public void WriteLog(LogEntry entry)
    {
        // 1. Message de debug dans la console
        Console.WriteLine(">>>> TENTATIVE D'ECRITURE DU LOG DANS : " + _logFolder);

        // Créer le dossier Logs s'il n'existe pas
        if (!Directory.Exists(_logFolder))
        {
            Directory.CreateDirectory(_logFolder);
        }

        // v1.1 : On définit l'extension selon le format choisi
        string extension = LogFormat.ToLower();
        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + "." + extension;
        string filePath = Path.Combine(_logFolder, fileName);

        // On ajoute l'heure actuelle au log
        entry.Timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

        // 2. Logique de choix du format
        if (extension == "json")
        {
            // format JSON
            string jsonText = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });
            File.AppendAllText(filePath, jsonText + Environment.NewLine);
        }
        else
        {
            // format XML
            XmlSerializer serializer = new XmlSerializer(typeof(LogEntry));

            // On ouvre le fichier en mode "Append" (true)
            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                serializer.Serialize(sw, entry);
                sw.WriteLine(); // Pour séparer les logs
            }
        }

        // 3. Confirmation finale
        Console.WriteLine(">>>> FICHIER CREE AVEC SUCCES : " + filePath);
    }
}
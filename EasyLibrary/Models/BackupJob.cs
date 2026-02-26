using System.Text.Json.Serialization;
using EasySave.Core;

namespace EasySave.Models;

public class BackupJob
{
    // Ajout de l'ID pour la V3 (Essentiel pour le parallélisme et le fichier d'état)
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string SourceDir { get; set; } = string.Empty;
    public string TargetDir { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))] // Pour lire "Full" ou "Differential" dans le JSON
    public BackupType Type { get; set; } = BackupType.Full;
}
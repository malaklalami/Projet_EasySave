using System.Text.Json.Serialization;
using EasySave.Core;

namespace EasySave.Models;

public class BackupJob
{
    public string Name { get; set; } = string.Empty;
    public string SourceDir { get; set; } = string.Empty;
    public string TargetDir { get; set; } = string.Empty;
    public BackupType Type { get; set; } = BackupType.Full;

    // Chaque job peut play,pause,resume et cancel tout seul
    [JsonIgnore] // On ne veut pas sauvegarder ça dans le JSON
    public ManualResetEventSlim PauseEvent { get; } = new(true);

    [JsonIgnore]
    public CancellationTokenSource JobCts { get; set; } = new();

}
//Contient uniquement les paramètres d'un travail (Nom, Dossier Source, Dossier Cible, Type) c ce qui est stocké dans jobs.json
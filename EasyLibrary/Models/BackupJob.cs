using EasySave.Core;

namespace EasySave.Models;

public class BackupJob
{
    public string Name { get; set; } = string.Empty;
    public string SourceDir { get; set; } = string.Empty;
    public string TargetDir { get; set; } = string.Empty;
    public BackupType Type { get; set; } = BackupType.Full;
}
//Contient uniquement les paramètres d'un travail (Nom, Dossier Source, Dossier Cible, Type). C'est ce qui est stocké dans jobs.json.
using System;
namespace EasySave.Model
{
    public class BackUpJob
    {

        public string Name { get; set; } = string.Empty; // Nom de la sauvegarde
        public string SourceDir { get; set; } = string.Empty; // Dossier source de la sauvegarde
        public string TargetDir { get; set; } = string.Empty; // Destination de la sauvegarde
        public string BackUpType { get; set; } = string.Empty; // Type de sauvegarde

        // État en temps réel (évolutifs)
        public string State { get; set; } = "Inactive";  // "Active" ou "Inactive"
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public int Progress { get; set; }

        public BackUpJob() { }

        public BackUpJob(string name, string source, string target, string type)
        {
            Name = name;
            SourceDir = source;
            TargetDir = target;
            BackUpType = type;
            State = "Inactive"; // Par défaut au démarrage
            Progress = 0;
        }
    }
}


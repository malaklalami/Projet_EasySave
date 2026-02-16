//BackupJob.cs
using System;

namespace EasyLibrary.Models
{
    public class BackUpJob
    {
        public string Name { get; set; } // Nom de la sauvegarde
        public string SourceDir { get; set; } // Dossier source de la sauvegarde
        public string TargetDir { get; set; } // Destination de la sauvegarde
        public string BackUpType { get; set; } // Type de sauvegarde

        // État en temps réel (évolutifs)
        public string State { get; set; }      // "Active" ou "Inactive"
        public int TotalFiles { get; set; }
        public long TotalSize { get; set; }
        public int Progress { get; set; }

        // Pour le json qui charge les jobs
        public BackUpJob()
        {
        }

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


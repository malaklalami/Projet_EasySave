using EasySave.Core;
using System;
using System.Collections.Generic;

namespace EasySave.Models;

public class BackupState
{
    public Guid JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public JobState Status { get; set; } = JobState.Inactive;
    public int FilesProcessed { get; set; }
    public int TotalFiles { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public List<string> PendingFiles { get; set; } = new();
    public List<string> CompletedFiles { get; set; } = new();
    public List<string> FailedFiles { get; set; } = new();
    public DateTime LastUpdate { get; set; }

    public double Progress => TotalFiles > 0 ? (FilesProcessed / (double)TotalFiles) * 100 : 0;
}
//Contient ce qui change pendant une sauvegarde (progression %, fichier en cours, statut, fichiers). C'est ce qui va dans state.json

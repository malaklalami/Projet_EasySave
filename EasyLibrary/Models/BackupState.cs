using EasySave.Core;
using System;

namespace EasySave.Models;

public class BackupState
{
    public string JobName { get; set; } = string.Empty;
    public JobState Status { get; set; } = JobState.Inactive;
    public double Progress { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
    public DateTime LastUpdate { get; set; }
}
//Contient ce qui change pendant une sauvegarde (progression %, fichier en cours, statut). C'est ce qui va dans state.json
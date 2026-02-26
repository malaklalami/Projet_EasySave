using EasySave.Core;
using System;
using System.Collections.Generic;

namespace EasySave.Models;

public class BackupState
{
    // PISTE : Utilisation d'un ID numérique (index ou ID unique) plutôt que le nom
    public int JobId { get; set; }

    public JobState Status { get; set; } = JobState.Inactive;

    // PISTE : Progress supprimé (il sera calculé dynamiquement par rapport à la liste des fichiers)

    // PISTE : On stocke la liste des chemins des fichiers restants à copier
    public List<string> FilesToCopy { get; set; } = new();

    public DateTime LastUpdate { get; set; } = DateTime.Now;
}
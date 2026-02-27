using EasySave.Core;
using System;
using System.Collections.Generic;

namespace EasySave.Models;

public class BackupState
{
    // ID unique du travail pour le suivi en parallèle
    public int JobId { get; set; }

    // État actuel (Active, Paused, Inactive, Waiting)
    public JobState Status { get; set; } = JobState.Inactive;

    // Nombre total de fichiers au départ (pour calculer la progression)
    public int TotalFilesCount { get; set; }

    // Liste des chemins des fichiers restants à copier
    public List<string> FilesToCopy { get; set; } = new();

    // Horodatage de la dernière mise à jour
    public DateTime LastUpdate { get; set; } = DateTime.Now;
}
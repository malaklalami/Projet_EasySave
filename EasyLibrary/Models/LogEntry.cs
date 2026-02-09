using System;

namespace EasyLibrary.Models;

// Cette classe définit TOUTES les infos qu'on veut noter dans le journal
public class LogEntry
{
    public string Timestamp { get; set; }      // L'heure exacte
    public string JobName { get; set; }        // Le nom de la sauvegarde
    public string SourcePath { get; set; }     // D'où vient le fichier
    public string TargetPath { get; set; }     // Où il va
    public long FileSize { get; set; }         // Taille en octets
    public double TransferTimeMs { get; set; }  // Temps de copie (-1 si erreur)
}
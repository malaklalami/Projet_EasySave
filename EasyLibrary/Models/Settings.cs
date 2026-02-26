using EasySave.Core;
using System.Collections.Generic;

namespace EasySave.Models;

public class Settings
{
    // --- V1 & V2 ---
    public string Language { get; set; } = "fr";
    public LogFormat LogFormat { get; set; } = LogFormat.Json;
    public string BusinessSoftware { get; set; } = "Calculator";
    public List<string> EncryptionExtensions { get; set; } = new();

    // --- V3 : Centralisation des Logs (Docker) ---
    public LogTarget LogStrategy { get; set; } = LogTarget.Local;
    public string RemoteIp { get; set; } = "127.0.0.1";

    // --- V3 : Gestion des Priorités ---
    // Extensions prioritaires définies par l'utilisateur
    public List<string> PriorityExtensions { get; set; } = new() { ".docx", ".xlsx", ".pdf" };

    // --- V3 : Gestion du Parallélisme et Bande Passante ---
    // Nombre maximum de threads (Workers)
    public int MaxParallelFiles { get; set; } = 4;

    // Seuil "n Ko" pour définir un fichier volumineux (ex: 100 Ko)
    public long LargeFileThreshold { get; set; } = 100; // En Ko
}
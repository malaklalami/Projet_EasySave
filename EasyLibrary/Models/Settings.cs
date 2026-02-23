using EasySave.Core;
using System.Collections.Generic;

namespace EasySave.Models;

public class Settings
{
    public string Language { get; set; } = "fr";
    public LogFormat LogFormat { get; set; } = LogFormat.Json;
    public string BusinessSoftware { get; set; } = "Calculator";
    public List<string> EncryptionExtensions { get; set; } = new();
    public LogTarget LogStrategy { get; set; } = LogTarget.Local; // Par défaut en local
    public string RemoteIp { get; set; } = "127.0.0.1";

    // Liste des extensions prioritaires (ex: .docx, .pdf)
    public List<string> PriorityExtensions { get; set; } = new() { ".docx", ".xlsx", ".pdf" };

    // Nombre maximum de fichiers traités en parallèle
    public int MaxParallelFiles { get; set; } = 4;
    public long LargeFileThreshold { get; set; } = 100 * 1024; // Par défaut 100 Ko
}

//Stocke la langue, le format des logs, le nom du logiciel métier et les extensions à chiffrer
using EasySave.Core;
using System.Collections.Generic;

namespace EasySave.Models;

public class Settings
{
    public string Language { get; set; } = "fr";
    public LogFormat LogFormat { get; set; } = LogFormat.Json;
    public string BusinessSoftware { get; set; } = "Calculator";
    public List<string> EncryptionExtensions { get; set; } = new();
}

//Stocke la langue, le format des logs, le nom du logiciel métier et les extensions à chiffrer
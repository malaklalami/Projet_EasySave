namespace EasySave.Models;

public class LogEntry
{
    public string Timestamp { get; set; } = string.Empty;
    public string JobName { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public double TransferTimeMs { get; set; }
    public long EncryptionTimeMs { get; set; }
}

//Structure d'une ligne de log (horodatage, temps de transfert, taille du fichier)
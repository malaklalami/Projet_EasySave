namespace EasySave.Models;

public record TransferResult
{
    public string JobName { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Dest { get; init; } = string.Empty;
    public long Size { get; init; }
    public long TransferTimeMs { get; init; }
    public long EncryptionTimeMs { get; init; }
    public bool Success { get; init; }
}

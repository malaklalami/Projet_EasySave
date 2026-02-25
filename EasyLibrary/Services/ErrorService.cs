using System;
using System.IO;
using EasySave.Core;

namespace EasySave.Services;

public enum ErrorType
{
    BusinessSoftwareActive,
    DiskFull,
    SourceNotFound,
    AccessDenied,
    Unknown
}

public class ErrorEventArgs : EventArgs
{
    public string Message { get; set; } = "";
    public bool IsCritical { get; set; }
}

public static class ErrorService // On le passe en static pour plus de simplicité
{
    public static event EventHandler<ErrorEventArgs>? OnErrorDetected;

    // Cette propriété sera remplie par le MainViewModel au démarrage
    public static LanguageService? Language { get; set; }

    public static void Report(ErrorType type, string details, bool isCritical = false)
    {
        if (Language == null) return;

        // Récupération de la traduction
        string message = type switch
        {
            ErrorType.BusinessSoftwareActive => Language.Get("Software_Detected"),
            ErrorType.DiskFull => string.Format(Language.Get("Error_DiskFull"), details),
            ErrorType.SourceNotFound => string.Format(Language.Get("Error_SourceNotFound"), details),
            ErrorType.AccessDenied => string.Format(Language.Get("Error_AccessDenied"), details),
            _ => string.Format(Language.Get("Error_Unknown"), details)
        };

        // Envoi de l'événement vers la MainWindow
        OnErrorDetected?.Invoke(null, new ErrorEventArgs
        {
            Message = message,
            IsCritical = isCritical
        });
    }

    public static bool CheckDiskSpace(string targetPath, long requiredSpace)
    {
        try
        {
            string drive = Path.GetPathRoot(Path.GetFullPath(targetPath)) ?? "";
            if (string.IsNullOrEmpty(drive)) return true;

            DriveInfo driveInfo = new DriveInfo(drive);
            if (driveInfo.AvailableFreeSpace < requiredSpace)
            {
                Report(ErrorType.DiskFull, drive, true);
                return false;
            }
        }
        catch { }
        return true;
    }
}
// Centralise la gestion des erreurs et la vérification des ressources système (disque, accès, source).
// Traduit les messages d'erreurs en temps réel et les propage vers l'interface via un système d'événements.
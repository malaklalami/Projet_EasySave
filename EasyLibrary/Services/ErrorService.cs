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

//piste:public class ErrorService // On le passe en static pour plus de simplicité
{
   //piste: public event EventHandler<ErrorEventArgs>? OnErrorDetected;

    // Cette propriété sera remplie par le MainViewModel au démarrage
   //piste private LanguageService? LanguageService { get; set; }

   //piste public ErrorService(LanguageService languageService)
    {
       //piste: this.LanguageService = languageService;
    }

   //piste: public void Report(ErrorType type, string details, bool isCritical = false)
    {
    //piste:    if (LanguageService == null) return;

        // Récupération de la traduction
        string message = type switch
        {
      //piste:      ErrorType.BusinessSoftwareActive => LanguageService.Get("Software_Detected"),
    //piste:        ErrorType.DiskFull => string.Format(LanguageService.Get("Error_DiskFull"), details),
       //piste     ErrorType.SourceNotFound => string.Format(LanguageService.Get("Error_SourceNotFound"), details),
      //piste:      ErrorType.AccessDenied => string.Format(LanguageService.Get("Error_AccessDenied"), details),
         //piste:   _ => string.Format(LanguageService.Get("Error_Unknown"), details)
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
# Refactoring de BackupService - Version Simplifiée

## ?? Objectif

Rendre le code aussi simple et lisible que possible :
- ? **BackupService** = juste copier les fichiers
- ? **FileTaskScheduler** = organiser la file d'attente
- ? **MainViewModel** = orchestrer tout
- ? Noms de paramètres **explicites partout**

---

## ? Avant (Monolithe confus)

```csharp
// BackupService.cs (300+ lignes mélangées)
private void Dispatch(string s, string d, BackupJob j) // Paramètres abstraits
{
    // Logique de classement des files
}

private void FinalizeFile(FileTask task, long duration, long crypt, bool success)
{
    // 200 lignes : logs, état, chiffrement, rapport
    // Difficile de trouver ce qu'on cherche
}
```

---

## ? Après (3 services clairs)

### **1. BackupService** (~250 lignes)
```csharp
public class BackupService
{
    // Responsabilité unique : copier les fichiers
    
    private void WorkerLoop()
    {
        // Boucle principale du worker
    }
    
    private void CopyFile(FileTaskScheduler.FileTask fileTask)
    {
        // JUSTE : copie + chiffrement optionnel
        // Émet OnFileCompleted + OnProgress
    }
    
    private void ScanAndScheduleFiles(List<BackupJob> backupJobs)
    {
        // JUSTE : scanne et ajoute à la file
    }
}
```

### **2. FileTaskScheduler** (~150 lignes)
```csharp
public class FileTaskScheduler
{
    // Responsabilité unique : organiser la file d'attente
    
    public void ScheduleFile(string sourcePath, string destinationPath, BackupJob backupJob)
    {
        // Classe dans la bonne file
    }
    
    public FileTask? GetNextTask(out bool isLargeFile)
    {
        // Récupère selon la priorité
    }
}
```

### **3. MainViewModel** (Orchestrateur)
```csharp
public class MainViewModel
{
    public async Task ExecuteSelection(string input)
    {
        // 1. Lance BackupService
        _backup.AddJobs(jobsToRun);
        
        // 2. Reçoit les événements
        _backup.OnProgress += (state) => 
        {
            // 3. Appelle BackupReportingService
            _reporter.HandleFileCompleted(result);
            
            // 4. Expose à l'UI
            OnProgressUpdate?.Invoke(state);
        };
    }
}
```

---

## ?? Comparaison

| Aspect | Avant | Après |
|--------|-------|-------|
| **Taille BackupService** | 300+ lignes | 250 lignes (+ claire) |
| **Services séparés** | 1 monolithe | 2 petits services |
| **Noms des paramètres** | `s`, `d`, `j` | `sourcePath`, `destinationPath`, `backupJob` |
| **Responsabilités** | Mélangées | Séparées (copie ? orchestration) |
| **Testabilité** | Difficile | Facile |
| **Debuggage** | Confus (où est le bug ?) | Ciblé (aller dans le bon service) |

---

## ?? Ce que chaque service fait

### BackupService
- ? Lance les workers
- ? Scanne les fichiers à copier
- ? Copie les fichiers
- ? Chiffre si besoin
- ? Émet 2 événements : `OnFileCompleted` + `OnProgress`

### FileTaskScheduler
- ? Classe les fichiers en 4 files (priorité/taille)
- ? Gère l'accès exclusif aux gros fichiers
- ? Compte les fichiers complétés
- ? Protège tout avec mutex

### MainViewModel
- ? Gère les jobs (CRUD)
- ? Appelle `BackupService.AddJobs()`
- ? Reçoit les événements du BackupService
- ? Appelle les autres services (logs, état, moniteur)
- ? Expose `OnProgressUpdate` à l'UI

---

## ?? Flux Complet

```
User : "Run job 1"
  ?
MainViewModel.ExecuteSelection()
  ?? BackupService.AddJobs() ? Lance les workers
  ?
  ?? Workers commencent à copier
  ?  Chaque fichier copié ? BackupService émet OnFileCompleted
  ?
  ?? MainViewModel reçoit OnFileCompleted
  ?  ?? Appelle BackupReportingService (qui appelle LoggerService + PersistentTcpLogger)
  ?  ?? Appelle StateService (sauvegarde l'état)
  ?
  ?? MainViewModel expose OnProgressUpdate
      ?
      UI (Avalonia/Console) met à jour la progression
```

---

## ?? Comment Déboguer

### Bug : Les fichiers ne se copient pas
? Aller dans `BackupService.CopyFile()` (ligne ~170)

### Bug : La progression n'avance pas
? Aller dans `FileTaskScheduler.IncrementCompletedFiles()` (ligne ~120)

### Bug : Les logs ne s'écrivent pas
? Aller dans `BackupReportingService.HandleFileCompleted()` (pas dans BackupService)

---

## ? Points Clés

1. **BackupService n'appelle jamais LoggerService** ? C'est MainViewModel qui le fait
2. **FileTaskScheduler n'a aucune dépendance externe** ? Juste la ConfigService
3. **Tous les paramètres sont explicites** ? Jamais de `s`, `d`, `j`
4. **Chaque classe a une responsabilité unique** ? SOLID

---

## ?? Documentation

Lire `ARCHITECTURE.md` pour le guide complet de l'application.


# Architecture Simplifiée - Guide du Code

## ?? Vue d'ensemble

L'application EasySave est organisée autour de 3 éléments centraux :
1. **BackupService** : juste copier les fichiers en parallèle
2. **FileTaskScheduler** : organiser la file d'attente
3. **MainViewModel** : orchestrer tout (logs, crypto, moniteur, etc.)

---

## ?? Responsabilités par Service

### **BackupService** (~250 lignes)
**Rôle** : Copier des fichiers de A à B en parallèle.

**Ce qu'il fait** :
- ? Lance N workers parallèles
- ? Scanne les répertoires source
- ? Copie les fichiers via les workers
- ? Chiffre si besoin (via CryptoService)
- ? Émet 2 événements : `OnFileCompleted` et `OnProgress`

**Ce qu'il NE fait PAS** :
- ? Gérer les logs (MainViewModel l'appelle)
- ? Envoyer les logs réseau (MainViewModel l'appelle)
- ? Sauvegarder l'état (MainViewModel l'appelle)

**Paramètres explicites** :
```csharp
CopyFile(FileTaskScheduler.FileTask fileTask)
ScanAndScheduleFiles(List<BackupJob> backupJobs)
```

---

### **FileTaskScheduler** (~150 lignes)
**Rôle** : Organiser les fichiers à copier en 4 files de priorité.

**Ce qu'il fait** :
- ? Classe les fichiers : (priorité/normal) × (gros/petit)
- ? Gère le slot unique pour gros fichiers
- ? Compte les fichiers complétés (progression)
- ? Protège tout avec mutex/lock

**Méthodes** :
```csharp
ScheduleFile(sourcePath, destinationPath, backupJob)
GetNextTask(out isLargeFile)
IncrementCompletedFiles(jobId)
GetRemainingFilesForJob(jobId)
```

---

### **MainViewModel** (~200 lignes)
**Rôle** : Orchestrer l'application.

**Ce qu'il fait** :
- ? Gère les jobs (CRUD)
- ? Appelle `BackupService.AddJobs()` pour lancer la sauvegarde
- ? Reçoit les événements `OnFileCompleted` du BackupService
- ? Appelle `BackupReportingService` pour logs + état
- ? Gère le moniteur logiciel métier
- ? Expose `OnProgressUpdate` pour l'UI

**Flux** :
```
User click "Run" 
  ? MainViewModel.ExecuteSelection()
    ? BackupService.AddJobs() (lance les workers)
    ? BackupService émet OnProgress
    ? MainViewModel reçoit l'événement
    ? MainViewModel appelle BackupReportingService
    ? BackupReportingService appelle LoggerService + PersistentTcpLogger
    ? MainViewModel expose OnProgressUpdate vers l'UI
```

---

## ?? Flux Complet de Sauvegarde

```
??????????????????????????????????????????????????????????
?                    MainViewModel                       ?
?  (Orchestrateur : appelle BackupService + reporter)   ?
??????????????????????????????????????????????????????????
                           ?
                           ?? AddJobs()
                           ?
??????????????????????????????????????????????????????????
?                   BackupService                        ?
?   (Scanne + lance workers pour copier)                ?
??????????????????????????????????????????????????????????
                           ?
         ?????????????????????????????????????
         ?                 ?                 ?
    Worker 1          Worker 2          Worker 3
    (Copie)           (Copie)           (Copie)
         ?                 ?                 ?
         ?????????????????????????????????????
                           ?
                    Événement émis
              (OnFileCompleted, OnProgress)
                           ?
                           ?
                   MainViewModel
                   (reçoit l'événement)
                           ?
                           ?? BackupReportingService
                           ?   ?? LoggerService (local)
                           ?   ?? PersistentTcpLogger (réseau)
                           ?
                           ?? StateService (sauvegarde état)
                                   ?
                                   ?
                           MainViewModel.OnProgressUpdate
                                   ?
                                   ?
                           UI (Avalonia / Console)
```

---

## ?? Fichiers et leurs responsabilités

| Fichier | Lignes | Responsabilité |
|---------|--------|-----------------|
| `BackupService.cs` | 250 | Copie de fichiers + workers |
| `FileTaskScheduler.cs` | 150 | Organisation des files |
| `MainViewModel.cs` | 200 | Orchestration |
| `BackupReportingService.cs` | 50 | Logs + état |
| `LoggerService.cs` | 80 | Sauvegarde en XML/JSON |
| `ClientServer.cs` | 40 | Envoi réseau des logs |
| `JobManager.cs` | 50 | CRUD des jobs |
| `StateService.cs` | 50 | Sauvegarde de l'état |
| `CryptoService.cs` | ? | Chiffrement |
| `BusinessSoftwareMonitor.cs` | ? | Détection logiciel métier |

---

## ?? Comment déboguer

### ? Bug : Les fichiers ne se copient pas
**Aller dans** : `BackupService.CopyFile()`
```csharp
private void CopyFile(FileTaskScheduler.FileTask fileTask)
{
    // Vérifier :
    // 1. File.Copy() réussit ?
    // 2. Répertoire destination créé ?
    // 3. Chiffrement en erreur ?
}
```

---

### ? Bug : Les logs ne s'écrivent pas
**Aller dans** : `MainViewModel.ExecuteSelection()`
? Puis `BackupReportingService.HandleFileCompleted()`
? Puis `LoggerService.Write()`

---

### ? Bug : La progression saute/redescend
**Aller dans** : `FileTaskScheduler.IncrementCompletedFiles()`
```csharp
public int IncrementCompletedFiles(int jobId)
{
    // Le compteur doit être monotone (croissant)
    // S'il redescend, c'est qu'on l'a réinitialisé mal
}
```

---

## ? Noms Explicites Partout

Chaque paramètre a un nom qui dit exactement ce qu'il est :

```csharp
// ? Explicite
CopyFile(FileTaskScheduler.FileTask fileTask)
ScheduleFile(string sourcePath, string destinationPath, BackupJob backupJob)
ScanAndScheduleFiles(List<BackupJob> backupJobs)
DetermineJobStatus(BackupJob backupJob, int remainingFileCount)

// ? Plus de trucs comme ça :
// CopyFile(FileTask t)
// ScheduleFile(string s, string d, BackupJob j)
```

---

## ?? Ajouter une Nouvelle Fonctionnalité

### Exemple : Notifier une API au lieu de logs locaux

**Option 1 : Modifier BackupReportingService** (mauvaise idée)
```csharp
// ? Ajouter la logique API ici mélange les responsabilités
```

**Option 2 : Ajouter dans MainViewModel** (bonne idée)
```csharp
// ? Dans MainViewModel.ExecuteSelection() :
_backup.OnProgress += (state) => 
{
    OnProgressUpdate?.Invoke(state);
    NotifyRemoteAPI(state);  // ?? Nouvelle logique ici
};
```

---

## ?? Résumé

- **BackupService** = La pompe (copie les fichiers)
- **FileTaskScheduler** = L'organisateur (trie la file)
- **MainViewModel** = Le chef d'orchestre (appelle tout)
- **Autres services** = Spécialistes (logs, état, crypto, etc.)

Chaque classe a **une** responsabilité claire, des **noms explicites**, et c'est facile à **déboguer**.

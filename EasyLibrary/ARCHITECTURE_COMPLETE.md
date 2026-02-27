# Architecture EasySave - Documentation Complète

## ?? Vue d'ensemble Globale

EasySave est une application de sauvegarde avec les couches suivantes :

```
???????????????????????????????????????????????????????????????
?                       INTERFACE UTILISATEUR                 ?
?              (EasyAvalonia / EasyConsole / WPF)             ?
???????????????????????????????????????????????????????????????
                               ??
???????????????????????????????????????????????????????????????
?                       VIEWMODEL LAYER                       ?
?                   MainViewModel (Orchestrateur)             ?
?  - Gère les jobs (CRUD)                                     ?
?  - Appelle BackupService.AddJobs()                          ?
?  - Appelle les services spécialisés                         ?
?  - Expose OnProgressUpdate à l'UI                           ?
???????????????????????????????????????????????????????????????
                               ??
???????????????????????????????????????????????????????????????
?                      SERVICE LAYER                          ?
?                                                             ?
?  1. BackupService                                           ?
?     ?? ScanAndScheduleFiles() : Scanne les fichiers        ?
?     ?? WorkerLoop() : Boucle principale des workers        ?
?     ?? CopyFile() : Copie + chiffrement                    ?
?                                                             ?
?  2. FileTaskScheduler                                       ?
?     ?? ScheduleFile() : Classe les fichiers               ?
?     ?? GetNextTask() : Récupère selon priorité            ?
?     ?? IncrementCompletedFiles() : Tracking progression    ?
?                                                             ?
?  3. BackupReportingService                                  ?
?     ?? HandleFileCompleted() : Reçoit l'événement         ?
?     ?? Appelle LoggerService + PersistentTcpLogger         ?
?                                                             ?
?  4. LoggerService                                           ?
?     ?? Write() : Sauvegarde XML/JSON                       ?
?                                                             ?
?  5. StateService                                            ?
?     ?? UpdateState() : Sauvegarde l'état                   ?
?                                                             ?
?  6. CryptoService                                           ?
?     ?? Encrypt() : Chiffre les fichiers                    ?
?                                                             ?
?  7. BusinessSoftwareMonitor                                 ?
?     ?? UpdateControlState() : Détecte logiciel métier      ?
?                                                             ?
?  8. PersistentTcpLogger (ClientServer.cs)                   ?
?     ?? SendLog() : Envoie logs réseau à ConsoleDeportee    ?
?                                                             ?
?  9. ConfigService                                           ?
?     ?? Gère la configuration et les paramètres             ?
?                                                             ?
?  10. JobManager                                             ?
?      ?? Gère CRUD des jobs                                 ?
???????????????????????????????????????????????????????????????
                               ??
???????????????????????????????????????????????????????????????
?                       MODEL LAYER                           ?
?                                                             ?
?  - BackupJob : Définition d'un job de sauvegarde          ?
?  - BackupState : État actuel du job                        ?
?  - TransferResult : Résultat d'un fichier copié           ?
?  - LogEntry : Entrée de log                                ?
?  - Settings : Configuration                                ?
?                                                             ?
?  Enums :                                                    ?
?  - BackupType (Full, Differential)                         ?
?  - JobState (Active, Paused, Stopped, Inactive, Waiting)   ?
?  - LogTarget (Local, Remote, Both)                         ?
?  - LogFormat (Xml, Json)                                   ?
???????????????????????????????????????????????????????????????
```

---

## ?? Flux Complet de Sauvegarde

```
1. USER
   ?
   ??? "Run jobs 1-3"
       ?
       ?
2. INTERFACE (Avalonia / Console)
   ?
   ??? MainViewModel.ExecuteSelection("1-3")
       ?
       ?? Parse la sélection ? [job1, job2, job3]
       ?
       ?
3. MAINVIEWMODEL
   ?
   ?? BackupService.AddJobs([job1, job2, job3])
   ?  ?
   ?  ?? ScanAndScheduleFiles()
   ?  ?  ?? Charge les états sauvegardés
   ?  ?  ?? Pour chaque job :
   ?  ?  ?  ?? Si en pause : reprend les fichiers en pause
   ?  ?  ?  ?? Sinon : scanne tous les fichiers
   ?  ?  ?      ?? Si différentiel : filtre par MD5
   ?  ?  ?? ScheduleFile(sourcePath, destPath, job)
   ?  ?     ?? FileTaskScheduler classe dans la bonne file
   ?  ?
   ?  ?? WorkerLoop() x N workers
   ?     ?? Boucle infinie :
   ?        ?? GetNextTask() de FileTaskScheduler
   ?        ?? WaitIfPaused()
   ?        ?? CopyFile(fileTask)
   ?        ?  ?? File.Copy()
   ?        ?  ?? Si extension chiffrement : CryptoService.Encrypt()
   ?        ?  ?? Mesure le temps
   ?        ?  ?? Émet OnFileCompleted + OnProgress
   ?        ?
   ?        ?? ReleaseLargeFileSlot() si c'était un gros fichier
   ?
   ?? Reçoit OnFileCompleted
   ?  ?
   ?  ?? BackupReportingService.HandleFileCompleted(result)
   ?     ?? Si LogTarget = Local/Both : LoggerService.Write()
   ?     ?  ?? Sauvegarde XML/JSON
   ?     ?
   ?     ?? Si LogTarget = Remote/Both : PersistentTcpLogger.SendLog()
   ?        ?? Envoie JSON au serveur ConsoleDeportee:11000
   ?
   ?? Reçoit OnProgress
   ?  ?
   ?  ?? StateService.UpdateState() ? state.json
   ?  ?
   ?  ?? MainViewModel.OnProgressUpdate?.Invoke()
   ?     ?
   ?     ?
4. INTERFACE
   ?
   ??? Met à jour la barre de progression
       ?? Calcul : FilesCompleted / TotalFilesCount * 100
       ?? Affiche % et statut
```

---

## ??? Services Détaillés

### **1. BackupService** (~250 lignes)
**Responsabilité** : Copier les fichiers de A à B

**Méthodes publiques** :
```csharp
AddJobs(List<BackupJob> backupJobs)      // Lance la sauvegarde
ForcePulse()                               // Réveille les workers
```

**Événements** :
```csharp
event Action<TransferResult>? OnFileCompleted;  // Un fichier est copié
event Action<BackupState>? OnProgress;          // État du job mis à jour
```

**Dépendances** :
- `ConfigService` : pour MaxParallelFiles, EncryptionExtensions, LargeFileThreshold
- `CryptoService` : pour chiffrer
- `BusinessSoftwareMonitor` : pour mettre à jour l'état
- `FileTaskScheduler` : pour organiser les files

---

### **2. FileTaskScheduler** (~150 lignes)
**Responsabilité** : Organiser les fichiers en 4 files

**Files** :
```
_priorityLargeFiles       (priorité + gros)
_prioritySmallFiles       (priorité + petit)
_nonPriorityLargeFiles    (normal + gros, slot unique)
_nonPrioritySmallFiles    (normal + petit)
```

**Méthodes publiques** :
```csharp
ScheduleFile(sourcePath, destPath, backupJob)      // Ajoute à la file
FileTask? GetNextTask(out isLargeFile)              // Récupère selon priorité
IncrementCompletedFiles(jobId)                      // Incrémente compteur
GetRemainingFilesForJob(jobId)                      // Fichiers restants
ClearQueues(backupJobs)                             // Réinitialise
ReleaseLargeFileSlot()                              // Libère le slot
WakeupWaitingWorkers()                              // Réveille les workers
WaitForTask(timeoutMs)                              // Fait attendre
```

**Protégé par** : `object _lock` (mutex)

---

### **3. MainViewModel** (~200 lignes)
**Responsabilité** : Orchestrer l'application

**Services appelés** :
- `BackupService` : lance les sauvegardes
- `BackupReportingService` : reçoit les logs
- `StateService` : sauvegarde l'état
- `JobManager` : gère les jobs
- `BusinessSoftwareMonitor` : détecte logiciel métier
- `ConfigService` : configuration
- `LanguageService` : traduction

**Méthodes** :
```csharp
ExecuteSelection(string input)      // Lance une sauvegarde
PauseAll() / ResumeAll()            // Contrôle global
StopAll()                           // Arrête tout
AddJob() / EditJob() / DeleteJob()  // CRUD
```

**Événement** :
```csharp
OnProgressUpdate?.Invoke(state)     // Notifie l'UI
```

---

### **4. BackupReportingService** (~50 lignes)
**Responsabilité** : Rapporter les événements de sauvegarde

**Flux** :
```
BackupService.OnFileCompleted
    ? BackupReportingService.HandleFileCompleted()
        ?? Si LogTarget.Local/Both : LoggerService.Write()
        ?? Si LogTarget.Remote/Both : PersistentTcpLogger.SendLog()
```

---

### **5. LoggerService** (~80 lignes)
**Responsabilité** : Sauvegarder les logs en XML/JSON

**Méthode** :
```csharp
Write(LogEntry entry, bool isJson)
```

**Sécurité** :
- Écriture atomique (fichier temp ? renommage)
- Gestion des fichiers corrompus (régénération)
- Verrou statique `_fileLock`

**Sorties** :
- `Logs/YYYY-MM-DD.xml`
- `Logs/YYYY-MM-DD.json`

---

### **6. StateService** (~50 lignes)
**Responsabilité** : Sauvegarder l'état des jobs

**Méthode** :
```csharp
UpdateState(BackupState state)
List<BackupState> ReadStates()
```

**Sortie** : `state.json`

---

### **7. PersistentTcpLogger (ClientServer.cs)** (~40 lignes)
**Responsabilité** : Envoyer les logs au serveur réseau

**Méthodes** :
```csharp
ConnectAsync(ip = "127.0.0.1")      // Se connecte au serveur
SendLog(LogEntry entry)              // Envoie un log
```

**Serveur** : ConsoleDeportee:11000

---

### **8. CryptoService** (~? lignes)
**Responsabilité** : Chiffrer les fichiers

**Méthode** :
```csharp
long Encrypt(string filePath)        // Retourne le temps de chiffrement
```

**Exécutable** : CryptoSoft.exe (process externe)

---

### **9. BusinessSoftwareMonitor** (~? lignes)
**Responsabilité** : Détecter le logiciel métier

**Méthode** :
```csharp
UpdateControlState(List<BackupJob> jobs)
```

**Événement** :
```csharp
OnSoftwareDetectionChanged?.Invoke(isDetected)
```

---

## ?? Models

### **BackupJob**
```csharp
int Id
string Name
string SourceDir
string TargetDir
BackupType Type          // Full ou Differential
bool IsPaused
bool IsStopped
int TotalFilesForThisJob
```

### **BackupState**
```csharp
int JobId
JobState Status          // Active, Paused, Stopped, Inactive
int TotalFilesCount
List<string> FilesToCopy // Fichiers restants
int FilesCompleted       // Fichiers traités (progression)
DateTime LastUpdate
```

### **TransferResult**
```csharp
string JobName
string Source
string Dest
long Size
long TransferTimeMs
long EncryptionTimeMs
bool Success
```

### **LogEntry**
```csharp
string JobName
string Source
string Target
long FileSize
long TransferTimeMs
long EncryptionTimeMs
string Timestamp
```

---

## ?? Enums

### **BackupType**
```
Full        ? Copie tous les fichiers
Differential ? Copie seulement les fichiers modifiés (MD5)
```

### **JobState**
```
Active   ? En cours de traitement
Paused   ? En pause
Stopped  ? Arrêté
Inactive ? Terminé
Waiting  ? En attente (inutilisé actuellement)
```

### **LogTarget**
```
Local   ? Sauvegarde locale (XML/JSON)
Remote  ? Envoi réseau (ConsoleDeportee)
Both    ? Local + Remote
```

### **LogFormat**
```
Xml     ? Format XML
Json    ? Format JSON
```

---

## ?? Contrôles et Synchronisation

### **JobControlService** (Service statique)
```csharp
// Contrôle global
IsStoppedAll        // Tous les jobs arrêtés
IsPausedAll         // Tous les jobs en pause

// Contrôle par job
job.IsStopped       // Ce job est arrêté
job.IsPaused        // Ce job est en pause

// Méthodes
PauseAll()          // Marquer tous en pause
ResumeAll(jobs)     // Reprendre tous
StopAll()           // Arrêter tous
WaitIfPaused()      // Bloquer si en pause
```

### **Synchronisation**
```
BackupService
?? object _lock (FileTaskScheduler)
?? Mutex _fileMutex (LoggerService)

ConsoleDeportee
?? Mutex _fileMutex (protection écriture fichier multi-clients)
```

---

## ?? Progression Monotone

**Avant** ? :
```
Progression = (TotalFiles - FilesToCopy.Count) / TotalFiles
? Peut fluctuer (0%, 50%, 40%, 50%) avec workers parallèles
```

**Après** ? :
```
Progression = FilesCompleted / TotalFiles
? Toujours croissant (0%, 10%, 20%, 30%, 40%, 50%, 100%)
```

**Implémentation** :
- `FileTaskScheduler._filesCompletedByJob` : dictionnaire thread-safe
- `IncrementCompletedFiles()` : incrémente sous verrou

---

## ?? Arborescence des Fichiers

```
EasyLibrary/
?? Services/
?  ?? BackupService.cs              (250 lignes, copie)
?  ?? FileTaskScheduler.cs          (150 lignes, files)
?  ?? BackupReportingService.cs     (50 lignes, rapports)
?  ?? LoggerService.cs              (80 lignes, logs)
?  ?? StateService.cs               (50 lignes, état)
?  ?? ClientServer.cs               (40 lignes, TCP)
?  ?? CryptoService.cs              (? lignes, chiffrement)
?  ?? BusinessSoftwareMonitor.cs    (? lignes, monitoring)
?  ?? ConfigService.cs
?  ?? JobManager.cs
?
?? ViewModels/
?  ?? MainViewModel.cs              (200 lignes, orchestration)
?
?? Models/
?  ?? BackupJob.cs
?  ?? BackupState.cs
?  ?? TransferResult.cs
?  ?? LogEntry.cs
?
?? Core/
?  ?? JobState.cs                   (Enum)
?  ?? BackupType.cs                 (Enum)
?  ?? LogTarget.cs                  (Enum)
?  ?? LogFormat.cs                  (Enum)
?  ?? JobControlService.cs          (Service statique)
?
?? ARCHITECTURE.md                  (Ce document)
    REFACTORING_NOTES.md
```

---

## ? Règles de Code

1. **Noms explicites** : `sourcePath`, `destinationPath`, `backupJob` (jamais `s`, `d`, `j`)
2. **Une responsabilité par classe** : BackupService = copie, FileTaskScheduler = files, MainViewModel = orchestration
3. **Les services sont thread-safe** : Tous les accès partagés sont protégés par verrou
4. **Les événements remontent** : BackupService émet ? MainViewModel reçoit ? UI se met à jour
5. **Pas de dépendances circulaires** : A appelle B, mais B n'appelle pas A
6. **Tests faciles** : Chaque service peut être testé isolément

---

## ?? Résumé

| Aspect | Solution |
|--------|----------|
| **Copie parallèle** | `BackupService` + `N workers` |
| **Planification intelligente** | `FileTaskScheduler` (4 files, priorités) |
| **Progression lisse** | Compteur monotone `FilesCompleted` |
| **Orchestration** | `MainViewModel` centralise tout |
| **Logs locaux** | `LoggerService` (XML/JSON atomique) |
| **Logs réseau** | `PersistentTcpLogger` ? ConsoleDeportee |
| **Chiffrement** | `CryptoService` via CryptoSoft.exe |
| **Logiciel métier** | `BusinessSoftwareMonitor` (détection) |
| **État persistant** | `StateService` (state.json) |
| **Configuration** | `ConfigService` (settings.json) |

---

**À retenir** : Chaque classe a une responsabilité claire. Chercher un bug ? Aller dans le service responsable. Simple, efficace, maintenable.

# 🎓 ROADMAP SOUTENANCE - Comprendre le Code Ligne par Ligne

## ⏱️ Ordre de Lecture (1h30 max)

**Suivez cet ordre STRICT** pour comprendre progressivement :

```
JOUR 0 (Context)
  ├─ Lire README.md (comprendre le projet)
  └─ Lire ce document (la roadmap)

JOUR 1 (Models = Fondations)
  ├─ BackupJob.cs (5 min)
  ├─ BackupState.cs (5 min)
  ├─ TransferResult.cs (5 min)
  └─ Enums (JobState, BackupType, etc.) (5 min)

JOUR 2 (Services = La Logique)
  ├─ FileTaskScheduler.cs (15 min) ⭐⭐⭐ CRITIQUE
  ├─ BackupService.cs (20 min) ⭐⭐⭐ CRITIQUE
  ├─ LoggerService.cs (10 min)
  └─ ClientServer.cs (5 min)

JOUR 3 (Orchestration)
  ├─ MainViewModel.cs (15 min) ⭐⭐
  ├─ BackupReportingService.cs (5 min)
  └─ ARCHITECTURE_COMPLETE.md (20 min)

JOUR 4 (Intégration)
  ├─ Tracer un flux complet (30 min)
  └─ Répondre aux 10 questions du prof (30 min)
```

---

## 📊 JOUR 1 : Models (Fondations)

### **BackupJob.cs** (5 min)
**À quoi ça sert ?** = Représente une tâche de sauvegarde

```csharp
public class BackupJob
{
    public int Id { get; set; }                    // ✅ Identifiant unique du job
                                                   //    (ex: 1, 2, 3...)
                                                   //    Pourquoi ? Pour savoir quel job
                                                   //    on traite en parallèle

    public string Name { get; set; }               // ✅ Nom lisible
                                                   //    (ex: "Backup Photos")

    public string SourceDir { get; set; }          // ✅ Chemin source
                                                   //    (ex: "C:\Photos")

    public string TargetDir { get; set; }          // ✅ Chemin destination
                                                   //    (ex: "D:\Backup")

    public int TotalFilesForThisJob { get; set; }  // ✅ Nombre total de fichiers
                                                   //    (calculé au scan)

    [JsonIgnore]
    public bool IsPaused { get; set; }             // ✅ Est-ce en pause ?
                                                   //    [JsonIgnore] = pas sauvé au redémarrage

    [JsonIgnore]
    public bool IsStopped { get; set; }            // ✅ Est-ce arrêté ?
                                                   //    [JsonIgnore] = pas sauvé au redémarrage

    public BackupType Type { get; set; }           // ✅ Full ou Differential
                                                   //    Full = tout copier
                                                   //    Differential = seulement fichiers modifiés
}
```

**Question probable** : "À quoi sert `[JsonIgnore]` ?"
**Réponse** : On ne veut pas sauvegarder `IsPaused` et `IsStopped` dans le JSON. Si l'app redémarre, le job ne doit pas rester en pause.

---

### **BackupState.cs** (5 min)
**À quoi ça sert ?** = Représente l'état actuel d'un job pendant la sauvegarde

```csharp
public class BackupState
{
    public int JobId { get; set; }                 // ✅ Quel job ?
    public JobState Status { get; set; }           // ✅ État actuel (Active, Paused, Stopped, Inactive)
    public int TotalFilesCount { get; set; }       // ✅ Nombre total à copier
    public List<string> FilesToCopy { get; set; }  // ✅ Liste des fichiers restants
    public int FilesCompleted { get; set; }        // ✅ 🔑 PROGRESSION MONOTONE
                                                   //    Toujours croissant : 0 → 100
                                                   //    = nombre de fichiers déjà copiés
}
```

**Différence BackupJob vs BackupState** :
- `BackupJob` = configuration (ne change pas pendant sauvegarde)
- `BackupState` = progression actuelle (change à chaque fichier)

---

### **Enums** (5 min)
```csharp
// JobState.cs
public enum JobState
{
    Active,    // En cours
    Paused,    // En pause (l'utilisateur a cliqué "Pause")
    Stopped,   // Arrêté (l'utilisateur a cliqué "Stop")
    Inactive,  // Terminé (tous les fichiers copiés)
    Waiting    // En attente (non utilisé pour l'instant)
}

// BackupType.cs
public enum BackupType
{
    Full,           // Copier TOUS les fichiers
    Differential    // Copier seulement les fichiers MODIFIÉS
}

// LogFormat.cs (pour les logs)
public enum LogFormat
{
    Xml,   // Format XML
    Json   // Format JSON
}

// LogTarget.cs
public enum LogTarget
{
    Local,   // Logs sur le disque local
    Remote,  // Logs envoyés au serveur réseau
    Both     // Local + Remote
}
```

---

## 📊 JOUR 2 : Services (La Logique)

### **FileTaskScheduler.cs** ⭐⭐⭐ (15 min)

**À quoi ça sert ?** = Organiser les fichiers à copier dans des files (queues) selon la priorité

```csharp
public class FileTaskScheduler
{
    // 🔑 Les 4 files de priorité/taille
    private readonly Queue<FileTask> _priorityLargeFiles = new();      // Priorité + Gros
    private readonly Queue<FileTask> _prioritySmallFiles = new();      // Priorité + Petit
    private readonly Queue<FileTask> _nonPriorityLargeFiles = new();   // Normal + Gros
    private readonly Queue<FileTask> _nonPrioritySmallFiles = new();   // Normal + Petit
    //
    // Pourquoi 4 files ?
    // Les gros fichiers prennent longtemps.
    // On les traite dans un slot UNIQUE pour ne pas bloquer les petits.
    // Exemple :
    //   - FileA (1GB, priorité)  → _priorityLargeFiles (slot 1)
    //   - FileB (10MB, priorité) → _prioritySmallFiles (pas d'attente)
    //   - FileC (1GB, normal)    → En attente (slot 1 occupé)
    //   - FileD (10MB, normal)   → _nonPrioritySmallFiles (pas d'attente)

    private bool _isLargeFileSlotBusy = false;  // Le slot gros fichier est-il occupé ?
    private Dictionary<int, int> _filesCompletedByJob = new();  // Compteur par job
    private readonly object _lock = new();      // Verrou pour thread-safety

    // 🔑 Méthode : Ajouter un fichier
    public void ScheduleFile(string sourcePath, string destinationPath, BackupJob backupJob)
    {
        // 1. Est-ce une extension prioritaire ? (.pdf, .docx, etc.)
        bool isPriority = _configService.Current.PriorityExtensions.Any(ext => 
            sourcePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        
        // 2. Est-ce un "gros" fichier ? (> 100 Ko par exemple)
        bool isLargeFile = new FileInfo(sourcePath).Length > 
            (_configService.Current.LargeFileThreshold * 1024);

        lock (_lock)  // Protection : un seul thread à la fois
        {
            // 3. Ajouter à la bonne file
            if (isPriority)
            {
                if (isLargeFile)
                    _priorityLargeFiles.Enqueue(new(sourcePath, destinationPath, backupJob));
                else
                    _prioritySmallFiles.Enqueue(new(sourcePath, destinationPath, backupJob));
            }
            else
            {
                if (isLargeFile)
                    _nonPriorityLargeFiles.Enqueue(new(sourcePath, destinationPath, backupJob));
                else
                    _nonPrioritySmallFiles.Enqueue(new(sourcePath, destinationPath, backupJob));
            }
        }
    }

    // 🔑 Méthode : Récupérer la prochaine tâche
    public FileTask? GetNextTask(out bool assignedToLargeSlot)
    {
        assignedToLargeSlot = false;
        lock (_lock)
        {
            // Ordre de priorité :
            
            // 1️⃣ Gros prioritaires (si slot libre)
            if (_priorityLargeFiles.Count > 0 && !_isLargeFileSlotBusy)
            {
                assignedToLargeSlot = true;
                _isLargeFileSlotBusy = true;  // Occuper le slot
                return _priorityLargeFiles.Dequeue();
            }

            // 2️⃣ Petits prioritaires (toujours possible)
            if (_prioritySmallFiles.Count > 0)
                return _prioritySmallFiles.Dequeue();

            // 3️⃣ Gros non-prioritaires (si slot libre)
            if (_nonPriorityLargeFiles.Count > 0 && !_isLargeFileSlotBusy)
            {
                assignedToLargeSlot = true;
                _isLargeFileSlotBusy = true;
                return _nonPriorityLargeFiles.Dequeue();
            }

            // 4️⃣ Petits non-prioritaires (toujours possible)
            if (_nonPrioritySmallFiles.Count > 0)
                return _nonPrioritySmallFiles.Dequeue();

            // Aucune tâche disponible
            return null;
        }
    }

    // 🔑 Méthode : Incrémenter le compteur de fichiers complétés
    public int IncrementCompletedFiles(int jobId)
    {
        lock (_lock)
        {
            if (_filesCompletedByJob.ContainsKey(jobId))
            {
                _filesCompletedByJob[jobId]++;
                return _filesCompletedByJob[jobId];
            }
            return 0;
        }
    }
    // Pourquoi un compteur séparé ?
    // Pour calculer la progression : FilesCompleted / TotalFilesCount * 100
    // Ce compteur est MONOTONE (toujours croissant).
    // Avant, on calculait : (Total - RemainingFiles) 
    // Ça fluctuait avec les workers parallèles ❌
    // Maintenant c'est lisse ✅
}
```

**Questions probables** :
1. "Pourquoi 4 files ?" → Réponse : Pour éviter qu'un gros fichier bloque les petits
2. "C'est quoi `lock (_lock)` ?" → Réponse : Sécurité thread-safe (un seul thread accède aux files à la fois)
3. "Pourquoi `_filesCompletedByJob` ?" → Réponse : Pour avoir une progression monotone (toujours croissante)

---

### **BackupService.cs** ⭐⭐⭐ (20 min)

**À quoi ça sert ?** = Copier les fichiers en parallèle avec N workers

```csharp
public class BackupService
{
    private readonly ConfigService _configService;
    private readonly CryptoService _cryptoService;
    private readonly BusinessSoftwareMonitor _monitor;
    private readonly FileTaskScheduler _scheduler;  // Les files

    // Événements
    public event Action<TransferResult>? OnFileCompleted;  // Quand un fichier est copié
    public event Action<BackupState>? OnProgress;         // Quand l'état change

    public BackupService(ConfigService configService, CryptoService cryptoService, 
        BusinessSoftwareMonitor monitor)
    {
        _configService = configService;
        _cryptoService = cryptoService;
        _monitor = monitor;
        _scheduler = new FileTaskScheduler(configService);

        // 🔑 Lancer les workers
        int maxWorkers = configService.Current.MaxParallelFiles > 0 ? configService.Current.MaxParallelFiles : 4;
        // Pourquoi ? MaxParallelFiles peut être 0, donc on force minimum 4
        
        for (int i = 0; i < maxWorkers; i++)
        {
            Task.Run(() => WorkerLoop());  // Chaque worker = une task infinie
        }

        // Mise à jour du moniteur (détecte logiciel métier)
        Task.Run(async () =>
        {
            while (true)
            {
                _monitor.UpdateControlState(_allJobs);
                await Task.Delay(1000);  // Vérifier chaque seconde
            }
        });
    }

    // 🔑 Lancer une sauvegarde
    public void AddJobs(List<BackupJob> backupJobs)
    {
        _allJobs = backupJobs;
        JobControlService.Reset();  // Réinitialiser les flags Stop/Pause

        // Scanner les fichiers en arrière-plan
        Task.Run(() => ScanAndScheduleFiles(backupJobs));
    }

    // 🔑 Boucle principale d'un worker
    private void WorkerLoop()
    {
        while (!JobControlService.IsStoppedAll)  // Boucle infinie jusqu'à StopAll()
        {
            // 1️⃣ Récupérer la prochaine tâche
            FileTaskScheduler.FileTask? currentTask = _scheduler.GetNextTask(out bool isLargeFile);

            if (currentTask == null)
            {
                // Aucune tâche = attendre avant de réessayer
                _scheduler.WaitForTask(1000);  // Attendre 1 seconde
                continue;
            }

            // 2️⃣ Vérifier si le job est en pause
            JobControlService.WaitIfPaused(currentTask.BackupJob);

            if (JobControlService.IsStoppedAll)
                break;

            try
            {
                // 3️⃣ Copier le fichier
                CopyFile(currentTask);
            }
            finally
            {
                if (isLargeFile)
                {
                    // Libérer le slot pour que d'autres gros fichiers passent
                    _scheduler.ReleaseLargeFileSlot();
                    _scheduler.WakeupWaitingWorkers();
                }
            }
        }
    }

    // 🔑 Copier un fichier
    private void CopyFile(FileTaskScheduler.FileTask fileTask)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        long encryptionTimeMs = 0;
        bool success = true;

        try
        {
            // 1️⃣ Créer le répertoire de destination
            string destinationDirectory = Path.GetDirectoryName(fileTask.DestinationPath)!;
            Directory.CreateDirectory(destinationDirectory);

            // 2️⃣ Copier le fichier
            File.Copy(fileTask.SourcePath, fileTask.DestinationPath, overwrite: true);

            // 3️⃣ Chiffrer si besoin
            if (_configService.Current.EncryptionExtensions.Any(ext => 
                fileTask.DestinationPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            {
                encryptionTimeMs = _cryptoService.Encrypt(fileTask.DestinationPath);
            }
        }
        catch
        {
            success = false;
        }

        stopwatch.Stop();

        // 4️⃣ Préparer le rapport
        int completedCount = _scheduler.IncrementCompletedFiles(fileTask.BackupJob.Id);
        
        var transferResult = new TransferResult
        {
            JobName = fileTask.BackupJob.Name,
            Source = fileTask.SourcePath,
            Dest = fileTask.DestinationPath,
            Size = new FileInfo(fileTask.SourcePath).Exists ? new FileInfo(fileTask.SourcePath).Length : 0,
            TransferTimeMs = stopwatch.ElapsedMilliseconds,
            EncryptionTimeMs = encryptionTimeMs,
            Success = success
        };

        // 5️⃣ Émettre l'événement
        OnFileCompleted?.Invoke(transferResult);

        // 6️⃣ Mettre à jour l'état
        var remainingFiles = _scheduler.GetRemainingFilesForJob(fileTask.BackupJob.Id);
        var backupState = new BackupState
        {
            JobId = fileTask.BackupJob.Id,
            Status = DetermineJobStatus(fileTask.BackupJob, remainingFiles.Count),
            TotalFilesCount = fileTask.BackupJob.TotalFilesForThisJob,
            FilesToCopy = remainingFiles,
            FilesCompleted = completedCount
        };

        OnProgress?.Invoke(backupState);  // Notifier l'UI
    }
}
```

**Questions probables** :
1. "Combien de workers ?" → Réponse : Défini par Settings.MaxParallelFiles (1-16)
2. "Comment plusieurs workers ne se marche pas dessus ?" → Réponse : FileTaskScheduler utilise `lock`
3. "Pourquoi OnFileCompleted + OnProgress ?" → Réponse : Pour que MainViewModel puisse logger ET afficher la progression

---

### **LoggerService.cs** (10 min)

**À quoi ça sert ?** = Sauvegarder les logs en XML/JSON

```csharp
public class LoggerService
{
    private static readonly object _fileLock = new object();  // Verrou pour thread-safety

    public void Write(LogEntry entry, bool isJson)
    {
        string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, $"{DateTime.Now:yyyy-MM-dd}.{(isJson ? "json" : "xml")}");

        entry.Timestamp = DateTime.Now.ToString("G");

        lock (_fileLock)  // Un seul thread à la fois
        {
            if (isJson)
            {
                try
                {
                    // 1️⃣ Charger les logs existants
                    var logs = File.Exists(path) ? 
                        JsonSerializer.Deserialize<List<LogEntry>>(File.ReadAllText(path)) : 
                        new List<LogEntry>();
                    
                    // 2️⃣ Ajouter le nouveau log
                    logs!.Add(entry);
                    
                    // 3️⃣ Sauvegarder atomiquement (temp → final)
                    string tempPath = path + ".tmp";
                    File.WriteAllText(tempPath, JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true }));
                    File.Delete(path);
                    File.Move(tempPath, path);
                    // Pourquoi temp ? Si crash pendant la sauvegarde, le fichier original reste intact
                }
                catch
                {
                    // Si le fichier est corrompu, on le régénère
                    var logs = new List<LogEntry> { entry };
                    File.WriteAllText(path, JsonSerializer.Serialize(logs, ...));
                }
            }
            // ... même chose pour XML
        }
    }
}
```

---

### **ClientServer.cs** (5 min)

**À quoi ça sert ?** = Envoyer les logs au serveur ConsoleDeportee via TCP

```csharp
public class PersistentTcpLogger : IDisposable
{
    private TcpClient? _client;
    private StreamWriter? _writer;

    // Connexion au serveur
    public async Task ConnectAsync(string ip = "127.0.0.1")
    {
        try
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, 11000);  // Port 11000
            _writer = new StreamWriter(_client.GetStream(), Encoding.UTF8) { AutoFlush = true };
        }
        catch 
        { 
            // Si serveur absent, on continue (mode dégradé)
        }
    }

    // Envoyer un log
    public void SendLog(LogEntry entry)
    {
        if (_writer != null && _client is { Connected: true })
        {
            try
            {
                string json = JsonSerializer.Serialize(entry);
                _writer.WriteLine(json);  // Une ligne = un log
            }
            catch { }  // Erreur réseau, on ignore
        }
    }
}
```

---

## 📊 JOUR 3 : Orchestration

### **MainViewModel.cs** ⭐⭐ (15 min)

**À quoi ça sert ?** = Diriger toute l'application

```csharp
public class MainViewModel
{
    // Services
    private readonly ConfigService _config;
    private readonly BackupService _backup;        // Le moteur
    private readonly LoggerService _logger;        // Les logs locaux
    private readonly BackupReportingService _reporter;  // Dispatcher les logs
    
    public Action<BackupState>? OnProgressUpdate { get; set; }  // Pour l'UI

    public MainViewModel()
    {
        // 1️⃣ Charger la configuration
        _config.Load();

        // 2️⃣ Créer le service de chiffrement
        var cryptoService = new CryptoService(cryptoPath, "MY_KEY");

        // 3️⃣ Créer le moteur de backup
        _backup = new BackupService(_config, cryptoService, _monitor);

        // 4️⃣ Connecter le reporter
        //    BackupService émet OnFileCompleted
        //    → BackupReportingService le reçoit
        //    → Appelle LoggerService + PersistentTcpLogger
        _reporter = new BackupReportingService(_backup, _logger, _state, _config);

        // 5️⃣ Connecter à l'UI
        _backup.OnProgress += (state) => OnProgressUpdate?.Invoke(state);
    }

    // 🔑 Lancer une sauvegarde
    public async Task ExecuteSelection(string input)
    {
        // Exemple : input = "1-3" → jobs 1, 2, 3
        var selectedIndices = JobParser.ParseSelection(input, Jobs.Count);
        var jobsToRun = selectedIndices.Select(i => Jobs[i]).ToList();

        if (jobsToRun.Any())
        {
            JobControlService.Reset();  // Réinitialiser
            _backup.AddJobs(jobsToRun);  // LANCER !
        }
    }

    // Contrôles
    public void PauseAll() => JobControlService.PauseAll();
    public void ResumeAll() => JobControlService.ResumeAll(Jobs);
    public void StopAll() => JobControlService.StopAll();

    // Gestion des jobs (CRUD)
    public void AddJob(string name, string src, string dest, BackupType type) { ... }
    public void EditJob(...) { ... }
    public void DeleteJob(...) { ... }
}
```

**Le flux complet** :
```
1. User clique "Run"
   ↓
2. MainViewModel.ExecuteSelection()
   ↓
3. BackupService.AddJobs(jobs)
   ├─ Lance ScanAndScheduleFiles() en arrière-plan
   └─ Les workers commencent à copier
   ↓
4. Chaque fichier copié :
   ├─ BackupService émet OnFileCompleted
   ├─ MainViewModel reçoit
   ├─ BackupReportingService appelle LoggerService + PersistentTcpLogger
   ├─ BackupService émet OnProgress
   ├─ MainViewModel reçoit
   └─ L'UI se met à jour
```

---

## 🎓 LES 10 QUESTIONS LES PLUS PROBABLES DU PROF

### **Question 1** : "Expliquez le système de files (queues)"
**Réponse**:
```
FileTaskScheduler organise 4 files :
1. Fichiers prioritaires + gros     → Slot unique (ne pas bloquer les autres)
2. Fichiers prioritaires + petit    → Pas d'attente
3. Fichiers normal + gros           → Slot unique
4. Fichiers normal + petit          → Pas d'attente

Exemple : 
- 1GB PDF prioritaire    → Prend le slot, autres attendent
- 10MB DOC prioritaire   → Passe immédiatement (pas besoin du slot)
- 1GB TXT normal         → Attend le slot libéré par le PDF
```

---

### **Question 2** : "Pourquoi `lock` ? À quoi ça sert ?"
**Réponse**:
```
lock (_lock) { ... }

Empêche 2+ threads de modifier la même variable en même temps.

Sans lock :
  Worker 1: count = 5
  Worker 2: count = 5  ← Lit la valeur OLD
  Worker 1: count++ = 6
  Worker 2: count++ = 6  ← ERREUR ! Devrait être 7

Avec lock :
  Worker 1 acquiert le verrou
  Worker 2 attend
  Worker 1: count = 6
  Worker 1 libère le verrou
  Worker 2 acquiert
  Worker 2: count = 7
```

---

### **Question 3** : "Comment gère-t-on la sauvegarde différentielle ?"
**Réponse**:
```
En CheckSum MD5 :

1. À la 1ère sauvegarde : copier TOUS les fichiers
2. À la 2ème sauvegarde (si Differential) :
   - Calculer MD5 du fichier source
   - Comparer avec MD5 enregistré
   - Si différent : copier
   - Si identique : skip

Code :
if (job.Type == BackupType.Differential)
{
    string fileHash = Md5Service.GetHash(sourceFilePath);
    if (!Md5Service.HasChanged(jobName, sourceFilePath, fileHash))
        continue;  // ← Skip ce fichier
}
```

---

### **Question 4** : "Comment la progression est-elle calculée ?"
**Réponse**:
```
progression = FilesCompleted / TotalFilesCount * 100

Avant (BUG) :
progression = (TotalFiles - FilesRemaining) / TotalFiles
↓ Fluctuait à cause des workers parallèles

Après (CORRECT) :
FileTaskScheduler.IncrementCompletedFiles(jobId) ← Incrémente à chaque fichier copié
FilesCompleted toujours croissant : 0% → 10% → 20% → ... → 100%
```

---

### **Question 5** : "Expliquez le chiffrement"
**Réponse**:
```
Dans BackupService.CopyFile() :

if (configService.Current.EncryptionExtensions.Any(ext => 
    filePath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
{
    encryptionTimeMs = cryptoService.Encrypt(filePath);
}

Si le fichier a une extension à chiffrer (.txt, .pdf, etc.) :
  → Appel CryptoService.Encrypt()
  → Qui exécute CryptoSoft.exe (process externe)
  → Retourne le temps pris

Le fichier est chiffré SUR PLACE après la copie.
```

---

### **Question 6** : "Comment remet-on un job en pause et on le reprend ?"
**Réponse**:
```
Service statique : JobControlService

PauseAll() :
  ├─ JobControlService.IsPausedAll = true
  └─ Tous les workers :
     └─ JobControlService.WaitIfPaused(job)
        └─ Bloque jusqu'à IsPausedAll = false

ResumeAll() :
  ├─ JobControlService.IsPausedAll = false
  └─ Les workers se réveillent

StopAll() :
  ├─ JobControlService.IsStoppedAll = true
  └─ Tous les workers sortent de leur boucle while
```

---

### **Question 7** : "Comment les logs locaux et distants sont gérés ?"
**Réponse**:
```
BackupReportingService.HandleFileCompleted() :

1. Reçoit l'événement OnFileCompleted
2. Regarde Settings.LogTarget :
   
   Si Local ou Both :
     → LoggerService.Write(entry, isJson)
        → Sauvegarde XML/JSON localement
        
   Si Remote ou Both :
     → PersistentTcpLogger.SendLog(entry)
        → Envoie JSON au serveur:11000

Serveur ConsoleDeportee :
  ├─ Écoute sur 127.0.0.1:11000
  ├─ Reçoit les logs JSON
  └─ Les sauvegarde dans ReceivedLogs/
```

---

### **Question 8** : "Pourquoi écriture atomique (temp file) ?"
**Réponse**:
```
LoggerService.Write() :

1. Écrire dans fichier.json.tmp
2. Si crash ici ↑ : le fichier original.json est INTOUCHÉ
3. Renommer fichier.tmp → fichier.json

Pourquoi pas écrire directement ?
- Si crash pendant l'écriture : fichier corrompu
- Au prochain démarrage : erreur XmlException

Avec l'approche atomique :
- Si crash : fichier original reste valide ✅
```

---

### **Question 9** : "Comment gère-t-on la détection du logiciel métier ?"
**Réponse**:
```
BusinessSoftwareMonitor :

1. Chaque seconde :
   └─ Vérifie si le logiciel est en cours d'exécution
   
2. Si logiciel détecté :
   └─ Pause automatiquement la sauvegarde
   
3. Si logiciel fermé :
   └─ Reprend automatiquement

Code dans MainViewModel :
_monitor.OnSoftwareDetectionChanged += (detected) => 
{
    if (detected)
        // Affiche popup "Logiciel métier détecté"
    else
        // Reprend sauvegarde
};
```

---

### **Question 10** : "Que se passe-t-il en cas d'erreur lors de la copie ?"
**Réponse**:
```
Dans CopyFile() :

try
{
    File.Copy(...);
    CryptoService.Encrypt(...);
}
catch
{
    success = false;  // Marquer l'erreur
}

Puis :
├─ Créer le rapport avec success=false
├─ Émettre OnFileCompleted (l'erreur est reportée)
└─ Continuer avec le fichier suivant (pas de crash)

Les logs montreront quels fichiers ont échoué.
```

---

## 🚀 SCRIPT DE PRÉSENTATION (5 min)

```
"Bonjour,

Notre application EasySave est une solution de sauvegarde parallèle
avec 3 composants majeurs :

1️⃣ MOTEUR DE COPIE (BackupService + FileTaskScheduler)
   - N workers parallèles
   - Organisés en 4 files de priorité
   - Gestion du slot unique pour gros fichiers

2️⃣ GESTION DES LOGS (LoggerService + ClientServer)
   - Local : XML/JSON sur disque (écriture atomique)
   - Remote : JSON envoyé au serveur ConsoleDeportee:11000

3️⃣ ORCHESTRATION (MainViewModel + JobControlService)
   - Commandes Pause/Resume/Stop
   - Reprise après arrêt
   - Progression monotone

Les avantages de l'architecture :
- Chaque classe a une responsabilité unique
- Thread-safe (utilisation de lock/mutex)
- Extensible (facile d'ajouter nouvelles fonctionnalités)
- Robuste (gestion des erreurs, fichiers corrompus)

Avez-vous des questions ?"
```

---

## ✅ Checklist Avant la Soutenance

- [ ] Lire tous les fichiers dans l'ordre indiqué
- [ ] Répondre aux 10 questions sans regarder
- [ ] Tracer un flux complet (de User click à log écrit)
- [ ] Comprendre pourquoi chaque `lock` est là
- [ ] Pouvoir expliquer l'architecture en 5 min
- [ ] Pratiquer la présentation 2x devant quelqu'un



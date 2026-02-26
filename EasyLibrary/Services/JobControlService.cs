using EasySave.Models;

namespace EasySave.Services;

public static class JobControlService
{
    private static readonly object _syncLock = new();

    // Nos interrupteurs (simples booléens)
    public static bool IsPaused { get; set; } = false;
    public static bool IsStopped { get; set; } = false;

    public static bool IsPausedAll { get; private set; } = false;
    public static bool IsStoppedAll { get; private set; } = false;


    public static void PauseAll()
    {
        lock (_syncLock) { IsPausedAll = true; }
    }

    public static void ResumeAll(System.Collections.Generic.IEnumerable<BackupJob> jobs)
    {
        lock (_syncLock)
        {
            IsPausedAll = false;
            // Quand on fait Resume All, on enlève aussi la pause individuelle de chaque job
            foreach (var j in jobs) j.IsPaused = false;

            Monitor.PulseAll(_syncLock);
        }
    }

    public static void StopAll()
    {
        lock (_syncLock)
        {
            IsStoppedAll = true;
            IsPausedAll = false;
            Monitor.PulseAll(_syncLock);
        }
    }


    // Méthodes pour changer l'état
    public static void Pause(BackupJob job)
    {
        lock (_syncLock) { job.IsPaused = true; }
    }

    public static void Resume(BackupJob job)
    {
        lock (_syncLock)
        {
            job.IsPaused = false;
            // On réveille tous les threads endormis dans la "douane"
            Monitor.PulseAll(_syncLock);
        }
    }
    public static void Stop(BackupJob job)
    {
        lock (_syncLock)
        {
            job.IsStopped = true;
            job.IsPaused = false; // On débloque la pause pour permettre l'arrêt
            Monitor.PulseAll(_syncLock);
        }
    }

    // Reset avant chaque nouvelle sauvegarde
    public static void Reset()
    {
        lock (_syncLock)
        {
            IsPaused = false;
            IsStopped = false;
        }
    }

    // La "douane" : on reste bloqué ici tant que c'est sur pause
    public static void WaitIfPaused(BackupJob job)
    {
        lock (_syncLock)
        {
            // Tant qu'on est en pause et qu'on n'a pas stoppé
            while ((IsPausedAll || job.IsPaused) && !IsStoppedAll && !job.IsStopped)
            {
                // Le thread s'endort et ne consomme AUCUN CPU
                Monitor.Wait(_syncLock);
            }
        }
    }
}
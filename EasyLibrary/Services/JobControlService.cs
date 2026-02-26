namespace EasySave.Services;

public static class JobControlService
{
    // Nos interrupteurs (simples booléens)
    public static bool IsPaused { get; set; } = false;
    public static bool IsStopped { get; set; } = false;

    // Méthodes pour changer l'état
    public static void Pause() => IsPaused = true;
    public static void Resume() => IsPaused = false;
    public static void Stop() => IsStopped = true;

    // Reset avant chaque nouvelle sauvegarde
    public static void Reset()
    {
        IsPaused = false;
        IsStopped = false;
    }

    // La "douane" : on reste bloqué ici tant que c'est sur pause
    public static void WaitIfPaused()
    {
        while (IsPaused && !IsStopped)
        {
            System.Threading.Thread.Sleep(200); // On attend 0.2s et on re-vérifie
        }
    }
}
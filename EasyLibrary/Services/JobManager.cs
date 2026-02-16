using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EasySave.Models;

namespace EasySave.Services;

public class JobManager
{
    private readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "jobs.json");

    public List<BackupJob> Load()
    {
        if (!File.Exists(_path)) return new List<BackupJob>();
        return JsonSerializer.Deserialize<List<BackupJob>>(File.ReadAllText(_path)) ?? new List<BackupJob>();
    }

    public void Save(List<BackupJob> jobs) => File.WriteAllText(_path, JsonSerializer.Serialize(jobs, new JsonSerializerOptions { WriteIndented = true }));
}

//Ne fait que lire et écrire la liste des jobs dans jobs.json. Il n'a aucune logique de copie.
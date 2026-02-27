using EasySave.Models; 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EasySave.Services;

public class StateService
{
    private readonly string _stateFilePath;
    private readonly object _lock = new object();
    private List<BackupState> _currentStates = new List<BackupState>();

    public StateService()
    {
        // Le fichier state.json sera créé à côté de l'exécutable (.exe / .dll)
        _stateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "state.json");
    }

    // Méthode Thread-Safe pour mettre à jour le JSON
    public void UpdateState(BackupState newState)
    {
        lock (_lock) // Verrou obligatoire car 4 threads trvaillent parallèle !
        {
            // 1. On cherche si le travail existe déjà dans notre liste en mémoire
            var existingJob = _currentStates.FirstOrDefault(j => j.JobId == newState.JobId);

            if (existingJob != null)
            {
                // On met à jour ses valeurs
                existingJob.Status = newState.Status;
                existingJob.TotalFilesCount = newState.TotalFilesCount;
                existingJob.FilesToCopy = newState.FilesToCopy; // Ta liste de fichiers restants
                existingJob.LastUpdate = DateTime.Now;
            }
            else
            {
                // C'est un nouveau travail, on l'ajoute
                _currentStates.Add(newState);
            }

            // 2. On convertit la liste en JSON formaté (WriteIndented = true pour que ce soit lisible)
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() } // Pour écrire "Active" au lieu de "0" pour ton Enum
            };

            string jsonString = JsonSerializer.Serialize(_currentStates, options);

            // 3. On écrit dans le fichier
            File.WriteAllText(_stateFilePath, jsonString);
        }
    }

    public List<BackupState> ReadStates()
    {
        lock (_lock)
        {
            if (!File.Exists(_stateFilePath))
                return new List<BackupState>();

            try
            {
                string jsonString = File.ReadAllText(_stateFilePath);
                return JsonSerializer.Deserialize<List<BackupState>>(jsonString) ?? new List<BackupState>();
            }
            catch
            {
                // Si le fichier est corrompu, on repart à zéro
                return new List<BackupState>();
            }
        }
    }
}


using System;
using System.IO;
using System.Text.Json;
using EasySave.Model;

namespace EasySave.ViewModel
{
    public class StateService
    {
        private string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasySave", "state.json");

        public void UpdateState(BackUpJob job)
        {
            // On s'assure que le dossier existe
            Directory.CreateDirectory(Path.GetDirectoryName(_path));

            // On sérialise l'objet job directement pour le fichier d'état
            string json = JsonSerializer.Serialize(job, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
        }
    }
}
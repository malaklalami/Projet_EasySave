using EasyLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;


namespace EasyLibrary.Services
{
    public class JobManager
    {
        public List<BackUpJob> loadJobs(string configPath)
        {
            if (!File.Exists(configPath)) return new List<BackUpJob>();
            string json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<List<BackUpJob>>(json) ?? new List<BackUpJob>();
        }

        public void saveJobs(List<BackUpJob> Jobs)
        {
            try
            {

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Jobs, options);

                // On écrit le fichier (il sera créé s'il n'existe pas)
                File.WriteAllText("jobs.json", json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur lors de la sauvegarde : " + ex.Message);
            }
        }

        public void clearJobs()
        {
            if (File.Exists("jobs.json"))
            {
                File.Delete("jobs.json");
            }
        }
    }
}
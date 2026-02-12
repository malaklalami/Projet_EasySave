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

        public void saveJobs(List<BackUpJob> jobs)
        {
            //TODO implémenter la sauvegarde dans un fichier json
        }
    }
}

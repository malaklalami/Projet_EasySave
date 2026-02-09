using EasyLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyLibrary.Services
{
    public class JobManager
    {
        public List<BackUpJob> loadJobs()
        {
            return new List<BackUpJob>();
        }

        public void saveJobs(List<BackUpJob> jobs)
        {
            //TODO implémenter la sauvegarde dans un fichier json
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using EasyLibrary.Models;
using EasyLibrary.Services;
using EasyLog;

namespace EasyLibrary.Services
{
    public class JobService
    {
        private readonly StateService _stateService;
        private readonly LoggerService _loggerService;

        public JobService(StateService stateService, LoggerService loggerService)
        {
            _stateService = stateService;
            _loggerService = loggerService;
        }

        public void ExecuteJob(BackUpJob job, IVue vue)
        {
            Stopwatch stopwatch = new Stopwatch();

            try
            {
                // 1. Vérification de sécurité
                if (!Directory.Exists(job.SourceDir))
                {
                    vue.JobExecutionError(job);
                    return;
                }

                // 2. Préparation
                job.State = "Active";
                job.Progress = 0;
                _stateService.UpdateState(job);

                string[] files = Directory.GetFiles(job.SourceDir, "*.*", SearchOption.AllDirectories);
                job.TotalFiles = files.Length;

                long totalSize = 0;
                foreach (string f in files) { totalSize += new FileInfo(f).Length; }
                job.TotalSize = totalSize;

                // 3. Boucle de copie
                for (int i = 0; i < files.Length; i++)
                {
                    string relativePath = Path.GetRelativePath(job.SourceDir, files[i]);
                    string destFile = Path.Combine(job.TargetDir, relativePath);

                    string destFolder = Path.GetDirectoryName(destFile);
                    if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);

                    FileInfo fileInfo = new FileInfo(files[i]);
                    stopwatch.Restart();

                    File.Copy(files[i], destFile, true);

                    stopwatch.Stop();

                    // Log via la DLL
                    _loggerService.WriteLog(new LogEntry
                    {
                        JobName = job.Name,
                        SourcePath = files[i],
                        TargetPath = destFile,
                        FileSize = fileInfo.Length,
                        TransferTimeMs = stopwatch.Elapsed.TotalMilliseconds
                    });

                    job.Progress = (int)((i + 1) * 100 / files.Length);
                    _stateService.UpdateState(job);
                }

                vue.JobSucceeded(job);
            }
            catch (Exception ex)
            {
                vue.JobExecutionError(job, ex);
            }
            finally
            {
                job.State = "Inactive";
                job.Progress = 100;
                _stateService.UpdateState(job);
            }
        }
    }
}
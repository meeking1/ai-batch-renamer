using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using AiBatchRenamer.Core.Models;

namespace AiBatchRenamer.Infrastructure.Services
{
    public class OperationLogRepository
    {
        private readonly string logDirectory;

        public OperationLogRepository()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiBatchRenamer",
                "Logs"))
        {
        }

        public OperationLogRepository(string logDirectory)
        {
            this.logDirectory = logDirectory;
        }

        public string Save(OperationLog log)
        {
            Directory.CreateDirectory(logDirectory);
            var filePath = Path.Combine(logDirectory, log.OperationId + ".json");

            var serializer = new DataContractJsonSerializer(typeof(OperationLog));
            using (var stream = File.Create(filePath))
            {
                serializer.WriteObject(stream, log);
            }

            File.WriteAllText(GetLatestPointerPath(), filePath, Encoding.UTF8);
            return filePath;
        }

        public OperationLog LoadLatest()
        {
            var latestPointerPath = GetLatestPointerPath();
            if (!File.Exists(latestPointerPath))
            {
                return null;
            }

            var filePath = File.ReadAllText(latestPointerPath, Encoding.UTF8).Trim();
            if (!File.Exists(filePath))
            {
                return null;
            }

            var serializer = new DataContractJsonSerializer(typeof(OperationLog));
            using (var stream = File.OpenRead(filePath))
            {
                return serializer.ReadObject(stream) as OperationLog;
            }
        }

        public void ClearLatestPointer()
        {
            var latestPointerPath = GetLatestPointerPath();
            if (File.Exists(latestPointerPath))
            {
                File.Delete(latestPointerPath);
            }
        }

        private string GetLatestPointerPath()
        {
            Directory.CreateDirectory(logDirectory);
            return Path.Combine(logDirectory, "latest.txt");
        }
    }
}

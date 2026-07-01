using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;

namespace AiBatchRenamer.Infrastructure.Services
{
    public class AppSettingsService
    {
        private const string DefaultModel = "deepseek-v4-flash";
        private readonly string settingsPath;

        public AppSettingsService()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiBatchRenamer",
                "settings.json"))
        {
        }

        public AppSettingsService(string settingsPath)
        {
            this.settingsPath = settingsPath;
        }

        public AppSettings Load()
        {
            var settings = LoadFromFile();
            if (string.IsNullOrWhiteSpace(settings.DeepSeekModel))
            {
                settings.DeepSeekModel = DefaultModel;
            }

            var environmentKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
            if (!string.IsNullOrWhiteSpace(environmentKey))
            {
                settings.DeepSeekApiKey = environmentKey;
                settings.IsApiKeyFromEnvironment = true;
                return settings;
            }

            settings.DeepSeekApiKey = Unprotect(settings.EncryptedDeepSeekApiKey);
            settings.IsApiKeyFromEnvironment = false;
            return settings;
        }

        public void Save(AppSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));
            var persisted = new PersistedSettings
            {
                DeepSeekModel = string.IsNullOrWhiteSpace(settings.DeepSeekModel)
                    ? DefaultModel
                    : settings.DeepSeekModel.Trim(),
                EncryptedDeepSeekApiKey = Protect(settings.DeepSeekApiKey)
            };

            var serializer = new DataContractJsonSerializer(typeof(PersistedSettings));
            using (var stream = File.Create(settingsPath))
            {
                serializer.WriteObject(stream, persisted);
            }
        }

        private AppSettings LoadFromFile()
        {
            if (!File.Exists(settingsPath))
            {
                return new AppSettings { DeepSeekModel = DefaultModel };
            }

            try
            {
                var serializer = new DataContractJsonSerializer(typeof(PersistedSettings));
                using (var stream = File.OpenRead(settingsPath))
                {
                    var persisted = serializer.ReadObject(stream) as PersistedSettings;
                    return new AppSettings
                    {
                        DeepSeekModel = persisted == null ? DefaultModel : persisted.DeepSeekModel,
                        EncryptedDeepSeekApiKey = persisted == null ? string.Empty : persisted.EncryptedDeepSeekApiKey
                    };
                }
            }
            catch
            {
                return new AppSettings { DeepSeekModel = DefaultModel };
            }
        }

        private static string Protect(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var bytes = Encoding.UTF8.GetBytes(value.Trim());
            var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encrypted);
        }

        private static string Unprotect(string encryptedValue)
        {
            if (string.IsNullOrWhiteSpace(encryptedValue))
            {
                return string.Empty;
            }

            try
            {
                var encrypted = Convert.FromBase64String(encryptedValue);
                var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    public class AppSettings
    {
        public string DeepSeekModel { get; set; }

        public string DeepSeekApiKey { get; set; }

        public string EncryptedDeepSeekApiKey { get; set; }

        public bool IsApiKeyFromEnvironment { get; set; }
    }

    [DataContract]
    internal class PersistedSettings
    {
        [DataMember(Order = 1)]
        public string DeepSeekModel { get; set; }

        [DataMember(Order = 2)]
        public string EncryptedDeepSeekApiKey { get; set; }
    }
}

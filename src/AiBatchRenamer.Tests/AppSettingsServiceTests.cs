using System;
using System.IO;
using AiBatchRenamer.Infrastructure.Services;

namespace AiBatchRenamer.Tests
{
    internal static class AppSettingsServiceTests
    {
        public static void SaveLoadAndClear_PreservesModelAndRemovesKey()
        {
            var root = Path.Combine(Path.GetTempPath(), "AiBatchRenamerSettingsTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var previousApiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
            Environment.SetEnvironmentVariable("DEEPSEEK_API_KEY", null);

            try
            {
                var settingsPath = Path.Combine(root, "settings.json");
                var service = new AppSettingsService(settingsPath);

                service.Save(new AppSettings
                {
                    DeepSeekModel = "deepseek-v4-flash",
                    DeepSeekApiKey = "test-key"
                });

                var loaded = service.Load();
                TestAssert.Equal("deepseek-v4-flash", loaded.DeepSeekModel, "loaded model");
                TestAssert.Equal("test-key", loaded.DeepSeekApiKey, "loaded API key");

                service.ClearDeepSeekApiKey();
                var cleared = service.Load();

                TestAssert.Equal("deepseek-v4-flash", cleared.DeepSeekModel, "cleared model");
                TestAssert.Equal(string.Empty, cleared.DeepSeekApiKey, "cleared API key");
            }
            finally
            {
                Environment.SetEnvironmentVariable("DEEPSEEK_API_KEY", previousApiKey);

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }
    }
}

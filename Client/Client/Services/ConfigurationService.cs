using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Client.Services
{
    public class ConfigurationService : IConfigurationService
    {
        private static readonly string ConfigFilePath =
            Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        private string UrlServerPath;
        private IConfiguration _configuration;
        public event Action<string>? UrlChanged;

        public ConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
            UrlServerPath = _configuration["ApiBaseUrl"] ?? "http://localhost:5015";
        }

        public IConfiguration GetConfig() => _configuration;

        public string GetApiUrl() => _configuration["ApiBaseUrl"] ?? "http://localhost:5015";

        public string GetTheme() => _configuration["Theme"] ?? "Dark";

        public void SaveApiUrl(string url)
        {
            WriteConfig(cfg => cfg["ApiBaseUrl"] = url);
            _configuration = BuildConfig();
            UrlServerPath = url;
            UrlChanged?.Invoke(url);
        }

        public void SaveSettings(float confidence, float iou, string theme)
        {
            WriteConfig(cfg =>
            {
                cfg["Theme"] = theme;
                cfg["NeuralNetwork"] = new Dictionary<string, object>
                {
                    ["Confidence"] = confidence.ToString(CultureInfo.InvariantCulture),
                    ["Iou"] = iou.ToString(CultureInfo.InvariantCulture)
                };
            });
            _configuration = BuildConfig();
        }

        private static void WriteConfig(Action<Dictionary<string, object>> modify)
        {
            Dictionary<string, object> cfg;
            if (File.Exists(ConfigFilePath))
            {
                var json = File.ReadAllText(ConfigFilePath);
                cfg = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
            }
            else
            {
                cfg = new();
            }

            modify(cfg);

            var newJson = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, newJson);
        }

        private static IConfiguration BuildConfig() =>
            new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .Build();
    }
}

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
        private const string ConfigFileName = "appsettings.json";
        private string UrlServerPath;
        private IConfiguration _configuration;
        public event Action<string>? UrlChanged;
        public ConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration;
            UrlServerPath = _configuration["ApiBaseUrl"] ?? "http://localhost:5015";
        }
        public IConfiguration GetConfig()
        {
            return _configuration;
        }
        public string GetApiUrl()
        {
            return _configuration["ApiBaseUrl"] ?? "http://localhost:5015";
        }

        public string GetTheme()
        {
            return _configuration["Theme"] ?? "Dark";
        }

        public void SaveApiUrl(string url)
        {
            if (File.Exists(ConfigFileName))
            {
                var json = File.ReadAllText(ConfigFileName);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();

                config["ApiBaseUrl"] = url;

                var newJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFileName, newJson);
            }
            _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            UrlServerPath = url;
            UrlChanged?.Invoke(url);
        }
        public void SaveSettings(float confidence, float iou, string theme)
        {
            if (File.Exists(ConfigFileName))
            {
                var json = File.ReadAllText(ConfigFileName);
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();

                if (config.ContainsKey("NeuralNetwork"))
                {
                    var neuralNetwork = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        JsonSerializer.Serialize(config["NeuralNetwork"])) ?? new();

                    neuralNetwork["Confidence"] = confidence.ToString(CultureInfo.InvariantCulture);
                    neuralNetwork["Iou"] = iou.ToString(CultureInfo.InvariantCulture);

                    config["NeuralNetwork"] = neuralNetwork;
                }
                else
                {
                    config["NeuralNetwork"] = new Dictionary<string, object>
                    {
                        ["Confidence"] = confidence.ToString(CultureInfo.InvariantCulture),
                        ["Iou"] = iou.ToString(CultureInfo.InvariantCulture)
                    };
                }

                config["Theme"] = theme;

                var newJson = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFileName, newJson);
            }
            _configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        }
    }
}

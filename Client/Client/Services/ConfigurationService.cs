using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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

        public string GetApiUrl()
        {
            return _configuration["ApiBaseUrl"] ?? "http://localhost:5015";
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
    }
}
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Services
{
    public interface IConfigurationService
    {
        event Action<string>? UrlChanged;

        string GetApiUrl();
        IConfiguration GetConfig();
        void SaveApiUrl(string url);
        void SaveSettings(float confidence, float iou);
    }
}
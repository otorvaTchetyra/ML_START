using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Services
{
    public interface IConfigurationService
    {
        event Action<string>? UrlChanged;

        string GetApiUrl();
        void SaveApiUrl(string url);
    }
}
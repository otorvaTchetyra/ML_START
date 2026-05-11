using Client.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Client.Services
{
    public class HealthService : IHealthService
    {
        private readonly IApiClient _apiClient;
        private readonly IConfigurationService _configService;
        public HealthService(IApiClient apiClient, IConfigurationService configService)
        {
            _apiClient = apiClient;
            _configService = configService;

            _configService.UrlChanged += ResetUrl;
        }
        public void ResetUrl(string? url)
        {
            if (!string.IsNullOrWhiteSpace(url))
                _apiClient.SetUrl(url);
        }
        public async Task<HealthStatus> GetHealthStatusAsync()
        {
            try
            {
                var response = await _apiClient.GetAsync<object>("/health");
                return new HealthStatus
                {
                    IsAvailable = true,
                    Message = "Сервер доступен"
                };
            }
            catch (HttpRequestException ex)
            {
                return new HealthStatus
                {
                    IsAvailable = false,
                    Message = $"Сервер недоступен ({_configService.GetApiUrl()}): {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new HealthStatus
                {
                    IsAvailable = false,
                    Message = $"Не удалось подключиться к серверу ({_configService.GetApiUrl()}): {ex.Message}"
                };
            }
        }
        public async Task<HealthStatus> DropDBAsync()
        {
            try
            {
                var response = await _apiClient.PostAsync<object>("/health", "");
                return new HealthStatus
                {
                    IsAvailable = true,
                    Message = "БД удалена"
                };
            }
            catch (HttpRequestException ex)
            {
                return new HealthStatus
                {
                    IsAvailable = false,
                    Message = $"Сервер недоступен ({_configService.GetApiUrl()}): {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new HealthStatus
                {
                    IsAvailable = false,
                    Message = $"Не удалось подключиться к серверу ({_configService.GetApiUrl()}): {ex.Message}"
                };
            }
        }
    }
}

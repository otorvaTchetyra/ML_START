using Client.Models;
using System.Threading.Tasks;

namespace Client.Services
{
    public class StreamService
    {
        private readonly IApiClient _apiClient;

        public StreamService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<HealthStatus?> GetStatusAsync()
        {
            return await _apiClient.GetAsync<HealthStatus>("/stream/status");
        }

        public async Task StopAsync()
        {
            await _apiClient.PostAsync<object>("/stream/stop", new { });
        }
    }
}
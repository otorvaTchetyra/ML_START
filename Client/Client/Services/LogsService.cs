using Client.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Services
{
    public class LogsService
    {
        private readonly IApiClient _apiClient;

        public LogsService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<AppLog>?> GetLogsAsync()
        {
            return await _apiClient.GetAsync<List<AppLog>>("/logs");
        }
    }
}
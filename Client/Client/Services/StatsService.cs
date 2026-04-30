using Client.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Services
{
    public class StatsService
    {
        private readonly IApiClient _apiClient;

        public StatsService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<FeedingStats>?> GetStatsAsync(string from, string to)
        {
            return await _apiClient.GetAsync<List<FeedingStats>>($"/stats?from={from}&to={to}");
        }
    }
}

using Client.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Services
{
    public class EventsService
    {
        private readonly IApiClient _apiClient;

        public EventsService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<FeedingEvent>?> GetEventsAsync()
        {
            return await _apiClient.GetAsync<List<FeedingEvent>>("/events");
        }

        public async Task AddCommentAsync(int eventId, string comment)
        {
            await _apiClient.PostAsync<object>($"/events/{eventId}/comment",
                new { comment });
        }
    }
}
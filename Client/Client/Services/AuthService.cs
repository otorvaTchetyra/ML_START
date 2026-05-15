using Client.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Client.Services
{
    public record LoginResult(bool Success, bool IsRateLimited, int RetryAfterSeconds);

    public class AuthService
    {
        private readonly IApiClient _apiClient;
        private readonly HttpClient _httpClient;
        private string? _token;
        private ApiUser? _currentUser;

        public AuthService(IApiClient apiClient, HttpClient httpClient)
        {
            _apiClient = apiClient;
            _httpClient = httpClient;
        }

        public bool IsAuthenticated => _token != null;
        public ApiUser? CurrentUser => _currentUser;
        public bool IsAdmin => string.Equals(_currentUser?.Role, "admin", StringComparison.OrdinalIgnoreCase);

        public async Task<LoginResult> LoginAsync(string username, string password)
        {
            try
            {
                var response = await _httpClient.PostAsync(
                    "/auth/login",
                    JsonContent.Create(new LoginRequest { Username = username, Password = password }));

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    int retryAfter = 60;
                    if (response.Headers.TryGetValues("Retry-After", out var values))
                        int.TryParse(System.Linq.Enumerable.FirstOrDefault(values), out retryAfter);
                    return new LoginResult(false, true, retryAfter);
                }

                if (!response.IsSuccessStatusCode)
                    return new LoginResult(false, false, 0);

                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (!string.IsNullOrWhiteSpace(loginResponse?.AccessToken))
                {
                    _token = loginResponse!.AccessToken;
                    _apiClient.SetAuthToken(_token);
                    _currentUser = await _apiClient.GetAsync<ApiUser>("/auth/me");
                    return new LoginResult(true, false, 0);
                }
                return new LoginResult(false, false, 0);
            }
            catch
            {
                return new LoginResult(false, false, 0);
            }
        }

        public void Logout()
        {
            _token = null;
            _currentUser = null;
            _apiClient.SetAuthToken(null);
        }
    }
}

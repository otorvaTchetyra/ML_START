using Client.Models;
using System.Threading.Tasks;

namespace Client.Services
{
    public class AuthService
    {
        private readonly IApiClient _apiClient;
        private string? _token;

        public AuthService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public bool IsAuthenticated => _token != null;

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                var response = await _apiClient.PostAsync<LoginResponse>(
                    "/auth/login",
                    new LoginRequest { Username = username, Password = password }
                );

                if (!string.IsNullOrWhiteSpace(response?.AccessToken))
                {
                    _token = response!.AccessToken;
                    _apiClient.SetAuthToken(_token);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void Logout()
        {
            _token = null;
            _apiClient.SetAuthToken(null);
        }
    }
}

using Client.Models;
using System.Threading.Tasks;

namespace Client.Services
{
    public class AuthService
    {
        private readonly IApiClient _apiClient;
        private string? _token;
        private ApiUser? _currentUser;

        public AuthService(IApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public bool IsAuthenticated => _token != null;
        public ApiUser? CurrentUser => _currentUser;
        public bool IsAdmin => string.Equals(_currentUser?.Role, "admin", System.StringComparison.OrdinalIgnoreCase);

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
                    _currentUser = await _apiClient.GetAsync<ApiUser>("/auth/me");
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
            _currentUser = null;
            _apiClient.SetAuthToken(null);
        }
    }
}

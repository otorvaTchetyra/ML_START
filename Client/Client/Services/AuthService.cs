using Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Client.Services
{
    public class AuthService
    {
        private readonly IApiClient _apiClient;
        private readonly HttpClient _httpClient;
        private string? _token;

        public AuthService(IApiClient apiClient, HttpClient httpClient)
        {
            _apiClient = apiClient;
            _httpClient = httpClient;
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

                if (response?.AccessToken != null)
                {
                    _token = response.AccessToken;
                    // Подставляем токен во все следующие запросы
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _token);
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
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}

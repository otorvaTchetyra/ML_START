using Client.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Services;

public class UsersService
{
    private readonly IApiClient _apiClient;

    public UsersService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public Task<List<ApiUser>?> GetUsersAsync()
    {
        return _apiClient.GetAsync<List<ApiUser>>("/auth/users");
    }

    public Task DeleteUserAsync(int id)
    {
        return _apiClient.DeleteAsync($"/auth/users/{id}");
    }

    public Task<ApiUser?> CreateOperatorAsync(string username, string password)
    {
        return _apiClient.PostAsync<ApiUser>(
            "/auth/users",
            new { username, password, role = "operator" });
    }

    public Task<ApiUser?> UpdateUserAsync(int id, string? username, string? password, string? role, bool? isActive)
    {
        return _apiClient.PutAsync<ApiUser>(
            $"/auth/users/{id}",
            new { username, password, role, is_active = isActive });
    }
}

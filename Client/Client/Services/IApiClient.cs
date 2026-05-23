using System;
using System.Net.Http;
using System.Threading.Tasks;

public interface IApiClient
{
    void SetUrl(string http);
    void SetAuthToken(string? token);
    Task<T?> GetAsync<T>(string endpoint);
    Task<T?> PostAsync<T>(string endpoint, object data);
    Task<HttpResponseMessage> PostRawAsync(string endpoint, object data);
    Task<HttpResponseMessage> PostFileAsync(string endpoint, string filePath, string formFieldName = "file", IProgress<double>? progress = null);
    Task<HttpResponseMessage> GetRawAsync(string endpoint);
    Task<T?> PatchAsync<T>(string endpoint, object data);
    Task<T?> PutAsync<T>(string endpoint, object data);
    Task DeleteAsync(string endpoint);
}

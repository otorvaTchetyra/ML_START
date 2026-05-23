using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Client.Services;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private string _baseUrl;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _baseUrl = _httpClient.BaseAddress?.ToString() ?? "http://localhost:8000";
    }

    public void SetUrl(string http)
    {
        if (!string.IsNullOrWhiteSpace(http))
            _baseUrl = http;
    }

    public void SetAuthToken(string? token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = string.IsNullOrWhiteSpace(token)
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync(BuildUri(endpoint));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        var response = await _httpClient.PostAsync(BuildUri(endpoint), JsonContent.Create(data, data.GetType()));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<HttpResponseMessage> PostRawAsync(string endpoint, object data)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(endpoint))
        {
            Content = JsonContent.Create(data)
        };

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"POST {endpoint} failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }
        return response;
    }

    public async Task<HttpResponseMessage> PostFileAsync(string endpoint, string filePath, string formFieldName = "file", IProgress<double>? progress = null)
    {
        await using var fs = File.OpenRead(filePath);
        using var progressStream = progress != null ? new ProgressStream(fs, fs.Length, progress) : null;
        Stream uploadStream = progressStream ?? (Stream)fs;
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(uploadStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, formFieldName, Path.GetFileName(filePath));

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri(endpoint))
        {
            Content = content
        };

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"POST {endpoint} failed: {(int)response.StatusCode} {response.ReasonPhrase}. {body}");
        }
        return response;
    }

    public async Task<HttpResponseMessage> GetRawAsync(string endpoint)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, BuildUri(endpoint));
        return await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
    }

    public async Task<T?> PatchAsync<T>(string endpoint, object data)
    {
        var response = await _httpClient.PatchAsJsonAsync(BuildUri(endpoint), data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        var response = await _httpClient.PutAsJsonAsync(BuildUri(endpoint), data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public async Task DeleteAsync(string endpoint)
    {
        var response = await _httpClient.DeleteAsync(BuildUri(endpoint));
        response.EnsureSuccessStatusCode();
    }

    private Uri BuildUri(string endpoint)
    {
        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var absoluteUri)
            && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
            return absoluteUri;

        return new Uri(new Uri(_baseUrl.TrimEnd('/') + "/"), endpoint.TrimStart('/'));
    }
}

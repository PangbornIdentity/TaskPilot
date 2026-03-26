using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskPilot.Shared.DTOs.Common;

namespace TaskPilot.Client.Services;

public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public HttpClientService(HttpClient http)
    {
        _http = http;
    }

    public async Task<T?> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var response = await _http.PostAsJsonAsync(url, body, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var response = await _http.PutAsJsonAsync(url, body, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
    }

    public async Task<TResponse?> PatchAsync<TRequest, TResponse>(string url, TRequest body)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body, options: _jsonOptions)
        };
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
    }

    public async Task DeleteAsync(string url)
    {
        var response = await _http.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
    }

    public async Task<ApiResponse<TData>?> GetApiResponseAsync<TData>(string url)
    {
        return await GetAsync<ApiResponse<TData>>(url);
    }

    public async Task<PagedApiResponse<TData>?> GetPagedApiResponseAsync<TData>(string url)
    {
        return await GetAsync<PagedApiResponse<TData>>(url);
    }

    public async Task<ApiResponse<TResponse>?> PostApiAsync<TRequest, TResponse>(string url, TRequest body)
    {
        return await PostAsync<TRequest, ApiResponse<TResponse>>(url, body);
    }

    public async Task<ApiResponse<TResponse>?> PutApiAsync<TRequest, TResponse>(string url, TRequest body)
    {
        return await PutAsync<TRequest, ApiResponse<TResponse>>(url, body);
    }

    public async Task<ApiResponse<TResponse>?> PatchApiAsync<TRequest, TResponse>(string url, TRequest body)
    {
        return await PatchAsync<TRequest, ApiResponse<TResponse>>(url, body);
    }
}

using TaskPilot.Shared.DTOs.Common;

namespace TaskPilot.Client.Services;

public interface IHttpClientService
{
    Task<T?> GetAsync<T>(string url);
    Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body);
    Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest body);
    Task<TResponse?> PatchAsync<TRequest, TResponse>(string url, TRequest body);
    Task DeleteAsync(string url);
    Task<ApiResponse<TData>?> GetApiResponseAsync<TData>(string url);
    Task<PagedApiResponse<TData>?> GetPagedApiResponseAsync<TData>(string url);
    Task<ApiResponse<TResponse>?> PostApiAsync<TRequest, TResponse>(string url, TRequest body);
    Task<ApiResponse<TResponse>?> PutApiAsync<TRequest, TResponse>(string url, TRequest body);
    Task<ApiResponse<TResponse>?> PatchApiAsync<TRequest, TResponse>(string url, TRequest body);
}

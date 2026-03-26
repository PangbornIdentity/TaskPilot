using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskPilot.Shared.DTOs.Common;

namespace TaskPilot.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public bool IsAuthenticated { get; private set; }
    public CurrentUserInfo? CurrentUser { get; private set; }

    public event Action? AuthStateChanged;

    public AuthService(HttpClient http)
    {
        _http = http;
    }

    public async Task InitializeAsync()
    {
        await GetCurrentUserAsync();
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/v1/account/login",
                new { Email = email, Password = password }, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                await GetCurrentUserAsync();
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RegisterAsync(string email, string password)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/v1/account/register",
                new { Email = email, Password = password }, _jsonOptions);
            if (response.IsSuccessStatusCode)
            {
                await GetCurrentUserAsync();
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _http.PostAsync("api/v1/account/logout", null);
        }
        catch { }
        finally
        {
            IsAuthenticated = false;
            CurrentUser = null;
            AuthStateChanged?.Invoke();
        }
    }

    public async Task GetCurrentUserAsync()
    {
        try
        {
            var response = await _http.GetAsync("api/v1/account/me");
            if (response.IsSuccessStatusCode)
            {
                var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<CurrentUserInfo>>(_jsonOptions);
                if (wrapper?.Data is not null)
                {
                    CurrentUser = wrapper.Data;
                    IsAuthenticated = true;
                    AuthStateChanged?.Invoke();
                    return;
                }
            }
        }
        catch { }
        IsAuthenticated = false;
        CurrentUser = null;
        AuthStateChanged?.Invoke();
    }
}

public record CurrentUserInfo(string Id, string Email, string UserName);

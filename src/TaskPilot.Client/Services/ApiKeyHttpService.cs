using TaskPilot.Shared.DTOs.ApiKeys;

namespace TaskPilot.Client.Services;

public class ApiKeyHttpService
{
    private readonly IHttpClientService _http;
    private const string Base = "api/v1/apikeys";

    public ApiKeyHttpService(IHttpClientService http)
    {
        _http = http;
    }

    public async Task<List<ApiKeyResponse>> GetKeysAsync()
    {
        var result = await _http.GetApiResponseAsync<List<ApiKeyResponse>>(Base);
        return result?.Data ?? new List<ApiKeyResponse>();
    }

    public async Task<CreateApiKeyResponse?> GenerateKeyAsync(CreateApiKeyRequest request)
    {
        var result = await _http.PostApiAsync<CreateApiKeyRequest, CreateApiKeyResponse>(Base, request);
        return result?.Data;
    }

    public async Task<ApiKeyResponse?> RenameKeyAsync(Guid id, RenameApiKeyRequest request)
    {
        var result = await _http.PatchApiAsync<RenameApiKeyRequest, ApiKeyResponse>($"{Base}/{id}/rename", request);
        return result?.Data;
    }

    public async Task ActivateKeyAsync(Guid id)
    {
        await _http.PostAsync<object, object>($"{Base}/{id}/activate", new { });
    }

    public async Task DeactivateKeyAsync(Guid id)
    {
        await _http.PostAsync<object, object>($"{Base}/{id}/deactivate", new { });
    }

    public async Task RevokeKeyAsync(Guid id)
    {
        await _http.DeleteAsync($"{Base}/{id}");
    }
}

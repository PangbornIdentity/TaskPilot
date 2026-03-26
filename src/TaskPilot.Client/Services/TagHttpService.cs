using TaskPilot.Shared.DTOs.Tags;

namespace TaskPilot.Client.Services;

public class TagHttpService
{
    private readonly IHttpClientService _http;
    private const string Base = "api/v1/tags";

    public TagHttpService(IHttpClientService http)
    {
        _http = http;
    }

    public async Task<List<TagResponse>> GetTagsAsync()
    {
        var result = await _http.GetApiResponseAsync<List<TagResponse>>(Base);
        return result?.Data ?? new List<TagResponse>();
    }

    public async Task<TagResponse?> CreateTagAsync(CreateTagRequest request)
    {
        var result = await _http.PostApiAsync<CreateTagRequest, TagResponse>(Base, request);
        return result?.Data;
    }

    public async Task DeleteTagAsync(Guid id)
    {
        await _http.DeleteAsync($"{Base}/{id}");
    }
}

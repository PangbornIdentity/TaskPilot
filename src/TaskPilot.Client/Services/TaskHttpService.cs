using TaskPilot.Shared.DTOs.Common;
using TaskPilot.Shared.DTOs.Tasks;
using TaskPilot.Shared.DTOs.Stats;

namespace TaskPilot.Client.Services;

public class TaskHttpService
{
    private readonly IHttpClientService _http;
    private const string Base = "api/v1/tasks";

    public TaskHttpService(IHttpClientService http)
    {
        _http = http;
    }

    public async Task<PagedApiResponse<TaskResponse>?> GetTasksAsync(TaskQueryParams? queryParams = null)
    {
        var url = BuildTasksUrl(queryParams);
        return await _http.GetPagedApiResponseAsync<TaskResponse>(url);
    }

    public async Task<TaskResponse?> GetTaskByIdAsync(Guid id)
    {
        var result = await _http.GetApiResponseAsync<TaskResponse>($"{Base}/{id}");
        return result?.Data;
    }

    public async Task<TaskResponse?> CreateTaskAsync(CreateTaskRequest request)
    {
        var result = await _http.PostApiAsync<CreateTaskRequest, TaskResponse>(Base, request);
        return result?.Data;
    }

    public async Task<TaskResponse?> UpdateTaskAsync(Guid id, UpdateTaskRequest request)
    {
        var result = await _http.PutApiAsync<UpdateTaskRequest, TaskResponse>($"{Base}/{id}", request);
        return result?.Data;
    }

    public async Task<TaskResponse?> PatchTaskAsync(Guid id, PatchTaskRequest request)
    {
        var result = await _http.PatchApiAsync<PatchTaskRequest, TaskResponse>($"{Base}/{id}", request);
        return result?.Data;
    }

    public async Task<TaskResponse?> CompleteTaskAsync(Guid id, CompleteTaskRequest request)
    {
        var result = await _http.PostApiAsync<CompleteTaskRequest, TaskResponse>($"{Base}/{id}/complete", request);
        return result?.Data;
    }

    public async Task DeleteTaskAsync(Guid id)
    {
        await _http.DeleteAsync($"{Base}/{id}");
    }

    public async Task<TaskStatsResponse?> GetStatsAsync()
    {
        var result = await _http.GetApiResponseAsync<TaskStatsResponse>($"{Base}/stats");
        return result?.Data;
    }

    private static string BuildTasksUrl(TaskQueryParams? p)
    {
        if (p is null) return Base;
        var parts = new List<string>();
        if (p.Status is not null) parts.Add($"status={p.Status}");
        if (p.Type is not null) parts.Add($"type={Uri.EscapeDataString(p.Type)}");
        if (p.Priority is not null) parts.Add($"priority={p.Priority}");
        if (!string.IsNullOrEmpty(p.Search)) parts.Add($"search={Uri.EscapeDataString(p.Search)}");
        if (!string.IsNullOrEmpty(p.Tags)) parts.Add($"tags={Uri.EscapeDataString(p.Tags)}");
        if (p.IsRecurring is not null) parts.Add($"isRecurring={p.IsRecurring}");
        parts.Add($"page={p.Page}");
        parts.Add($"pageSize={p.PageSize}");
        parts.Add($"sortBy={p.SortBy}");
        parts.Add($"sortDir={p.SortDir}");
        return parts.Count > 0 ? $"{Base}?{string.Join("&", parts)}" : Base;
    }
}

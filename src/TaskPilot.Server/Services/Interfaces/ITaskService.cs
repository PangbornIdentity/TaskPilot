using TaskPilot.Shared.DTOs.Common;
using TaskPilot.Shared.DTOs.Tasks;

namespace TaskPilot.Server.Services.Interfaces;

public interface ITaskService
{
    Task<PagedApiResponse<TaskResponse>> GetTasksAsync(TaskQueryParams queryParams, string userId, CancellationToken cancellationToken = default);
    Task<TaskResponse?> GetTaskByIdAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default);
    Task<TaskResponse?> UpdateTaskAsync(Guid id, UpdateTaskRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default);
    Task<TaskResponse?> PatchTaskAsync(Guid id, PatchTaskRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default);
    Task<TaskResponse?> CompleteTaskAsync(Guid id, CompleteTaskRequest request, string userId, string modifiedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteTaskAsync(Guid id, string userId, string modifiedBy, CancellationToken cancellationToken = default);
    Task<bool> UpdateSortOrderAsync(Guid id, int sortOrder, string userId, CancellationToken cancellationToken = default);
}

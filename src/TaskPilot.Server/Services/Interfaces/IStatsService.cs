using TaskPilot.Shared.DTOs.Stats;

namespace TaskPilot.Server.Services.Interfaces;

public interface IStatsService
{
    Task<TaskStatsResponse> GetTaskStatsAsync(string userId, CancellationToken cancellationToken = default);
}

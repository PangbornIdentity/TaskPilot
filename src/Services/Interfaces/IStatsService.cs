using TaskPilot.Models.Stats;

namespace TaskPilot.Services.Interfaces;

public interface IStatsService
{
    Task<TaskStatsResponse> GetTaskStatsAsync(string userId, CancellationToken cancellationToken = default);
}

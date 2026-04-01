using TaskPilot.Entities;

namespace TaskPilot.Repositories.Interfaces;

public interface ITaskTypeRepository
{
    Task<IReadOnlyList<TaskType>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}

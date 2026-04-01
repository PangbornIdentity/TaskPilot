using TaskPilot.Models.TaskTypes;

namespace TaskPilot.Services.Interfaces;

public interface ITaskTypeService
{
    Task<IReadOnlyList<TaskTypeResponse>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}

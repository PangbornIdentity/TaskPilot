using TaskPilot.Repositories.Interfaces;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.TaskTypes;

namespace TaskPilot.Services;

public class TaskTypeService(ITaskTypeRepository taskTypeRepository) : ITaskTypeService
{
    public async Task<IReadOnlyList<TaskTypeResponse>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var types = await taskTypeRepository.GetAllActiveAsync(cancellationToken);
        return types.Select(t => new TaskTypeResponse(t.Id, t.Name, t.SortOrder)).ToList();
    }
}

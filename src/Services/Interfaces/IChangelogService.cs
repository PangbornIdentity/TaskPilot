using TaskPilot.Models.Changelog;

namespace TaskPilot.Services.Interfaces;

public interface IChangelogService
{
    IReadOnlyList<ChangelogVersion> GetAll();
    ChangelogVersion? GetLatest();
}

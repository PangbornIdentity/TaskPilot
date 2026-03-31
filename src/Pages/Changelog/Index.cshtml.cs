using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskPilot.Models.Changelog;
using TaskPilot.Services.Interfaces;

namespace TaskPilot.Pages.Changelog;

public class ChangelogIndexModel(IChangelogService changelogService) : PageModel
{
    public IReadOnlyList<ChangelogVersion> Versions { get; private set; } = [];

    public void OnGet()
    {
        Versions = changelogService.GetAll();
    }
}

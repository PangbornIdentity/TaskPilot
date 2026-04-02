using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TaskPilot.Pages.Integrations;

public class IntegrationsIndexModel(IWebHostEnvironment env) : PageModel
{
    public bool IsDevelopment { get; private set; }

    public void OnGet()
    {
        IsDevelopment = env.IsDevelopment();
    }
}

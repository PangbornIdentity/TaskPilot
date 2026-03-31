using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TaskPilot.Models.Audit;
using TaskPilot.Services.Interfaces;

namespace TaskPilot.Pages.Audit;

public class AuditIndexModel(IAuditService auditService, IActivityLogService activityLogService) : PageModel
{
    // API Access tab
    public List<AuditLogResponse> ApiLogs { get; private set; } = [];
    public AuditSummaryResponse? ApiSummary { get; private set; }
    public int ApiCurrentPage { get; private set; } = 1;
    public int ApiTotalPages { get; private set; } = 1;
    public string? MethodFilter { get; private set; }
    public DateTime? From { get; private set; }
    public DateTime? To { get; private set; }

    // Task History tab
    public List<ActivityLogResponse> ActivityLogs { get; private set; } = [];
    public int ActivityCurrentPage { get; private set; } = 1;
    public int ActivityTotalPages { get; private set; } = 1;
    public string? FieldFilter { get; private set; }
    public string? ChangedByFilter { get; private set; }

    // Tab state
    public string ActiveTab { get; private set; } = "tasks";

    public async Task OnGetAsync(
        string tab = "tasks",
        // API Access filters
        int apiPage = 1, string? method = null, DateTime? from = null, DateTime? to = null,
        // Task History filters
        int activityPage = 1, string? field = null, string? changedBy = null)
    {
        ActiveTab = tab == "api" ? "api" : "tasks";
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        if (ActiveTab == "api")
        {
            ApiCurrentPage = apiPage;
            MethodFilter = method;
            From = from;
            To = to;

            ApiSummary = await auditService.GetSummaryAsync(userId);

            var result = await auditService.GetAuditLogsAsync(new AuditQueryParams(
                HttpMethod: string.IsNullOrWhiteSpace(method) ? null : method.ToUpperInvariant(),
                From: from,
                To: to,
                Page: apiPage,
                PageSize: 50
            ), userId);

            ApiLogs = result.Data?.ToList() ?? [];
            ApiTotalPages = result.Meta?.TotalPages ?? 1;
        }
        else
        {
            ActivityCurrentPage = activityPage;
            FieldFilter = field;
            ChangedByFilter = changedBy;

            var result = await activityLogService.GetPagedAsync(new ActivityLogQueryParams(
                From: from,
                To: to,
                FieldChanged: string.IsNullOrWhiteSpace(field) ? null : field,
                ChangedBy: string.IsNullOrWhiteSpace(changedBy) ? null : changedBy,
                Page: activityPage,
                PageSize: 50
            ), userId);

            ActivityLogs = result.Data?.ToList() ?? [];
            ActivityTotalPages = result.Meta?.TotalPages ?? 1;
            From = from;
            To = to;
        }
    }
}

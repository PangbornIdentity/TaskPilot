using Microsoft.EntityFrameworkCore;
using TaskPilot.Data;
using TaskPilot.Services.Interfaces;
using TaskPilot.Models.Stats;
using TaskPilot.Models.Enums;
using TaskStatus = TaskPilot.Models.Enums.TaskStatus;

namespace TaskPilot.Services;

public class StatsService(ApplicationDbContext context) : IStatsService
{
    public async Task<TaskStatsResponse> GetTaskStatsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        var totalActive = await context.Tasks
            .CountAsync(t => t.UserId == userId && t.Status != TaskStatus.Completed && t.Status != TaskStatus.Cancelled, cancellationToken);

        var completedToday = await context.Tasks
            .CountAsync(t => t.UserId == userId && t.CompletedDate.HasValue && t.CompletedDate.Value >= today, cancellationToken);

        var overdue = await context.Tasks
            .CountAsync(t => t.UserId == userId && t.TargetDate < today
                && t.Status != TaskStatus.Completed && t.Status != TaskStatus.Cancelled, cancellationToken);

        var inProgress = await context.Tasks
            .CountAsync(t => t.UserId == userId && t.Status == TaskStatus.InProgress, cancellationToken);

        var blocked = await context.Tasks
            .CountAsync(t => t.UserId == userId && t.Status == TaskStatus.Blocked, cancellationToken);

        var completedPerWeek = await GetCompletedPerWeekAsync(userId, 12, cancellationToken);
        var completedPerMonth = await GetCompletedPerMonthAsync(userId, 12, cancellationToken);
        var completedPerYear = await GetCompletedPerYearAsync(userId, cancellationToken);
        var completionRate = await GetCompletionRateAsync(userId, 12, cancellationToken);
        var byType = await GetByTypeAsync(userId, cancellationToken);
        var byPriority = await GetByPriorityAsync(userId, cancellationToken);
        var avgCompletion = await GetAvgCompletionAsync(userId, 12, cancellationToken);

        return new TaskStatsResponse(totalActive, completedToday, overdue, inProgress, blocked,
            completedPerWeek, completedPerMonth, completedPerYear, completionRate, byType, byPriority, avgCompletion);
    }

    private async Task<List<WeeklyCompletionData>> GetCompletedPerWeekAsync(string userId, int weeks, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-weeks * 7);
        // Load raw data client-side to avoid SQLite DayOfYear translation issues
        var raw = await context.Tasks
            .Where(t => t.UserId == userId && t.CompletedDate.HasValue && t.CompletedDate >= cutoff)
            .Select(t => new { t.CompletedDate })
            .ToListAsync(ct);

        return raw
            .GroupBy(t => new { t.CompletedDate!.Value.Year, Week = (t.CompletedDate.Value.DayOfYear - 1) / 7 })
            .Select(g => new WeeklyCompletionData($"W{g.Key.Week + 1}/{g.Key.Year}", g.Count()))
            .OrderBy(x => x.WeekLabel)
            .ToList();
    }

    private async Task<List<MonthlyCompletionData>> GetCompletedPerMonthAsync(string userId, int months, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-months);
        var raw = await context.Tasks
            .Where(t => t.UserId == userId && t.CompletedDate.HasValue && t.CompletedDate >= cutoff)
            .Select(t => new { t.CompletedDate })
            .ToListAsync(ct);

        return raw
            .GroupBy(t => new { t.CompletedDate!.Value.Year, t.CompletedDate.Value.Month })
            .Select(g => new MonthlyCompletionData($"{g.Key.Year}-{g.Key.Month:D2}", g.Count()))
            .OrderBy(x => x.MonthLabel)
            .ToList();
    }

    private async Task<List<YearlyCompletionData>> GetCompletedPerYearAsync(string userId, CancellationToken ct)
    {
        var raw = await context.Tasks
            .Where(t => t.UserId == userId && t.CompletedDate.HasValue)
            .Select(t => new { t.CompletedDate })
            .ToListAsync(ct);

        return raw
            .GroupBy(t => t.CompletedDate!.Value.Year)
            .Select(g => new YearlyCompletionData(g.Key, g.Count()))
            .OrderBy(x => x.Year) // Year is the property name from YearlyCompletionData
            .ToList();
    }

    private async Task<List<CompletionRateData>> GetCompletionRateAsync(string userId, int weeks, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-weeks * 7);

        var createdRaw = await context.Tasks
            .Where(t => t.UserId == userId && t.CreatedDate >= cutoff)
            .Select(t => new { t.CreatedDate })
            .ToListAsync(ct);

        var completedRaw = await context.Tasks
            .Where(t => t.UserId == userId && t.CompletedDate.HasValue && t.CompletedDate >= cutoff)
            .Select(t => new { t.CompletedDate })
            .ToListAsync(ct);

        var created = createdRaw
            .GroupBy(t => new { t.CreatedDate.Year, Week = (t.CreatedDate.DayOfYear - 1) / 7 })
            .Select(g => new { g.Key.Year, g.Key.Week, Count = g.Count() })
            .ToList();

        var completed = completedRaw
            .GroupBy(t => new { t.CompletedDate!.Value.Year, Week = (t.CompletedDate.Value.DayOfYear - 1) / 7 })
            .Select(g => new { g.Key.Year, g.Key.Week, Count = g.Count() })
            .ToList();

        return created.Select(c =>
        {
            var comp = completed.FirstOrDefault(x => x.Year == c.Year && x.Week == c.Week);
            return new CompletionRateData($"W{c.Week + 1}/{c.Year}", c.Count, comp?.Count ?? 0);
        }).ToList();
    }

    private async Task<List<TypeBreakdownData>> GetByTypeAsync(string userId, CancellationToken ct)
    {
        var raw = await context.Tasks
            .Where(t => t.UserId == userId && t.Status != TaskStatus.Completed && t.Status != TaskStatus.Cancelled)
            .GroupBy(t => t.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return raw.Select(x => new TypeBreakdownData(x.Type, x.Count)).ToList();
    }

    private async Task<List<PriorityBreakdownData>> GetByPriorityAsync(string userId, CancellationToken ct)
    {
        // Load client-side to avoid complex grouped subquery issues with SQLite
        var raw = await context.Tasks
            .Where(t => t.UserId == userId && t.Status != TaskStatus.Completed && t.Status != TaskStatus.Cancelled)
            .Select(t => new { t.Priority, t.Status })
            .ToListAsync(ct);

        return raw
            .GroupBy(t => t.Priority)
            .Select(g => new PriorityBreakdownData(
                g.Key.ToString(),
                g.Count(t => t.Status == TaskStatus.NotStarted),
                g.Count(t => t.Status == TaskStatus.InProgress),
                g.Count(t => t.Status == TaskStatus.Blocked)))
            .ToList();
    }

    private async Task<List<AvgCompletionData>> GetAvgCompletionAsync(string userId, int weeks, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-weeks * 7);
        // Load client-side to perform DateTime arithmetic that SQLite can't translate
        var raw = await context.Tasks
            .Where(t => t.UserId == userId && t.CompletedDate.HasValue && t.CompletedDate >= cutoff)
            .Select(t => new { t.CompletedDate, t.CreatedDate })
            .ToListAsync(ct);

        return raw
            .GroupBy(t => new { t.CompletedDate!.Value.Year, Week = (t.CompletedDate.Value.DayOfYear - 1) / 7 })
            .Select(g => new AvgCompletionData(
                $"W{g.Key.Week + 1}/{g.Key.Year}",
                g.Average(t => (t.CompletedDate!.Value - t.CreatedDate).TotalDays)))
            .OrderBy(x => x.WeekLabel)
            .ToList();
    }
}

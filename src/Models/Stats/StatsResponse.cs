namespace TaskPilot.Models.Stats;

public record TaskStatsResponse(
    int TotalActive,
    int CompletedToday,
    int Overdue,
    int InProgress,
    int Blocked,
    IReadOnlyList<WeeklyCompletionData> CompletedPerWeek,
    IReadOnlyList<MonthlyCompletionData> CompletedPerMonth,
    IReadOnlyList<YearlyCompletionData> CompletedPerYear,
    IReadOnlyList<CompletionRateData> CompletionRateByWeek,
    IReadOnlyList<TypeBreakdownData> ByType,
    IReadOnlyList<PriorityBreakdownData> ByPriority,
    IReadOnlyList<AvgCompletionData> AvgTimeToCompletionByWeek
);

public record WeeklyCompletionData(string WeekLabel, int Count);
public record MonthlyCompletionData(string MonthLabel, int Count);
public record YearlyCompletionData(int Year, int Count);
public record CompletionRateData(string WeekLabel, int Created, int Completed);
public record TypeBreakdownData(string Type, int Count);
public record PriorityBreakdownData(string Priority, int NotStarted, int InProgress, int Blocked);
public record AvgCompletionData(string WeekLabel, double AvgDays);

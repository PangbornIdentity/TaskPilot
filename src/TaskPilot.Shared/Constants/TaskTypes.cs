namespace TaskPilot.Shared.Constants;

public static class TaskTypes
{
    public const string Work = "Work";
    public const string Personal = "Personal";
    public const string Health = "Health";
    public const string Finance = "Finance";
    public const string Learning = "Learning";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All = [Work, Personal, Health, Finance, Learning, Other];
}

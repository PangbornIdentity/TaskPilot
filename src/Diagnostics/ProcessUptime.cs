namespace TaskPilot.Diagnostics;

/// <summary>Captures the process start time once for uptime calculations.</summary>
public static class ProcessUptime
{
    /// <summary>UTC time when the application process started.</summary>
    public static readonly DateTime StartTime = DateTime.UtcNow;
}

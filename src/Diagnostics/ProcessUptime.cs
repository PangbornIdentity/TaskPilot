using System.Diagnostics;

namespace TaskPilot.Diagnostics;

public static class ProcessUptime
{
    public static readonly DateTime StartTime = Process.GetCurrentProcess().StartTime.ToUniversalTime();
}

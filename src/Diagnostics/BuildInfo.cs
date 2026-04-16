using System.Reflection;

namespace TaskPilot.Diagnostics;

/// <summary>
/// Static accessor for build-time version metadata stamped into the assembly via MSBuild.
/// All values are read once from <see cref="AssemblyMetadataAttribute"/> and cached.
/// Falls back to "unknown" when git stamping was unavailable (e.g. source-only builds).
/// </summary>
public static class BuildInfo
{
    private static readonly Assembly _assembly = typeof(BuildInfo).Assembly;

    /// <summary>The SemVer string from &lt;Version&gt; in the csproj, e.g. "1.8.0".</summary>
    public static string Version { get; } = _assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    /// <summary>Full 40-char git SHA, or "unknown".</summary>
    public static string GitCommit { get; } = GetMetadata("GitCommit");

    /// <summary>Short 7-char git SHA, or "unknown".</summary>
    public static string GitCommitShort { get; } = GetMetadata("GitCommitShort");

    /// <summary>UTC build timestamp as ISO 8601, or <see cref="DateTime.MinValue"/> when unavailable.</summary>
    public static DateTime BuildTimestampUtc { get; } = ParseBuildTimestamp(GetMetadata("BuildTimestampUtc"));

    private static string GetMetadata(string key)
    {
        var attr = _assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == key);

        var value = attr?.Value?.Trim();
        return string.IsNullOrEmpty(value) ? "unknown" : value;
    }

    private static DateTime ParseBuildTimestamp(string raw)
    {
        if (raw == "unknown") return DateTime.MinValue;
        return DateTime.TryParse(raw, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt
            : DateTime.MinValue;
    }
}

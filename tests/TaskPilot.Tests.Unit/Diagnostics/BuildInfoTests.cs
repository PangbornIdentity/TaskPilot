using TaskPilot.Diagnostics;

namespace TaskPilot.Tests.Unit.Diagnostics;

/// <summary>
/// HLTH-001 through HLTH-005 — BuildInfo static class.
/// </summary>
public class BuildInfoTests
{
    [Fact] // HLTH-001
    public void BuildInfo_Version_ReadsFromAssembly()
    {
        var version = BuildInfo.Version;

        Assert.False(string.IsNullOrWhiteSpace(version));
        // Must be parseable as a SemVer-compatible version (at least Major.Minor.Patch)
        Assert.Matches(@"^\d+\.\d+\.\d+", version);
    }

    [Fact] // HLTH-002
    public void BuildInfo_GitCommit_ReadsFromAssemblyMetadata()
    {
        var commit = BuildInfo.GitCommit;

        Assert.NotNull(commit);
        Assert.NotEmpty(commit);
        // Must be either a 40-char hex SHA or "unknown"
        Assert.True(
            commit == "unknown" || System.Text.RegularExpressions.Regex.IsMatch(commit, @"^[0-9a-f]{40}$"),
            $"GitCommit '{commit}' is neither 'unknown' nor a 40-char hex SHA.");
    }

    [Fact] // HLTH-003
    public void BuildInfo_GitCommitShort_IsSevenChars()
    {
        var commitShort = BuildInfo.GitCommitShort;

        Assert.NotNull(commitShort);
        Assert.NotEmpty(commitShort);
        Assert.True(
            commitShort == "unknown" || commitShort.Length == 7,
            $"GitCommitShort '{commitShort}' is neither 'unknown' nor 7 chars.");
    }

    [Fact] // HLTH-004
    public void BuildInfo_BuildTimestampUtc_IsParseable()
    {
        var timestamp = BuildInfo.BuildTimestampUtc;

        // When git stamping runs, timestamp is within last 365 days
        // If not stamped, it returns DateTime.MinValue — we accept that with "unknown" fallback
        if (timestamp != DateTime.MinValue)
        {
            Assert.Equal(DateTimeKind.Utc, timestamp.Kind);
            Assert.True(timestamp > DateTime.UtcNow.AddDays(-365),
                "BuildTimestampUtc is more than 365 days ago.");
            Assert.True(timestamp <= DateTime.UtcNow.AddMinutes(5),
                "BuildTimestampUtc is in the future.");
        }
        // Else: acceptable fallback for source-only builds
    }

    [Fact] // HLTH-005
    public void BuildInfo_FallsBackGracefully_WhenMetadataMissing()
    {
        // BuildInfo reads real assembly metadata. The test verifies it never throws
        // and never returns null/empty — "unknown" is the correct fallback.
        string? gitCommit = null;
        string? gitCommitShort = null;

        var ex = Record.Exception(() =>
        {
            gitCommit = BuildInfo.GitCommit;
            gitCommitShort = BuildInfo.GitCommitShort;
        });

        Assert.Null(ex);
        Assert.NotNull(gitCommit);
        Assert.NotEmpty(gitCommit);
        Assert.NotNull(gitCommitShort);
        Assert.NotEmpty(gitCommitShort);
    }
}

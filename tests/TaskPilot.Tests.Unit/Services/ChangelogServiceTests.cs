using TaskPilot.Services;

namespace TaskPilot.Tests.Unit.Services;

public class ChangelogServiceTests
{
    private static string MinimalJson(string version = "1.0", string type = "major") => $$"""
        {
          "versions": [
            {
              "version": "{{version}}",
              "releaseDate": "2026-01-01",
              "versionType": "{{type}}",
              "summary": "Test release",
              "changes": [
                { "type": "Feature", "description": "Something new" }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void GetAll_ValidJson_ReturnsVersions()
    {
        var service = new ChangelogService(MinimalJson());

        var versions = service.GetAll();

        Assert.Single(versions);
        Assert.Equal("1.0", versions[0].Version);
    }

    [Fact]
    public void GetLatest_ValidJson_ReturnsFirstEntry()
    {
        var service = new ChangelogService(MinimalJson("2.0"));

        var latest = service.GetLatest();

        Assert.NotNull(latest);
        Assert.Equal("2.0", latest.Version);
    }

    [Fact]
    public void GetAll_MultipleVersions_SortedDescending()
    {
        var json = """
            {
              "versions": [
                { "version": "1.0", "releaseDate": "2026-01-01", "versionType": "major", "summary": "v1", "changes": [] },
                { "version": "1.2", "releaseDate": "2026-03-01", "versionType": "minor", "summary": "v1.2", "changes": [] },
                { "version": "1.1", "releaseDate": "2026-02-01", "versionType": "minor", "summary": "v1.1", "changes": [] }
              ]
            }
            """;

        var service = new ChangelogService(json);
        var versions = service.GetAll();

        Assert.Equal("1.2", versions[0].Version);
        Assert.Equal("1.1", versions[1].Version);
        Assert.Equal("1.0", versions[2].Version);
    }

    [Fact]
    public void GetLatest_MultipleVersions_ReturnsHighestVersion()
    {
        var json = """
            {
              "versions": [
                { "version": "1.0", "releaseDate": "2026-01-01", "versionType": "major", "summary": "v1", "changes": [] },
                { "version": "1.3", "releaseDate": "2026-04-01", "versionType": "minor", "summary": "v1.3", "changes": [] },
                { "version": "1.1", "releaseDate": "2026-02-01", "versionType": "minor", "summary": "v1.1", "changes": [] }
              ]
            }
            """;

        var service = new ChangelogService(json);

        Assert.Equal("1.3", service.GetLatest()!.Version);
    }

    [Fact]
    public void GetAll_EmptyJson_ReturnsEmptyList()
    {
        var service = new ChangelogService("{}");

        Assert.Empty(service.GetAll());
    }

    [Fact]
    public void GetLatest_EmptyJson_ReturnsNull()
    {
        var service = new ChangelogService("{}");

        Assert.Null(service.GetLatest());
    }

    [Fact]
    public void IsMajor_MajorVersionType_ReturnsTrue()
    {
        var service = new ChangelogService(MinimalJson("1.0", "major"));

        Assert.True(service.GetLatest()!.IsMajor);
    }

    [Fact]
    public void IsMajor_MinorVersionType_ReturnsFalse()
    {
        var service = new ChangelogService(MinimalJson("1.1", "minor"));

        Assert.False(service.GetLatest()!.IsMajor);
    }

    [Fact]
    public void GetAll_ParsesChanges_TypeAndDescription()
    {
        var json = """
            {
              "versions": [
                {
                  "version": "1.0", "releaseDate": "2026-01-01", "versionType": "major",
                  "summary": "Initial",
                  "changes": [
                    { "type": "Feature", "description": "Task management" },
                    { "type": "Fix", "description": "Login redirect" }
                  ]
                }
              ]
            }
            """;

        var service = new ChangelogService(json);
        var changes = service.GetAll()[0].Changes;

        Assert.Equal(2, changes.Count);
        Assert.Equal("Feature", changes[0].Type);
        Assert.Equal("Task management", changes[0].Description);
        Assert.Equal("Fix", changes[1].Type);
    }

    [Fact]
    public void GetAll_MalformedJson_ReturnsEmptyList()
    {
        var service = new ChangelogService("not json at all");

        Assert.Empty(service.GetAll());
    }

    [Fact]
    public void GetAll_VersionsWithDoubleDigitMinor_OrdersByNumericComponents()
    {
        // Regression: ordinal-string sort treats "1.10" < "1.8" because at index 2
        // the digit '1' (0x31) < '8' (0x38). The fix parses each version into a
        // (major, minor, patch) tuple and sorts numerically.
        var json = """
            {
              "versions": [
                { "version": "1.8",  "releaseDate": "2026-04-15", "versionType": "minor", "summary": "v1.8",  "changes": [] },
                { "version": "1.10", "releaseDate": "2026-05-01", "versionType": "minor", "summary": "v1.10", "changes": [] },
                { "version": "1.9",  "releaseDate": "2026-04-17", "versionType": "patch", "summary": "v1.9",  "changes": [] }
              ]
            }
            """;

        var service = new ChangelogService(json);
        var versions = service.GetAll();

        Assert.Equal("1.10", versions[0].Version);
        Assert.Equal("1.9",  versions[1].Version);
        Assert.Equal("1.8",  versions[2].Version);
    }

    [Fact]
    public void GetAll_PatchVersions_OrderedByThirdSegment()
    {
        // 1.10.10 should beat 1.10.1 should beat 1.10.0 — same regression as above
        // applied to the patch segment.
        var json = """
            {
              "versions": [
                { "version": "1.10.0",  "releaseDate": "2026-05-01", "versionType": "minor", "summary": "v1.10",     "changes": [] },
                { "version": "1.10.10", "releaseDate": "2026-05-15", "versionType": "patch", "summary": "v1.10.10",  "changes": [] },
                { "version": "1.10.1",  "releaseDate": "2026-05-02", "versionType": "patch", "summary": "v1.10.1",   "changes": [] }
              ]
            }
            """;

        var service = new ChangelogService(json);
        var versions = service.GetAll();

        Assert.Equal("1.10.10", versions[0].Version);
        Assert.Equal("1.10.1",  versions[1].Version);
        Assert.Equal("1.10.0",  versions[2].Version);
    }

    [Fact]
    public void GetAll_MalformedVersion_DoesNotThrowAndSortsAfterNumeric()
    {
        // A malformed version segment falls back to 0 — the entry is still returned,
        // and it sorts behind any well-formed entry.
        var json = """
            {
              "versions": [
                { "version": "abc",  "releaseDate": "2026-01-01", "versionType": "major", "summary": "broken",  "changes": [] },
                { "version": "1.10", "releaseDate": "2026-05-01", "versionType": "minor", "summary": "v1.10",   "changes": [] }
              ]
            }
            """;

        var service = new ChangelogService(json);
        var versions = service.GetAll();

        Assert.Equal(2, versions.Count);
        Assert.Equal("1.10", versions[0].Version);
        Assert.Equal("abc",  versions[1].Version);
    }

    [Fact]
    public void GetLatest_PicksHighestSemver_NotLexLargest()
    {
        // The bug visible to users: GetLatest returned "1.8" instead of "1.10" because
        // the cached order was wrong.
        var json = """
            {
              "versions": [
                { "version": "1.8",  "releaseDate": "2026-04-15", "versionType": "minor", "summary": "v1.8",  "changes": [] },
                { "version": "1.10", "releaseDate": "2026-05-01", "versionType": "minor", "summary": "v1.10", "changes": [] }
              ]
            }
            """;

        var service = new ChangelogService(json);

        Assert.Equal("1.10", service.GetLatest()!.Version);
    }
}

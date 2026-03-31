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
}

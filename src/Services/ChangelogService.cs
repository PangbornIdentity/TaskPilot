using System.Text.Json;
using System.Text.Json.Serialization;
using TaskPilot.Models.Changelog;
using TaskPilot.Services.Interfaces;

namespace TaskPilot.Services;

public class ChangelogService : IChangelogService
{
    private readonly IReadOnlyList<ChangelogVersion> _versions;

    public ChangelogService(string jsonContent)
    {
        _versions = Parse(jsonContent);
    }

    public IReadOnlyList<ChangelogVersion> GetAll() => _versions;

    public ChangelogVersion? GetLatest() => _versions.Count > 0 ? _versions[0] : null;

    private static IReadOnlyList<ChangelogVersion> Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var doc = JsonSerializer.Deserialize<ChangelogDocument>(json, options);
            if (doc?.Versions is null) return [];

        return doc.Versions
            .Select(v => new ChangelogVersion(
                v.Version ?? "0.0",
                DateOnly.TryParse(v.ReleaseDate, out var d) ? d : DateOnly.MinValue,
                v.VersionType ?? "minor",
                v.Summary ?? string.Empty,
                (v.Changes ?? []).Select(c => new ChangelogEntry(c.Type ?? "Change", c.Description ?? "")).ToList()))
            .OrderByDescending(v => ParseSemVer(v.Version))
            .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    // Parse a version string into a tuple so it sorts numerically, not lexicographically.
    // The previous OrdinalIgnoreCase string comparer treated "1.10" as less than "1.8"
    // (char index 2: '1' < '8'), so versions ≥ 1.10 were buried in the changelog UI.
    // Tolerates malformed segments by falling back to 0 (and the raw string as a final
    // tiebreaker so duplicate versions stay deterministic).
    private static (int Major, int Minor, int Patch, string Raw) ParseSemVer(string version)
    {
        var parts = (version ?? string.Empty).Split('.');
        int Get(int i) => parts.Length > i && int.TryParse(parts[i], out var n) ? n : 0;
        return (Get(0), Get(1), Get(2), version ?? string.Empty);
    }

    // Private DTOs for JSON deserialization only
    private sealed class ChangelogDocument
    {
        public List<VersionDto>? Versions { get; set; }
    }

    private sealed class VersionDto
    {
        public string? Version { get; set; }
        public string? ReleaseDate { get; set; }
        public string? VersionType { get; set; }
        public string? Summary { get; set; }
        public List<EntryDto>? Changes { get; set; }
    }

    private sealed class EntryDto
    {
        public string? Type { get; set; }
        public string? Description { get; set; }
    }
}

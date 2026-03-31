namespace TaskPilot.Models.Changelog;

public record ChangelogEntry(string Type, string Description);

public record ChangelogVersion(
    string Version,
    DateOnly ReleaseDate,
    string VersionType,
    string Summary,
    IReadOnlyList<ChangelogEntry> Changes)
{
    public bool IsMajor => VersionType.Equals("major", StringComparison.OrdinalIgnoreCase);
}

namespace TaskPilot.Models.Tasks;

/// <summary>
/// Optional overrides for POST /api/v1/tasks/{id}/clone.
/// An empty body <c>{}</c> is valid — all fields have safe defaults.
/// </summary>
/// <param name="Title">
/// Override the clone's title. When null, empty, or whitespace the service produces
/// <c>"{source.Title} (copy)"</c>. Still subject to the 200-character limit.
/// </param>
/// <param name="TargetDate">
/// Override the clone's target date. Ignored when <see cref="ClearTargetDate"/> is true.
/// When omitted, the source's TargetDate is copied verbatim.
/// </param>
/// <param name="ClearTargetDate">
/// When true, the clone's TargetDate is set to null regardless of the source value
/// or any TargetDate override in this request. Defaults to false.
/// </param>
public record CloneTaskRequest(
    string? Title = null,
    DateTime? TargetDate = null,
    bool ClearTargetDate = false);

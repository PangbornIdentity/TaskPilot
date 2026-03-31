namespace TaskPilot.Models.Tags;

public record TagResponse(
    Guid Id,
    string Name,
    string Color,
    DateTime CreatedDate
);

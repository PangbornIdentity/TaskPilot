namespace TaskPilot.Shared.DTOs.Tags;

public record TagResponse(
    Guid Id,
    string Name,
    string Color,
    DateTime CreatedDate
);

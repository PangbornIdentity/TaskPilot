namespace TaskPilot.Shared.DTOs.Common;

public record ApiResponse<T>(T Data, ResponseMeta Meta);

public record PagedApiResponse<T>(IReadOnlyList<T> Data, PagedResponseMeta Meta);

public record ResponseMeta(DateTime Timestamp, string RequestId);

public record PagedResponseMeta(DateTime Timestamp, string RequestId, int Page, int PageSize, int TotalCount, int TotalPages)
    : ResponseMeta(Timestamp, RequestId);

public record ErrorResponse(ApiError Error);

public record ApiError(string Code, string Message, IReadOnlyList<FieldError>? Details = null);

public record FieldError(string Field, string Message);

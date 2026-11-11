namespace SchoolAdmission.Api.DTOs;

public sealed record ApiErrorResponse(
    ApiErrorDetail Error
);

public sealed record ApiErrorDetail(
    string Code,
    string Message
);
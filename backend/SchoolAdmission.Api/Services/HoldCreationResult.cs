using SchoolAdmission.Api.DTOs;

namespace SchoolAdmission.Api.Services;

public enum HoldCreationStatus
{
    Created,
    ClassNotFound,
    DuplicateApplication,
    NoSeatsAvailable
}

public sealed record HoldCreationResult(
    HoldCreationStatus Status,
    CreateHoldResponse? Hold = null
);
using SchoolAdmission.Api.DTOs;

namespace SchoolAdmission.Api.Services;

public enum ConfirmHoldStatus
{
    Confirmed,
    NotFound,
    NotOwner,
    Expired,
    Cancelled
}

public enum CancelHoldStatus
{
    Cancelled,
    NotFound,
    NotOwner,
    Expired,
    Confirmed
}

public sealed record ConfirmHoldResult(
    ConfirmHoldStatus Status,
    HoldStatusResponse? Response = null
);
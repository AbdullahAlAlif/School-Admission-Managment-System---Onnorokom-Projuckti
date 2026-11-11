namespace SchoolAdmission.Api.DTOs;

public sealed class HoldResponse
{
    public string HoldId { get; set; } = string.Empty;

    public string ClassId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string ExpiresAt { get; set; } = string.Empty;
}
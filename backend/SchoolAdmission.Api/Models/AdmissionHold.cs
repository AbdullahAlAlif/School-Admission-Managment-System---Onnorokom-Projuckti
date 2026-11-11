namespace SchoolAdmission.Api.Models;

public sealed class AdmissionHold
{
    public string Id { get; set; } =
        "h_" + Guid.NewGuid().ToString("N")[..12];

    public string ClassId { get; set; } = string.Empty;

    public string StudentId { get; set; } = string.Empty;

    public HoldStatus Status { get; set; } = HoldStatus.HELD;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    // Navigation property.
    public SchoolClass Class { get; set; } = null!;
}
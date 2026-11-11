namespace SchoolAdmission.Api.DTOs;

public sealed class CreateClassRequest
{
    public string? Name { get; set; }

    public int? TotalSeats { get; set; }
}
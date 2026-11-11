namespace SchoolAdmission.Api.DTOs;

public sealed class CreateClassResponse
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int TotalSeats { get; set; }
}
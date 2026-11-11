namespace SchoolAdmission.Api.Models;

public sealed class SchoolClass
{
    public string Id { get; set; } =
        "c_" + Guid.NewGuid().ToString("N")[..12];

    public string Name { get; set; } = string.Empty;

    public int TotalSeats { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // One class can have multiple admission holds.
    public ICollection<AdmissionHold> Holds { get; set; }
        = new List<AdmissionHold>();
}
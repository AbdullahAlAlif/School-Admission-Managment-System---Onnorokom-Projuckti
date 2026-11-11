using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Services;

public sealed class ClassService : IClassService
{
    private readonly SchoolAdmissionDbContext _db;

    public ClassService(SchoolAdmissionDbContext db)
    {
        _db = db;
    }

    // ==========================================
    // CREATE CLASS
    // ==========================================

    public async Task<CreateClassResponse> CreateAsync(
        string name,
        int totalSeats,
        CancellationToken cancellationToken)
    {
        var schoolClass = new SchoolClass
        {
            Name = name,
            TotalSeats = totalSeats
        };

        _db.Classes.Add(schoolClass);

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateClassResponse
        {
            Id = schoolClass.Id,
            Name = schoolClass.Name,
            TotalSeats = schoolClass.TotalSeats
        };
    }


    // ==========================================
    // GET ALL CLASSES
    // ==========================================

    public async Task<IReadOnlyList<ClassResponse>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        // Get the current time from PostgreSQL.
        var databaseNow = await _db.Database
            .SqlQuery<DateTimeOffset>(
                $"SELECT CURRENT_TIMESTAMP AS \"Value\"")
            .SingleAsync(cancellationToken);

        // Calculate seat counts directly from hold records.
        var classes = await _db.Classes
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,

                HeldSeats = c.Holds.Count(h =>
                    h.Status == HoldStatus.HELD &&
                    h.ExpiresAt > databaseNow),

                ConfirmedSeats = c.Holds.Count(h =>
                    h.Status == HoldStatus.CONFIRMED)
            })
            .ToListAsync(cancellationToken);

        // Convert database results into API responses.
        return classes.Select(c => new ClassResponse
        {
            Id = c.Id,
            Name = c.Name,
            TotalSeats = c.TotalSeats,
            HeldSeats = c.HeldSeats,
            ConfirmedSeats = c.ConfirmedSeats,

            AvailableSeats = Math.Max(
                0,
                c.TotalSeats - c.HeldSeats - c.ConfirmedSeats
            )
        }).ToList();
    }
}
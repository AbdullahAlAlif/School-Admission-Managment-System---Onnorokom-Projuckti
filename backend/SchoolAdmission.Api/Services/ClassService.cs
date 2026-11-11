using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Configuration;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Services;

public sealed class ClassService : IClassService
{
    private readonly SchoolAdmissionDbContext _db;
    private readonly HoldSettings _holdSettings;

    public ClassService(
        SchoolAdmissionDbContext db,
        HoldSettings holdSettings)
    {
        _db = db;
        _holdSettings = holdSettings;
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
        var databaseNow =
            await GetDatabaseNowAsync(cancellationToken);

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


    // ==========================================
    // GET ONE CLASS
    // ==========================================

    public async Task<ClassResponse?> GetByIdAsync(
        string classId,
        CancellationToken cancellationToken)
    {
        var databaseNow =
            await GetDatabaseNowAsync(cancellationToken);

        var result = await _db.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId)
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
            .SingleOrDefaultAsync(cancellationToken);

        if (result is null)
        {
            return null;
        }

        return new ClassResponse
        {
            Id = result.Id,
            Name = result.Name,
            TotalSeats = result.TotalSeats,
            HeldSeats = result.HeldSeats,
            ConfirmedSeats = result.ConfirmedSeats,

            AvailableSeats = Math.Max(
                0,
                result.TotalSeats -
                result.HeldSeats -
                result.ConfirmedSeats
            )
        };
    }


    // ==========================================
    // CREATE ADMISSION HOLD
    // ==========================================

    public async Task<HoldCreationResult> CreateHoldAsync(
        string classId,
        string studentId,
        CancellationToken cancellationToken)
    {
        // STEP 1: Begin a database transaction.
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken
            );


        // STEP 2: Lock the requested class row.
        //
        // SELECT ... FOR UPDATE prevents other transactions
        // from simultaneously changing seat allocation
        // for this same class.

        var lockedClasses = await _db.Classes
            .FromSqlInterpolated(
                $"SELECT * FROM classes WHERE id = {classId} FOR UPDATE"
            )
            .ToListAsync(cancellationToken);

        var schoolClass = lockedClasses.SingleOrDefault();

        if (schoolClass is null)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new HoldCreationResult(
                HoldCreationStatus.ClassNotFound
            );
        }


        // STEP 3: Get the actual current database time.
        //
        // We do this AFTER acquiring the lock because
        // another request may have been waiting for it.

        var databaseNow =
            await GetDatabaseNowAsync(cancellationToken);


        // STEP 4: Mark expired holds for this class.
        //
        // The partial unique index checks stored status.
        // We must update stale HELD records before inserting
        // another application for the same student.

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE holds
            SET status = 'EXPIRED'
            WHERE class_id = {classId}
              AND status = 'HELD'
              AND expires_at <= {databaseNow}
            """,
            cancellationToken
        );


        // STEP 5: Check whether this student already has
        // an occupying application in this class.

        var alreadyApplied = await _db.Holds.AnyAsync(h =>
            h.ClassId == classId &&
            h.StudentId == studentId &&
            (
                h.Status == HoldStatus.CONFIRMED ||
                (
                    h.Status == HoldStatus.HELD &&
                    h.ExpiresAt > databaseNow
                )
            ),
            cancellationToken
        );

        if (alreadyApplied)
        {
            await transaction.CommitAsync(cancellationToken);

            return new HoldCreationResult(
                HoldCreationStatus.DuplicateApplication
            );
        }


        // STEP 6: Count occupied seats.

        var heldSeats = await _db.Holds.CountAsync(h =>
            h.ClassId == classId &&
            h.Status == HoldStatus.HELD &&
            h.ExpiresAt > databaseNow,
            cancellationToken
        );

        var confirmedSeats = await _db.Holds.CountAsync(h =>
            h.ClassId == classId &&
            h.Status == HoldStatus.CONFIRMED,
            cancellationToken
        );


        // STEP 7: Check available seats.

        var occupiedSeats = heldSeats + confirmedSeats;

        if (occupiedSeats >= schoolClass.TotalSeats)
        {
            await transaction.CommitAsync(cancellationToken);

            return new HoldCreationResult(
                HoldCreationStatus.NoSeatsAvailable
            );
        }


        // STEP 8: Create the new hold.

        var hold = new AdmissionHold
        {
            ClassId = classId,
            StudentId = studentId,
            Status = HoldStatus.HELD,
            CreatedAt = databaseNow,
            ExpiresAt = databaseNow.AddSeconds(
                _holdSettings.DurationSeconds
            )
        };

        _db.Holds.Add(hold);

        await _db.SaveChangesAsync(cancellationToken);


        // STEP 9: Commit the transaction.
        //
        // This saves the hold and releases the class row lock.

        await transaction.CommitAsync(cancellationToken);


        // STEP 10: Return the API response.

        return new HoldCreationResult(
            HoldCreationStatus.Created,
            new CreateHoldResponse
            {
                HoldId = hold.Id,
                ClassId = hold.ClassId,
                StudentId = hold.StudentId,
                Status = hold.Status.ToString(),
                ExpiresAt = FormatUtc(hold.ExpiresAt)
            }
        );
    }


    // ==========================================
    // DATABASE CLOCK
    // ==========================================

    private Task<DateTimeOffset> GetDatabaseNowAsync(
        CancellationToken cancellationToken)
    {
        return _db.Database
            .SqlQuery<DateTimeOffset>(
                $"SELECT clock_timestamp() AS \"Value\""
            )
            .SingleAsync(cancellationToken);
    }


    // ==========================================
    // UTC TIME FORMATTING
    // ==========================================

    private static string FormatUtc(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString(
            "O",
            CultureInfo.InvariantCulture
        );
    }
}
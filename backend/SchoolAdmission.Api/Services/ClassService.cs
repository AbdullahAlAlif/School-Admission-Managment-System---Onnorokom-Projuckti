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
    // GET MY APPLICATIONS
    // ==========================================

    public async Task<IReadOnlyList<MyHoldResponse>> GetMyHoldsAsync(
        string studentId,
        CancellationToken cancellationToken)
    {
        // Obtain the current time from PostgreSQL.
        var databaseNow =
            await GetDatabaseNowAsync(cancellationToken);

        // Retrieve every application belonging to this student.
        var holds = await _db.Holds
            .AsNoTracking()
            .Where(h => h.StudentId == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new
            {
                h.Id,
                h.ClassId,
                ClassName = h.Class.Name,
                h.Status,
                h.ExpiresAt
            })
            .ToListAsync(cancellationToken);

        // Build the API response.
        return holds.Select(h =>
        {
            // Derive expiry at read time.
            var currentStatus =
                h.Status == HoldStatus.HELD &&
                h.ExpiresAt <= databaseNow
                    ? HoldStatus.EXPIRED
                    : h.Status;

            return new MyHoldResponse
            {
                HoldId = h.Id,
                ClassId = h.ClassId,
                ClassName = h.ClassName,
                Status = currentStatus.ToString(),
                ExpiresAt = FormatUtc(h.ExpiresAt)
            };
        }).ToList();
    }
    
    // ==========================================
    // GET HOLD STATUS
    // ==========================================

    public async Task<HoldResponse?> GetHoldAsync(
        string holdId,
        CancellationToken cancellationToken)
    {
        // Obtain the current time from PostgreSQL.
        var databaseNow =
            await GetDatabaseNowAsync(cancellationToken);

        var hold = await _db.Holds
            .AsNoTracking()
            .Where(h => h.Id == holdId)
            .Select(h => new
            {
                h.Id,
                h.ClassId,
                h.Status,
                h.ExpiresAt
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (hold is null)
        {
            return null;
        }

        // Derive expiry at read time.
        var currentStatus =
            hold.Status == HoldStatus.HELD &&
            hold.ExpiresAt <= databaseNow
                ? HoldStatus.EXPIRED
                : hold.Status;

        return new HoldResponse
        {
            HoldId = hold.Id,
            ClassId = hold.ClassId,
            Status = currentStatus.ToString(),
            ExpiresAt = FormatUtc(hold.ExpiresAt)
        };
    }


    // ==========================================
    // CONFIRM ADMISSION
    // ==========================================

    public async Task<ConfirmHoldResult> ConfirmHoldAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken)
    {
        // STEP 1: Begin transaction.
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken
            );

        // STEP 2: Find the class ID associated with this hold.
        //
        // ClassId is immutable, so we can read it before
        // acquiring the class row lock.

        var classId = await _db.Holds
            .AsNoTracking()
            .Where(h => h.Id == holdId)
            .Select(h => h.ClassId)
            .SingleOrDefaultAsync(cancellationToken);

        if (classId is null)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new ConfirmHoldResult(
                ConfirmHoldStatus.NotFound
            );
        }

        // STEP 3: Lock the class row.
        //
        // All operations affecting seat allocation for this
        // class follow the same class-first locking order.

        var lockedClasses = await _db.Classes
            .FromSqlInterpolated(
                $"SELECT * FROM classes WHERE id = {classId} FOR UPDATE"
            )
            .ToListAsync(cancellationToken);

        if (lockedClasses.Count == 0)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new ConfirmHoldResult(
                ConfirmHoldStatus.NotFound
            );
        }

        // STEP 4: Load the hold again inside the transaction.

        var hold = await _db.Holds
            .SingleOrDefaultAsync(
                h => h.Id == holdId,
                cancellationToken
            );

        if (hold is null)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new ConfirmHoldResult(
                ConfirmHoldStatus.NotFound
            );
        }

        // STEP 5: Verify ownership.

        if (hold.StudentId != studentId)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new ConfirmHoldResult(
                ConfirmHoldStatus.NotOwner
            );
        }

        // STEP 6: Check the database clock.

        var databaseNow =
            await GetDatabaseNowAsync(cancellationToken);

        // STEP 7: Detect an expired HELD application.

        if (hold.Status == HoldStatus.HELD &&
            hold.ExpiresAt <= databaseNow)
        {
            hold.Status = HoldStatus.EXPIRED;

            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new ConfirmHoldResult(
                ConfirmHoldStatus.Expired
            );
        }

        // STEP 8: Handle existing terminal states.

        if (hold.Status == HoldStatus.EXPIRED)
        {
            await transaction.CommitAsync(cancellationToken);

            return new ConfirmHoldResult(
                ConfirmHoldStatus.Expired
            );
        }

        if (hold.Status == HoldStatus.CANCELLED)
        {
            await transaction.CommitAsync(cancellationToken);

            return new ConfirmHoldResult(
                ConfirmHoldStatus.Cancelled
            );
        }

        // Repeated confirmation is successful.
        // We never increase a separate seat counter.
        if (hold.Status == HoldStatus.CONFIRMED)
        {
            await transaction.CommitAsync(cancellationToken);

            return new ConfirmHoldResult(
                ConfirmHoldStatus.Confirmed,
                new HoldStatusResponse
                {
                    HoldId = hold.Id,
                    Status = HoldStatus.CONFIRMED.ToString()
                }
            );
        }

        // STEP 9: Confirm the application.

        hold.Status = HoldStatus.CONFIRMED;

        await _db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new ConfirmHoldResult(
            ConfirmHoldStatus.Confirmed,
            new HoldStatusResponse
            {
                HoldId = hold.Id,
                Status = HoldStatus.CONFIRMED.ToString()
            }
        );
    }


        // ==========================================
        // CANCEL ADMISSION
        // ==========================================

        public async Task<CancelHoldStatus> CancelHoldAsync(
            string holdId,
            string studentId,
            CancellationToken cancellationToken)
        {
            // STEP 1: Begin transaction.

            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    cancellationToken
                );

            // STEP 2: Find the associated class.

            var classId = await _db.Holds
                .AsNoTracking()
                .Where(h => h.Id == holdId)
                .Select(h => h.ClassId)
                .SingleOrDefaultAsync(cancellationToken);

            if (classId is null)
            {
                await transaction.RollbackAsync(cancellationToken);

                return CancelHoldStatus.NotFound;
            }

            // STEP 3: Lock the class row.

            var lockedClasses = await _db.Classes
                .FromSqlInterpolated(
                    $"SELECT * FROM classes WHERE id = {classId} FOR UPDATE"
                )
                .ToListAsync(cancellationToken);

            if (lockedClasses.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);

                return CancelHoldStatus.NotFound;
            }

            // STEP 4: Load the application.

            var hold = await _db.Holds
                .SingleOrDefaultAsync(
                    h => h.Id == holdId,
                    cancellationToken
                );

            if (hold is null)
            {
                await transaction.RollbackAsync(cancellationToken);

                return CancelHoldStatus.NotFound;
            }

            // STEP 5: Verify ownership.

            if (hold.StudentId != studentId)
            {
                await transaction.RollbackAsync(cancellationToken);

                return CancelHoldStatus.NotOwner;
            }

            // STEP 6: Read PostgreSQL time.

            var databaseNow =
                await GetDatabaseNowAsync(cancellationToken);

            // STEP 7: Detect expiry.

            if (hold.Status == HoldStatus.HELD &&
                hold.ExpiresAt <= databaseNow)
            {
                hold.Status = HoldStatus.EXPIRED;

                await _db.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return CancelHoldStatus.Expired;
            }

            // STEP 8: Handle expired application.

            if (hold.Status == HoldStatus.EXPIRED)
            {
                await transaction.CommitAsync(cancellationToken);

                return CancelHoldStatus.Expired;
            }

            // STEP 9: Confirmed applications cannot be cancelled.

            if (hold.Status == HoldStatus.CONFIRMED)
            {
                await transaction.CommitAsync(cancellationToken);

                return CancelHoldStatus.Confirmed;
            }

            // STEP 10: Cancelling an already cancelled application
            // is allowed and returns success (idempotent).

            if (hold.Status == HoldStatus.CANCELLED)
            {
                await transaction.CommitAsync(cancellationToken);

                return CancelHoldStatus.Cancelled;
            }

            // STEP 11: Cancel the HELD application.

            hold.Status = HoldStatus.CANCELLED;

            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return CancelHoldStatus.Cancelled;
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
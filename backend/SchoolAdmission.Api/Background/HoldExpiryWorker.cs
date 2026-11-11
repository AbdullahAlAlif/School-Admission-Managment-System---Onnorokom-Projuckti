using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;

namespace SchoolAdmission.Api.Background;

public sealed class HoldExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HoldExpiryWorker> _logger;

    private static readonly TimeSpan Interval =
        TimeSpan.FromSeconds(2);


    public HoldExpiryWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<HoldExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }


    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Hold expiry worker started."
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireOverdueHoldsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "An error occurred while expiring overdue holds."
                );
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation(
            "Hold expiry worker stopped."
        );
    }


    private async Task ExpireOverdueHoldsAsync(
        CancellationToken cancellationToken)
    {
        // A background service is long-lived.
        // DbContext must be created within a scope,
        // not injected as a singleton.

        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<SchoolAdmissionDbContext>();


        // Lock eligible class rows before changing holds.
        //
        // SKIP LOCKED prevents us from waiting for a class
        // currently being processed by another transaction.
        //
        // The worker follows the same class-first lock order
        // as the application, confirmation and cancellation
        // operations.

        var sql = """
            WITH locked_classes AS MATERIALIZED
            (
                SELECT c.id
                FROM classes AS c
                WHERE EXISTS
                (
                    SELECT 1
                    FROM holds AS h
                    WHERE h.class_id = c.id
                      AND h.status = 'HELD'
                      AND h.expires_at <= clock_timestamp()
                )
                ORDER BY c.id
                FOR UPDATE OF c SKIP LOCKED
            )
            UPDATE holds AS h
            SET status = 'EXPIRED'
            FROM locked_classes AS lc
            WHERE h.class_id = lc.id
              AND h.status = 'HELD'
              AND h.expires_at <= clock_timestamp();
            """;


        var affectedRows = await db.Database
            .ExecuteSqlRawAsync(
                sql,
                cancellationToken
            );


        if (affectedRows > 0)
        {
            _logger.LogInformation(
                "Automatically expired {Count} admission holds.",
                affectedRows
            );
        }
    }
}
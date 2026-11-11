using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Models;
namespace SchoolAdmission.Api.Services;


public interface IClassService
{
    Task<CreateClassResponse> CreateAsync(
        string name,
        int totalSeats,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<ClassResponse>> GetAllAsync(
        CancellationToken cancellationToken
    );

    Task<ClassResponse?> GetByIdAsync(
        string classId,
        CancellationToken cancellationToken
    );

    Task<HoldCreationResult> CreateHoldAsync(
        string classId,
        string studentId,
        CancellationToken cancellationToken
    );

    Task<HoldResponse?> GetHoldAsync(
        string holdId,
        CancellationToken cancellationToken
    );

    Task<ConfirmHoldResult> ConfirmHoldAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken
    );

    Task<CancelHoldStatus> CancelHoldAsync(
        string holdId,
        string studentId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<MyHoldResponse>> GetMyHoldsAsync(
        string studentId,
        CancellationToken cancellationToken
    );
}
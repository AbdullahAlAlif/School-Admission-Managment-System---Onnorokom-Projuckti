using SchoolAdmission.Api.DTOs;

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
}
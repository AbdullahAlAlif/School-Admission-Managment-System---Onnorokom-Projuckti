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
}
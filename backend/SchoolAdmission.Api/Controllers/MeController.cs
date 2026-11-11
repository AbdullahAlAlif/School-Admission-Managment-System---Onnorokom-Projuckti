using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("me")]
public sealed class MeController : ControllerBase
{
    private readonly IClassService _classService;

    public MeController(IClassService classService)
    {
        _classService = classService;
    }


    // ==========================================
    // GET /me/holds
    // GET MY APPLICATIONS
    // ==========================================

    [HttpGet("holds")]
    public async Task<ActionResult<IReadOnlyList<MyHoldResponse>>> GetMyHolds(
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        // Validate the student's identity.
        var validStudentId =
            studentId is not null &&
            studentId.Length == 17 &&
            studentId.All(c => c >= '0' && c <= '9');

        if (!validStudentId)
        {
            return BadRequest(new ApiErrorResponse(
                new ApiErrorDetail(
                    "INVALID_STUDENT_ID",
                    "A valid 17-digit student birth registration number is required in the X-Student-Id header."
                )
            ));
        }

        // Retrieve the student's applications.
        var result = await _classService.GetMyHoldsAsync(
            studentId!,
            cancellationToken
        );

        return Ok(result);
    }
}
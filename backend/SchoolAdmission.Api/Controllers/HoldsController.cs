using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("holds")]
public sealed class HoldsController : ControllerBase
{
    private readonly IClassService _classService;

    public HoldsController(IClassService classService)
    {
        _classService = classService;
    }


    // ==========================================
    // GET /holds/{holdId}
    // CHECK APPLICATION STATUS
    // ==========================================

    [HttpGet("{holdId}")]
    public async Task<ActionResult<HoldResponse>> GetHold(
        string holdId,
        CancellationToken cancellationToken)
    {
        var result = await _classService.GetHoldAsync(
            holdId,
            cancellationToken
        );

        if (result is null)
        {
            return NotFound(Error(
                "HOLD_NOT_FOUND",
                "The requested application could not be found."
            ));
        }

        return Ok(result);
    }


    // ==========================================
    // POST /holds/{holdId}/confirm
    // CONFIRM ADMISSION
    // ==========================================

    [HttpPost("{holdId}/confirm")]
    public async Task<ActionResult<HoldStatusResponse>> ConfirmHold(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        if (!IsValidStudentId(studentId))
        {
            return BadRequest(Error(
                "INVALID_STUDENT_ID",
                "A valid 17-digit student birth registration number is required in the X-Student-Id header."
            ));
        }

        var result = await _classService.ConfirmHoldAsync(
            holdId,
            studentId!,
            cancellationToken
        );

        switch (result.Status)
        {
            case ConfirmHoldStatus.Confirmed:
                return Ok(result.Response);

            case ConfirmHoldStatus.NotFound:
                return NotFound(Error(
                    "HOLD_NOT_FOUND",
                    "The requested application could not be found."
                ));

            case ConfirmHoldStatus.NotOwner:
                return StatusCode(403, Error(
                    "HOLD_NOT_OWNED",
                    "You cannot confirm another student's application."
                ));

            case ConfirmHoldStatus.Expired:
                return Conflict(Error(
                    "HOLD_EXPIRED",
                    "This application has expired. Please apply again."
                ));

            case ConfirmHoldStatus.Cancelled:
                return Conflict(Error(
                    "HOLD_CANCELLED",
                    "This application has already been cancelled."
                ));

            default:
                return StatusCode(500, Error(
                    "INTERNAL_SERVER_ERROR",
                    "An unexpected error occurred."
                ));
        }
    }


    // ==========================================
    // DELETE /holds/{holdId}
    // CANCEL ADMISSION
    // ==========================================

    [HttpDelete("{holdId}")]
    public async Task<IActionResult> CancelHold(
        string holdId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        if (!IsValidStudentId(studentId))
        {
            return BadRequest(Error(
                "INVALID_STUDENT_ID",
                "A valid 17-digit student birth registration number is required in the X-Student-Id header."
            ));
        }

        var result = await _classService.CancelHoldAsync(
            holdId,
            studentId!,
            cancellationToken
        );

        switch (result)
        {
            case CancelHoldStatus.Cancelled:
                return NoContent();

            case CancelHoldStatus.NotFound:
                return NotFound(Error(
                    "HOLD_NOT_FOUND",
                    "The requested application could not be found."
                ));

            case CancelHoldStatus.NotOwner:
                return StatusCode(403, Error(
                    "HOLD_NOT_OWNED",
                    "You cannot cancel another student's application."
                ));

            case CancelHoldStatus.Expired:
                return Conflict(Error(
                    "HOLD_EXPIRED",
                    "This application has already expired."
                ));

            case CancelHoldStatus.Confirmed:
                return Conflict(Error(
                    "HOLD_ALREADY_CONFIRMED",
                    "A confirmed admission cannot be cancelled."
                ));

            default:
                return StatusCode(500, Error(
                    "INTERNAL_SERVER_ERROR",
                    "An unexpected error occurred."
                ));
        }
    }


    // ==========================================
    // VALIDATE STUDENT ID
    // ==========================================

    private static bool IsValidStudentId(string? studentId)
    {
        return studentId is not null
            && studentId.Length == 17
            && studentId.All(c => c >= '0' && c <= '9');
    }


    // ==========================================
    // STANDARD ERROR RESPONSE
    // ==========================================

    private static ApiErrorResponse Error(
        string code,
        string message)
    {
        return new ApiErrorResponse(
            new ApiErrorDetail(code, message)
        );
    }
}
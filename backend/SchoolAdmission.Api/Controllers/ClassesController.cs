using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Services;
using SchoolAdmission.Api.Models;
using System.Linq;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public sealed class ClassesController : ControllerBase
{
    private readonly IClassService _classService;

    public ClassesController(IClassService classService)
    {
        _classService = classService;
    }


    
    // POST /classes
    // CREATE A CLASS
  

    [HttpPost]
    public async Task<ActionResult<CreateClassResponse>> CreateClass(
        [FromBody] CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        // Validate class name.
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ApiErrorResponse(
                new ApiErrorDetail(
                    "INVALID_CLASS_DATA",
                    "Class name cannot be empty."
                )
            ));
        }

        var name = request.Name.Trim();

        if (name.Length > 100)
        {
            return BadRequest(new ApiErrorResponse(
                new ApiErrorDetail(
                    "INVALID_CLASS_DATA",
                    "Class name cannot exceed 100 characters."
                )
            ));
        }

        // Validate seat count.
        if (!request.TotalSeats.HasValue ||
            request.TotalSeats.Value < 1 ||
            request.TotalSeats.Value > 100000)
        {
            return BadRequest(new ApiErrorResponse(
                new ApiErrorDetail(
                    "INVALID_CLASS_DATA",
                    "Total seats must be between 1 and 100000."
                )
            ));
        }

        // Delegate database operations to the service.
        var result = await _classService.CreateAsync(
            name,
            request.TotalSeats.Value,
            cancellationToken
        );

        // Return HTTP 201 Created.
        return Created(
            $"/classes/{result.Id}",
            result
        );
    }


    
    // GET /classes
    // GET ALL CLASSES
    

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClassResponse>>> GetClasses(
        CancellationToken cancellationToken)
    {
        var result = await _classService.GetAllAsync(
            cancellationToken
        );

        return Ok(result);
    }
    

    // GET /classes/{classId}
    // GET ONE CLASS

    [HttpGet("{classId}")]
    public async Task<ActionResult<ClassResponse>> GetClass(
        string classId,
        CancellationToken cancellationToken)
    {
        var result = await _classService.GetByIdAsync(
            classId,
            cancellationToken
        );

        if (result is null)
        {
            return NotFound(Error(
                "CLASS_NOT_FOUND",
                "The requested class could not be found."
            ));
        }

        return Ok(result);
    }



    // POST /classes/{classId}/holds
    // APPLY FOR A SEAT

    [HttpPost("{classId}/holds")]
    public async Task<ActionResult<CreateHoldResponse>> CreateHold(
        string classId,
        [FromHeader(Name = "X-Student-Id")] string? studentId,
        CancellationToken cancellationToken)
    {
        // Validate the student's birth registration number.

        var validStudentId =
            studentId is not null &&
            studentId.Length == 17 &&
            studentId.All(c => c >= '0' && c <= '9');

        if (!validStudentId)
        {
            return BadRequest(Error(
                "INVALID_STUDENT_ID",
                "A valid 17-digit student birth registration number is required in the X-Student-Id header."
            ));
        }


        // Ask the service to create the hold.

        var result = await _classService.CreateHoldAsync(
            classId,
            studentId!,
            cancellationToken
        );


        // Translate business results into HTTP responses.

        if (result.Status == HoldCreationStatus.Created)
        {
            return Created(
                $"/holds/{result.Hold!.HoldId}",
                result.Hold
            );
        }

        if (result.Status == HoldCreationStatus.ClassNotFound)
        {
            return NotFound(Error(
                "CLASS_NOT_FOUND",
                "The requested class could not be found."
            ));
        }

        if (result.Status == HoldCreationStatus.DuplicateApplication)
        {
            return Conflict(Error(
                "ACTIVE_APPLICATION_EXISTS",
                "You already have an active application or confirmed admission for this class."
            ));
        }

        if (result.Status == HoldCreationStatus.NoSeatsAvailable)
        {
            return Conflict(Error(
                "NO_SEATS_AVAILABLE",
                "Sorry, no seats are currently available in this class."
            ));
        }

        return StatusCode(500, Error(
            "INTERNAL_SERVER_ERROR",
            "An unexpected error occurred."
        ));
    }


    
    // STANDARD ERROR RESPONSE
    private static ApiErrorResponse Error(
        string code,
        string message)
    {
        return new ApiErrorResponse(
            new ApiErrorDetail(code, message)
        );
    }
}
using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Services;

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


    // ==========================================
    // POST /classes
    // CREATE A CLASS
    // ==========================================

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


    // ==========================================
    // GET /classes
    // GET ALL CLASSES
    // ==========================================

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClassResponse>>> GetClasses(
        CancellationToken cancellationToken)
    {
        var result = await _classService.GetAllAsync(
            cancellationToken
        );

        return Ok(result);
    }
}
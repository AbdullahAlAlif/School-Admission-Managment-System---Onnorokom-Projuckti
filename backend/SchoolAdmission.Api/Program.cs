using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Configuration;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register API controllers.
builder.Services.AddControllers();

// Register application services.
builder.Services.AddScoped<IClassService, ClassService>();

// Keep model-binding errors in our standard API format.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = context.ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault()
            ?? "The request data is invalid.";

        return new BadRequestObjectResult(
            new ApiErrorResponse(
                new ApiErrorDetail("INVALID_REQUEST", message)
            )
        );
    };
});

// Enable OpenAPI documentation.
builder.Services.AddOpenApi();

// Allow the React frontend to access the backend.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Read the PostgreSQL connection string.
var connectionString =
    builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException(
        "Connection string 'Default' was not found.");

// Register Entity Framework Core with PostgreSQL.
builder.Services.AddDbContext<SchoolAdmissionDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure admission hold duration.
var holdDurationRaw =
    builder.Configuration["HOLD_DURATION_SECONDS"];

if (!int.TryParse(
        holdDurationRaw,
        NumberStyles.Integer,
        CultureInfo.InvariantCulture,
        out var holdDurationSeconds)
    || holdDurationSeconds <= 0)
{
    holdDurationSeconds = 600;

    Console.WriteLine(
        "WARNING: Invalid or missing HOLD_DURATION_SECONDS. " +
        "Using the default of 600 seconds."
    );
}

builder.Services.AddSingleton(
    new HoldSettings(holdDurationSeconds)
);

var app = builder.Build();

// Enable OpenAPI documentation during development.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Enable CORS.
app.UseCors("Frontend");

// Map controller endpoints.
app.MapControllers();

app.Run();
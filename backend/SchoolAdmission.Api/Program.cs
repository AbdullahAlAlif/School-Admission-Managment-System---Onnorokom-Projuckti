using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Register API controllers.
builder.Services.AddControllers();

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
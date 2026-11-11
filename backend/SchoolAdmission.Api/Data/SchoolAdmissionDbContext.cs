using Microsoft.EntityFrameworkCore;

namespace SchoolAdmission.Api.Data;

public class SchoolAdmissionDbContext : DbContext
{
    public SchoolAdmissionDbContext(
        DbContextOptions<SchoolAdmissionDbContext> options)
        : base(options)
    {
    }
}
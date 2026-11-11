using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Data;

public class SchoolAdmissionDbContext : DbContext
{
    public SchoolAdmissionDbContext(
        DbContextOptions<SchoolAdmissionDbContext> options)
        : base(options)
    {
    }

    // Database tables.
    public DbSet<SchoolClass> Classes => Set<SchoolClass>();

    public DbSet<AdmissionHold> Holds => Set<AdmissionHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==========================================
        // SCHOOL CLASSES TABLE
        // ==========================================

        modelBuilder.Entity<SchoolClass>(entity =>
        {
            entity.ToTable("classes", table =>
            {
                table.HasCheckConstraint(
                    "ck_classes_total_seats_range",
                    "\"total_seats\" BETWEEN 1 AND 100000");

                table.HasCheckConstraint(
                    "ck_classes_name_not_empty",
                    "length(btrim(\"name\")) > 0");
            });

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .HasColumnName("id")
                .HasMaxLength(14)
                .IsRequired();

            entity.Property(c => c.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(c => c.TotalSeats)
                .HasColumnName("total_seats")
                .IsRequired();

            entity.Property(c => c.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // One class has many holds.
            entity.HasMany(c => c.Holds)
                .WithOne(h => h.Class)
                .HasForeignKey(h => h.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // ==========================================
        // ADMISSION HOLDS TABLE
        // ==========================================

        modelBuilder.Entity<AdmissionHold>(entity =>
        {
            entity.ToTable("holds", table =>
            {
                table.HasCheckConstraint(
                    "ck_holds_student_id_format",
                    "\"student_id\" ~ '^[0-9]{17}$'");

                table.HasCheckConstraint(
                    "ck_holds_status_valid",
                    "\"status\" IN ('HELD', 'CONFIRMED', 'EXPIRED', 'CANCELLED')");
            });

            entity.HasKey(h => h.Id);

            entity.Property(h => h.Id)
                .HasColumnName("id")
                .HasMaxLength(14)
                .IsRequired();

            entity.Property(h => h.ClassId)
                .HasColumnName("class_id")
                .HasMaxLength(14)
                .IsRequired();

            entity.Property(h => h.StudentId)
                .HasColumnName("student_id")
                .HasMaxLength(17)
                .IsRequired();

            entity.Property(h => h.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(9)
                .IsRequired();

            entity.Property(h => h.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(h => h.ExpiresAt)
                .HasColumnName("expires_at")
                .IsRequired();

            // Index for finding applications by class.
            entity.HasIndex(h => h.ClassId)
                .HasDatabaseName("ix_holds_class_id");

            // Index for finding applications by student.
            entity.HasIndex(h => h.StudentId)
                .HasDatabaseName("ix_holds_student_id");

            // A student cannot have two occupying holds
            // for the same class.
            entity.HasIndex(h => new { h.ClassId, h.StudentId })
                .IsUnique()
                .HasDatabaseName("ux_holds_one_occupying_per_student_class")
                .HasFilter("\"status\" IN ('HELD', 'CONFIRMED')");
        });
    }
}
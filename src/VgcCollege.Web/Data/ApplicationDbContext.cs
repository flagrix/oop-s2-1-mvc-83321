using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Branch> Branches { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<StudentProfile> StudentProfiles { get; set; }
    public DbSet<FacultyProfile> FacultyProfiles { get; set; }
    public DbSet<FacultyCourseAssignment> FacultyCourseAssignments { get; set; }
    public DbSet<CourseEnrolment> CourseEnrolments { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    public DbSet<Assignment> Assignments { get; set; }
    public DbSet<AssignmentResult> AssignmentResults { get; set; }
    public DbSet<Exam> Exams { get; set; }
    public DbSet<ExamResult> ExamResults { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AssignmentResult>()
            .HasIndex(r => new { r.AssignmentId, r.StudentProfileId })
            .IsUnique();

        builder.Entity<ExamResult>()
            .HasIndex(r => new { r.ExamId, r.StudentProfileId })
            .IsUnique();

        builder.Entity<CourseEnrolment>()
            .HasIndex(e => new { e.StudentProfileId, e.CourseId })
            .IsUnique();

        builder.Entity<FacultyCourseAssignment>()
            .HasIndex(a => new { a.FacultyProfileId, a.CourseId })
            .IsUnique();
    }
}

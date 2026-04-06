using System.ComponentModel.DataAnnotations;

namespace VgcCollege.Web.Models;

public class StudentProfile
{
    public int Id { get; set; }

    [Required]
    public string IdentityUserId { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateOfBirth { get; set; }

    [Required, StringLength(20)]
    public string StudentNumber { get; set; } = string.Empty;

    public ICollection<CourseEnrolment> Enrolments { get; set; } = new List<CourseEnrolment>();
    public ICollection<AssignmentResult> AssignmentResults { get; set; } = new List<AssignmentResult>();
    public ICollection<ExamResult> ExamResults { get; set; } = new List<ExamResult>();
}

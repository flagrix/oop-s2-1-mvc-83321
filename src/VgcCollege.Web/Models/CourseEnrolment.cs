using System.ComponentModel.DataAnnotations;

namespace VgcCollege.Web.Models;

public enum EnrolmentStatus { Active, Withdrawn, Completed }

public class CourseEnrolment
{
    public int Id { get; set; }

    public int StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    [DataType(DataType.Date)]
    public DateTime EnrolDate { get; set; }

    public EnrolmentStatus Status { get; set; } = EnrolmentStatus.Active;

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}

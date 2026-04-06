using System.ComponentModel.DataAnnotations;

namespace VgcCollege.Web.Models;

public class AttendanceRecord
{
    public int Id { get; set; }

    public int CourseEnrolmentId { get; set; }
    public CourseEnrolment CourseEnrolment { get; set; } = null!;

    public int WeekNumber { get; set; }

    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    public bool Present { get; set; }
}

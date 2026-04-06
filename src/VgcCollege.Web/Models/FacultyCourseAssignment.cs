namespace VgcCollege.Web.Models;

public class FacultyCourseAssignment
{
    public int Id { get; set; }
    public int FacultyProfileId { get; set; }
    public FacultyProfile FacultyProfile { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
}

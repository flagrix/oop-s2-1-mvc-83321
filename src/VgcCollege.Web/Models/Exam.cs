using System.ComponentModel.DataAnnotations;

namespace VgcCollege.Web.Models;

public class Exam
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }
    public decimal MaxScore { get; set; }
    public bool ResultsReleased { get; set; } = false;
    public ICollection<ExamResult> Results { get; set; } = new List<ExamResult>();
}

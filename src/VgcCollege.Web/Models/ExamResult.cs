using System.ComponentModel.DataAnnotations;

namespace VgcCollege.Web.Models;

public class ExamResult
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;
    public int StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;
    public decimal Score { get; set; }
    [StringLength(5)]
    public string? Grade { get; set; }
}

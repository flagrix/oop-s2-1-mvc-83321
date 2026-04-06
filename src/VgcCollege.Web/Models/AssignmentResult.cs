using System.ComponentModel.DataAnnotations;

namespace VgcCollege.Web.Models;

public class AssignmentResult
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;
    public int StudentProfileId { get; set; }
    public StudentProfile StudentProfile { get; set; } = null!;
    public decimal Score { get; set; }
    [StringLength(500)]
    public string? Feedback { get; set; }
}

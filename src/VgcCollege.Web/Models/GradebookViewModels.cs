using VgcCollege.Web.Models;

namespace VgcCollege.Web.Models;

// ─── Assignment Result ViewModels ─────────────────────────────────────────────

public class AssignmentResultsViewModel
{
    public Assignment Assignment { get; set; } = null!;
    public List<AssignmentResultEntry> Entries { get; set; } = new();
}

public class AssignmentResultEntry
{
    public StudentProfile StudentProfile { get; set; } = null!;
    public int? ExistingResultId { get; set; }
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
}

public class AssignmentResultPost
{
    public int StudentProfileId { get; set; }
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
}

// ─── Exam Result ViewModels ───────────────────────────────────────────────────

public class ExamResultsViewModel
{
    public Exam Exam { get; set; } = null!;
    public List<ExamResultEntry> Entries { get; set; } = new();
}

public class ExamResultEntry
{
    public StudentProfile StudentProfile { get; set; } = null!;
    public int? ExistingResultId { get; set; }
    public decimal? Score { get; set; }
    public string? Grade { get; set; }
}

public class ExamResultPost
{
    public int StudentProfileId { get; set; }
    public decimal? Score { get; set; }
    public string? Grade { get; set; }
}

// ─── Student Gradebook ────────────────────────────────────────────────────────

public class StudentGradebookViewModel
{
    public StudentProfile StudentProfile { get; set; } = null!;
    public List<StudentGradebookItem> Items { get; set; } = new();

    public decimal? AverageScore => Items.Where(i => i.Score.HasValue && i.Assignment.MaxScore > 0)
        .Select(i => i.Score!.Value / i.Assignment.MaxScore * 100)
        .DefaultIfEmpty()
        .Average() is decimal avg && avg > 0 ? Math.Round(avg, 1) : null;
}

public class StudentGradebookItem
{
    public Assignment Assignment { get; set; } = null!;
    public decimal? Score { get; set; }
    public string? Feedback { get; set; }
    public decimal? Percentage => Score.HasValue && Assignment.MaxScore > 0
        ? Math.Round(Score.Value / Assignment.MaxScore * 100, 1)
        : null;
}

// ─── Student Exam Results ─────────────────────────────────────────────────────

public class StudentExamResultsViewModel
{
    public StudentProfile StudentProfile { get; set; } = null!;
    public List<StudentExamResultItem> Items { get; set; } = new();
}

public class StudentExamResultItem
{
    public Exam Exam { get; set; } = null!;
    public decimal? Score { get; set; }
    public string? Grade { get; set; }
    public decimal? Percentage => Score.HasValue && Exam.MaxScore > 0
        ? Math.Round(Score.Value / Exam.MaxScore * 100, 1)
        : null;
}

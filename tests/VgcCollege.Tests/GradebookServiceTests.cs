using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;

namespace VgcCollege.Tests;

public class GradebookServiceTests
{
    private ApplicationDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }

    // ─── Assignment score validation ──────────────────────────────────────────

    [Fact]
    public void AssignmentScore_WithinRange_IsValid()
    {
        var assignment = new Assignment { MaxScore = 100, Title = "Test", DueDate = DateTime.Today };
        var result = new AssignmentResult { Score = 72 };
        bool valid = result.Score >= 0 && result.Score <= assignment.MaxScore;
        Assert.True(valid);
    }

    [Fact]
    public void AssignmentScore_ExceedsMax_IsInvalid()
    {
        var assignment = new Assignment { MaxScore = 50, Title = "Test", DueDate = DateTime.Today };
        var result = new AssignmentResult { Score = 60 };
        bool valid = result.Score >= 0 && result.Score <= assignment.MaxScore;
        Assert.False(valid, "Score above MaxScore should be invalid");
    }

    [Fact]
    public void AssignmentScore_Zero_IsValid()
    {
        var assignment = new Assignment { MaxScore = 100, Title = "Test", DueDate = DateTime.Today };
        var result = new AssignmentResult { Score = 0 };
        bool valid = result.Score >= 0 && result.Score <= assignment.MaxScore;
        Assert.True(valid, "Zero score is a valid submission");
    }

    // ─── Percentage calculation ───────────────────────────────────────────────

    [Fact]
    public void Percentage_CalculatedCorrectly()
    {
        decimal score = 75;
        decimal maxScore = 100;
        decimal pct = Math.Round(score / maxScore * 100, 1);
        Assert.Equal(75.0m, pct);
    }

    [Fact]
    public void Percentage_PartialScore_CalculatedCorrectly()
    {
        decimal score = 37;
        decimal maxScore = 50;
        decimal pct = Math.Round(score / maxScore * 100, 1);
        Assert.Equal(74.0m, pct);
    }

    // ─── Exam visibility rules ────────────────────────────────────────────────

    [Fact]
    public async Task Student_CannotSeeExamResults_WhenNotReleased()
    {
        using var db = CreateDb("ExamHiddenTest");
        var branch = new Branch { Name = "Dublin", Address = "Addr" };
        db.Branches.Add(branch);
        var student = new StudentProfile { Name = "Alice", Email = "a@s.ie", StudentNumber = "S1", IdentityUserId = "u1" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();

        var course = new Course { Name = "CS", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        db.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id, EnrolDate = DateTime.Today });

        var exam = new Exam { Title = "Final", CourseId = course.Id, Date = DateTime.Today, MaxScore = 100, ResultsReleased = false };
        db.Exams.Add(exam);
        await db.SaveChangesAsync();

        db.ExamResults.Add(new ExamResult { ExamId = exam.Id, StudentProfileId = student.Id, Score = 88, Grade = "A" });
        await db.SaveChangesAsync();

        // Student query: only released exams
        var visibleExams = await db.Exams
            .Where(e => e.CourseId == course.Id && e.ResultsReleased)
            .ToListAsync();

        Assert.Empty(visibleExams);
    }

    [Fact]
    public async Task Student_CanSeeExamResults_WhenReleased()
    {
        using var db = CreateDb("ExamVisibleTest");
        var branch = new Branch { Name = "Cork", Address = "Addr" };
        db.Branches.Add(branch);
        var student = new StudentProfile { Name = "Bob", Email = "b@s.ie", StudentNumber = "S2", IdentityUserId = "u2" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();

        var course = new Course { Name = "BA", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        db.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id, EnrolDate = DateTime.Today });

        var exam = new Exam { Title = "Midterm", CourseId = course.Id, Date = DateTime.Today, MaxScore = 100, ResultsReleased = true };
        db.Exams.Add(exam);
        await db.SaveChangesAsync();

        db.ExamResults.Add(new ExamResult { ExamId = exam.Id, StudentProfileId = student.Id, Score = 72, Grade = "B" });
        await db.SaveChangesAsync();

        var visibleExams = await db.Exams
            .Where(e => e.CourseId == course.Id && e.ResultsReleased)
            .ToListAsync();

        Assert.Single(visibleExams);
    }

    [Fact]
    public async Task ToggleRelease_ChangesFlag()
    {
        using var db = CreateDb("ToggleReleaseTest");
        var branch = new Branch { Name = "Galway", Address = "Addr" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var course = new Course { Name = "Nursing", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var exam = new Exam { Title = "Sem1", CourseId = course.Id, Date = DateTime.Today, MaxScore = 100, ResultsReleased = false };
        db.Exams.Add(exam);
        await db.SaveChangesAsync();

        // Simulate toggle
        var loaded = await db.Exams.FindAsync(exam.Id);
        loaded!.ResultsReleased = !loaded.ResultsReleased;
        await db.SaveChangesAsync();

        var updated = await db.Exams.FindAsync(exam.Id);
        Assert.True(updated!.ResultsReleased);
    }

    // ─── Grade entry tests ────────────────────────────────────────────────────

    [Fact]
    public async Task ExamResult_CanBeCreatedWithGrade()
    {
        using var db = CreateDb("ExamGradeCreateTest");
        var branch = new Branch { Name = "Dublin", Address = "Addr" };
        db.Branches.Add(branch);
        var student = new StudentProfile { Name = "Carol", Email = "c@s.ie", StudentNumber = "S3", IdentityUserId = "u3" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();

        var course = new Course { Name = "Psychology", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var exam = new Exam { Title = "Final Exam", CourseId = course.Id, Date = DateTime.Today, MaxScore = 100, ResultsReleased = true };
        db.Exams.Add(exam);
        await db.SaveChangesAsync();

        db.ExamResults.Add(new ExamResult { ExamId = exam.Id, StudentProfileId = student.Id, Score = 90, Grade = "A+" });
        await db.SaveChangesAsync();

        var result = await db.ExamResults.FirstOrDefaultAsync(r => r.ExamId == exam.Id && r.StudentProfileId == student.Id);
        Assert.NotNull(result);
        Assert.Equal(90, result.Score);
        Assert.Equal("A+", result.Grade);
    }

    [Fact]
    public async Task AssignmentResult_CanBeUpdated()
    {
        using var db = CreateDb("AssignmentUpdateTest");
        var branch = new Branch { Name = "Cork", Address = "Addr" };
        db.Branches.Add(branch);
        var student = new StudentProfile { Name = "Dave", Email = "d@s.ie", StudentNumber = "S4", IdentityUserId = "u4" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();

        var course = new Course { Name = "Business", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var assignment = new Assignment { CourseId = course.Id, Title = "Essay", MaxScore = 100, DueDate = DateTime.Today };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        db.AssignmentResults.Add(new AssignmentResult { AssignmentId = assignment.Id, StudentProfileId = student.Id, Score = 55 });
        await db.SaveChangesAsync();

        // Update the result
        var existing = await db.AssignmentResults.FirstAsync(r => r.AssignmentId == assignment.Id && r.StudentProfileId == student.Id);
        existing.Score = 70;
        existing.Feedback = "Improved after resubmission";
        await db.SaveChangesAsync();

        var updated = await db.AssignmentResults.FirstAsync(r => r.AssignmentId == assignment.Id && r.StudentProfileId == student.Id);
        Assert.Equal(70, updated.Score);
        Assert.Equal("Improved after resubmission", updated.Feedback);
    }

    // ─── Faculty scope: only sees their courses' data ─────────────────────────

    [Fact]
    public async Task Faculty_OnlySeesAssignmentsForTheirCourses()
    {
        using var db = CreateDb("FacultyAssignmentScopeTest");
        var branch = new Branch { Name = "Dublin", Address = "Addr" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var course1 = new Course { Name = "CS", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        var course2 = new Course { Name = "BA", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.AddRange(course1, course2);

        var faculty = new FacultyProfile { Name = "Dr. Test", Email = "t@vgc.ie", IdentityUserId = "f99" };
        db.FacultyProfiles.Add(faculty);
        await db.SaveChangesAsync();

        // Faculty only assigned to course1
        db.FacultyCourseAssignments.Add(new FacultyCourseAssignment { FacultyProfileId = faculty.Id, CourseId = course1.Id });
        db.Assignments.AddRange(
            new Assignment { CourseId = course1.Id, Title = "A1", MaxScore = 100, DueDate = DateTime.Today },
            new Assignment { CourseId = course2.Id, Title = "A2", MaxScore = 100, DueDate = DateTime.Today }
        );
        await db.SaveChangesAsync();

        var allowedCourseIds = await db.FacultyCourseAssignments
            .Where(a => a.FacultyProfileId == faculty.Id)
            .Select(a => a.CourseId).ToListAsync();

        var visible = await db.Assignments
            .Where(a => allowedCourseIds.Contains(a.CourseId))
            .ToListAsync();

        Assert.Single(visible);
        Assert.Equal("A1", visible[0].Title);
    }
}

using System.ComponentModel.DataAnnotations;
using VgcCollege.Web.Models;

namespace VgcCollege.Tests;

/// <summary>
/// Tests for model validation annotations and business validation rules.
/// </summary>
public class ValidationTests
{
    private static IList<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    // ─── StudentProfile validation ────────────────────────────────────────────

    [Fact]
    public void StudentProfile_Valid_PassesValidation()
    {
        var s = new StudentProfile
        {
            Name = "Alice Murphy",
            Email = "alice@vgc.ie",
            Phone = "0851234567",
            Address = "5 Main St",
            StudentNumber = "VGC001",
            IdentityUserId = "uid1"
        };
        var results = Validate(s);
        Assert.Empty(results);
    }

    [Fact]
    public void StudentProfile_MissingName_FailsValidation()
    {
        var s = new StudentProfile
        {
            Name = "",
            Email = "alice@vgc.ie",
            StudentNumber = "VGC001",
            IdentityUserId = "uid1"
        };
        var results = Validate(s);
        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void StudentProfile_MissingEmail_FailsValidation()
    {
        var s = new StudentProfile
        {
            Name = "Alice",
            Email = "",
            StudentNumber = "VGC001",
            IdentityUserId = "uid1"
        };
        var results = Validate(s);
        Assert.Contains(results, r => r.MemberNames.Contains("Email"));
    }

    // ─── Assignment validation ────────────────────────────────────────────────

    [Fact]
    public void Assignment_Valid_PassesValidation()
    {
        var a = new Assignment
        {
            Title = "Project 1",
            CourseId = 1,
            MaxScore = 100,
            DueDate = DateTime.Today.AddDays(14)
        };
        var results = Validate(a);
        Assert.Empty(results);
    }

    [Fact]
    public void Assignment_EmptyTitle_FailsValidation()
    {
        var a = new Assignment
        {
            Title = "",
            CourseId = 1,
            MaxScore = 100,
            DueDate = DateTime.Today
        };
        var results = Validate(a);
        Assert.Contains(results, r => r.MemberNames.Contains("Title"));
    }

    // ─── Exam validation ──────────────────────────────────────────────────────

    [Fact]
    public void Exam_Valid_PassesValidation()
    {
        var e = new Exam
        {
            Title = "Final Exam",
            CourseId = 1,
            MaxScore = 100,
            Date = DateTime.Today,
            ResultsReleased = false
        };
        var results = Validate(e);
        Assert.Empty(results);
    }

    [Fact]
    public void Exam_EmptyTitle_FailsValidation()
    {
        var e = new Exam { Title = "", CourseId = 1, MaxScore = 100, Date = DateTime.Today };
        var results = Validate(e);
        Assert.Contains(results, r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void Exam_ResultsReleased_DefaultsFalse()
    {
        var e = new Exam { Title = "Midterm", CourseId = 1, MaxScore = 100, Date = DateTime.Today };
        Assert.False(e.ResultsReleased);
    }

    // ─── Branch validation ────────────────────────────────────────────────────

    [Fact]
    public void Branch_Valid_PassesValidation()
    {
        var b = new Branch { Name = "Dublin City Centre", Address = "12 O'Connell St" };
        var results = Validate(b);
        Assert.Empty(results);
    }

    [Fact]
    public void Branch_MissingName_FailsValidation()
    {
        var b = new Branch { Name = "", Address = "Some Street" };
        var results = Validate(b);
        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    // ─── Course validation ────────────────────────────────────────────────────

    [Fact]
    public void Course_Valid_PassesValidation()
    {
        var c = new Course
        {
            Name = "BSc Computer Science",
            BranchId = 1,
            StartDate = new DateTime(2024, 9, 1),
            EndDate = new DateTime(2027, 6, 30)
        };
        var results = Validate(c);
        Assert.Empty(results);
    }

    // ─── Score boundary tests ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 100, true)]
    [InlineData(50, 100, true)]
    [InlineData(100, 100, true)]
    [InlineData(-1, 100, false)]
    [InlineData(101, 100, false)]
    public void Score_BoundaryChecks(decimal score, decimal maxScore, bool expected)
    {
        bool valid = score >= 0 && score <= maxScore;
        Assert.Equal(expected, valid);
    }

    // ─── Grade string values ──────────────────────────────────────────────────

    [Theory]
    [InlineData("A+")]
    [InlineData("A")]
    [InlineData("B+")]
    [InlineData("B")]
    [InlineData("C")]
    [InlineData("F")]
    public void ExamResult_GradeValues_AreValidStrings(string grade)
    {
        var result = new ExamResult { Score = 70, Grade = grade };
        Assert.False(string.IsNullOrEmpty(result.Grade));
        Assert.True(result.Grade.Length <= 5);
    }

    [Fact]
public void FacultyProfile_MissingName_FailsValidation()
{
    var f = new FacultyProfile { Name = "", Email = "f@vgc.ie", IdentityUserId = "uid1" };
    var results = Validate(f);
    Assert.Contains(results, r => r.MemberNames.Contains("Name"));
}

[Fact]
public void FacultyProfile_MissingEmail_FailsValidation()
{
    var f = new FacultyProfile { Name = "Dr. Smith", Email = "", IdentityUserId = "uid1" };
    var results = Validate(f);
    Assert.Contains(results, r => r.MemberNames.Contains("Email"));
}

[Fact]
public void CourseEnrolment_DefaultStatus_IsActive()
{
    var e = new CourseEnrolment { StudentProfileId = 1, CourseId = 1, EnrolDate = DateTime.Today };
    Assert.Equal(EnrolmentStatus.Active, e.Status);
}

[Fact]
public void AttendanceRecord_Present_DefaultsFalse()
{
    var r = new AttendanceRecord { CourseEnrolmentId = 1, WeekNumber = 1, Date = DateTime.Today };
    Assert.False(r.Present);
}

[Theory]
[InlineData("", false)]
[InlineData(null, false)]
[InlineData("alice@vgc.ie", true)]
[InlineData("notanemail", true)] // format validation is handled by [EmailAddress]
public void StudentProfile_EmailPresence(string? email, bool shouldPass)
{
    var s = new StudentProfile
    {
        Name = "Alice",
        Email = email ?? "",
        StudentNumber = "VGC001",
        IdentityUserId = "uid1"
    };
    var results = Validate(s);
    bool hasEmailError = results.Any(r => r.MemberNames.Contains("Email"));
    Assert.Equal(!shouldPass, hasEmailError);
}
}

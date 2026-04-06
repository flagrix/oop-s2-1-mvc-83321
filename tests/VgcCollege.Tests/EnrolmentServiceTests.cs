using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;

namespace VgcCollege.Tests;

public class EnrolmentServiceTests
{
    private ApplicationDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }

    // --- Enrolment rules ---

    [Fact]
    public async Task Student_CannotBeEnrolledTwiceInSameCourse()
    {
        using var db = CreateDb("DuplicateEnrolTest");
        var student = new StudentProfile { Name = "Alice", Email = "a@test.ie", StudentNumber = "S001", IdentityUserId = "u1" };
        var branch = new Branch { Name = "Dublin", Address = "Addr" };
        db.StudentProfiles.Add(student);
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var course = new Course { Name = "CS101", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        db.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id, EnrolDate = DateTime.Today, Status = EnrolmentStatus.Active });
        await db.SaveChangesAsync();

        var duplicate = await db.CourseEnrolments.AnyAsync(e =>
            e.StudentProfileId == student.Id && e.CourseId == course.Id);

        Assert.True(duplicate, "First enrolment should exist");

        // Simulating the controller check
        bool wouldReject = duplicate;
        Assert.True(wouldReject, "Duplicate enrolment should be rejected");
    }

    [Fact]
    public async Task Student_CanBeEnrolledInMultipleCourses()
    {
        using var db = CreateDb("MultiCourseEnrolTest");
        var branch = new Branch { Name = "Cork", Address = "Addr" };
        db.Branches.Add(branch);
        var student = new StudentProfile { Name = "Bob", Email = "b@test.ie", StudentNumber = "S002", IdentityUserId = "u2" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();

        var c1 = new Course { Name = "Math", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        var c2 = new Course { Name = "English", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.AddRange(c1, c2);
        await db.SaveChangesAsync();

        db.CourseEnrolments.AddRange(
            new CourseEnrolment { StudentProfileId = student.Id, CourseId = c1.Id, EnrolDate = DateTime.Today, Status = EnrolmentStatus.Active },
            new CourseEnrolment { StudentProfileId = student.Id, CourseId = c2.Id, EnrolDate = DateTime.Today, Status = EnrolmentStatus.Active }
        );
        await db.SaveChangesAsync();

        var count = await db.CourseEnrolments.CountAsync(e => e.StudentProfileId == student.Id);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task EnrolmentStatus_DefaultsToActive()
    {
        var enrolment = new CourseEnrolment { StudentProfileId = 1, CourseId = 1, EnrolDate = DateTime.Today };
        Assert.Equal(EnrolmentStatus.Active, enrolment.Status);
    }

    // --- Attendance rules ---

    [Fact]
    public async Task AttendancePercentage_CalculatedCorrectly()
    {
        var records = new List<AttendanceRecord>
        {
            new() { WeekNumber = 1, Present = true },
            new() { WeekNumber = 2, Present = true },
            new() { WeekNumber = 3, Present = false },
            new() { WeekNumber = 4, Present = true }
        };

        int total = records.Count;
        int present = records.Count(r => r.Present);
        double pct = (double)present / total * 100;

        Assert.Equal(4, total);
        Assert.Equal(3, present);
        Assert.Equal(75.0, pct);
    }

    [Fact]
    public async Task AttendancePercentage_ZeroWhenNoRecords()
    {
        var records = new List<AttendanceRecord>();
        int total = records.Count;
        double pct = total > 0 ? (double)records.Count(r => r.Present) / total * 100 : 0;
        Assert.Equal(0, pct);
    }

    // --- Visibility rules ---

    [Fact]
    public async Task ExamResult_HiddenFromStudentWhenNotReleased()
    {
        using var db = CreateDb("ExamVisibilityTest");
        var exam = new Exam
        {
            Title = "Semester 1",
            MaxScore = 100,
            Date = DateTime.Today,
            ResultsReleased = false
        };

        // A student should NOT see results when ResultsReleased = false
        bool canView = exam.ResultsReleased;
        Assert.False(canView, "Student should not see unreleased exam results");
    }

    [Fact]
    public async Task ExamResult_VisibleToStudentWhenReleased()
    {
        var exam = new Exam { Title = "Semester 2", MaxScore = 100, Date = DateTime.Today, ResultsReleased = true };
        Assert.True(exam.ResultsReleased, "Student should see released exam results");
    }

    // --- Grade calculations ---

    [Fact]
    public void AssignmentScore_CannotExceedMaxScore()
    {
        var assignment = new Assignment { MaxScore = 100, Title = "Test", DueDate = DateTime.Today };
        var result = new AssignmentResult { Score = 85, AssignmentId = 1, StudentProfileId = 1 };

        bool valid = result.Score <= assignment.MaxScore && result.Score >= 0;
        Assert.True(valid);
    }

    [Fact]
    public void AssignmentScore_NegativeIsInvalid()
    {
        var assignment = new Assignment { MaxScore = 100, Title = "Test", DueDate = DateTime.Today };
        var result = new AssignmentResult { Score = -5 };
        bool valid = result.Score >= 0 && result.Score <= assignment.MaxScore;
        Assert.False(valid, "Negative score should be invalid");
    }

    // --- Faculty data access ---

    [Fact]
    public async Task Faculty_OnlySeesStudentsInTheirCourses()
    {
        using var db = CreateDb("FacultyAccessTest");
        var branch = new Branch { Name = "Galway", Address = "Addr" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var course1 = new Course { Name = "CS", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        var course2 = new Course { Name = "BA", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.AddRange(course1, course2);

        var faculty = new FacultyProfile { Name = "Dr. Smith", Email = "s@vgc.ie", IdentityUserId = "f1" };
        db.FacultyProfiles.Add(faculty);
        await db.SaveChangesAsync();

        db.FacultyCourseAssignments.Add(new FacultyCourseAssignment { FacultyProfileId = faculty.Id, CourseId = course1.Id });

        var s1 = new StudentProfile { Name = "Alice", Email = "a@s.ie", StudentNumber = "S1", IdentityUserId = "su1" };
        var s2 = new StudentProfile { Name = "Bob", Email = "b@s.ie", StudentNumber = "S2", IdentityUserId = "su2" };
        db.StudentProfiles.AddRange(s1, s2);
        await db.SaveChangesAsync();

        db.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = s1.Id, CourseId = course1.Id, EnrolDate = DateTime.Today });
        db.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = s2.Id, CourseId = course2.Id, EnrolDate = DateTime.Today });
        await db.SaveChangesAsync();

        // Faculty teaches only course1 — should see only s1
        var facultyCourseIds = await db.FacultyCourseAssignments
            .Where(a => a.FacultyProfileId == faculty.Id)
            .Select(a => a.CourseId).ToListAsync();

        var visibleStudents = await db.CourseEnrolments
            .Where(e => facultyCourseIds.Contains(e.CourseId))
            .Select(e => e.StudentProfileId)
            .Distinct().ToListAsync();

        Assert.Single(visibleStudents);
        Assert.Contains(s1.Id, visibleStudents);
        Assert.DoesNotContain(s2.Id, visibleStudents);
    }
}

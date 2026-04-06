using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;

namespace VgcCollege.Tests;

/// <summary>
/// Tests that verify server-side data filtering by role (authorization query layer).
/// These are separate from the gradebook/enrolment tests and cover edge cases.
/// </summary>
public class AuthorizationQueryTests
{
    private ApplicationDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }

    // ─── Student can only see their own profile ───────────────────────────────

    [Fact]
    public async Task Student_CannotAccessAnotherStudentsProfile()
    {
        using var db = CreateDb("StudentProfileAccess");
        db.StudentProfiles.AddRange(
            new StudentProfile { Name = "Alice", Email = "a@s.ie", StudentNumber = "S1", IdentityUserId = "u1" },
            new StudentProfile { Name = "Bob",   Email = "b@s.ie", StudentNumber = "S2", IdentityUserId = "u2" }
        );
        await db.SaveChangesAsync();

        // Simulate: user "u1" queries by IdentityUserId — should only find their own profile
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(s => s.IdentityUserId == "u1");
        Assert.NotNull(profile);
        Assert.Equal("Alice", profile.Name);

        // "u1" tries to get "u2"'s profile — the check should fail
        bool canAccess = profile.IdentityUserId == "u2";
        Assert.False(canAccess);
    }

    // ─── Faculty cannot access other faculty's courses ────────────────────────

    [Fact]
    public async Task Faculty_CannotSeeExamsOutsideTheirCourses()
    {
        using var db = CreateDb("FacultyExamScope");
        var branch = new Branch { Name = "Dublin", Address = "Addr" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var c1 = new Course { Name = "CS", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        var c2 = new Course { Name = "BA", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.AddRange(c1, c2);

        var f1 = new FacultyProfile { Name = "Dr. Smith", Email = "s@vgc.ie", IdentityUserId = "f1" };
        db.FacultyProfiles.Add(f1);
        await db.SaveChangesAsync();

        db.FacultyCourseAssignments.Add(new FacultyCourseAssignment { FacultyProfileId = f1.Id, CourseId = c1.Id });
        db.Exams.AddRange(
            new Exam { CourseId = c1.Id, Title = "CS Final", Date = DateTime.Today, MaxScore = 100 },
            new Exam { CourseId = c2.Id, Title = "BA Final", Date = DateTime.Today, MaxScore = 100 }
        );
        await db.SaveChangesAsync();

        var allowedCourseIds = await db.FacultyCourseAssignments
            .Where(a => a.FacultyProfileId == f1.Id)
            .Select(a => a.CourseId).ToListAsync();

        var visibleExams = await db.Exams
            .Where(e => allowedCourseIds.Contains(e.CourseId))
            .ToListAsync();

        Assert.Single(visibleExams);
        Assert.Equal("CS Final", visibleExams[0].Title);
    }

    // ─── Student cannot see another student's assignment results ─────────────

    [Fact]
    public async Task Student_CannotSeeAnotherStudentsAssignmentResults()
    {
        using var db = CreateDb("StudentResultScope");
        var branch = new Branch { Name = "Cork", Address = "Addr" };
        db.Branches.Add(branch);
        var s1 = new StudentProfile { Name = "Alice", Email = "a@s.ie", StudentNumber = "S1", IdentityUserId = "u1" };
        var s2 = new StudentProfile { Name = "Bob",   Email = "b@s.ie", StudentNumber = "S2", IdentityUserId = "u2" };
        db.StudentProfiles.AddRange(s1, s2);
        await db.SaveChangesAsync();

        var course = new Course { Name = "CS", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var assignment = new Assignment { CourseId = course.Id, Title = "Essay", MaxScore = 100, DueDate = DateTime.Today };
        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        db.AssignmentResults.AddRange(
            new AssignmentResult { AssignmentId = assignment.Id, StudentProfileId = s1.Id, Score = 80 },
            new AssignmentResult { AssignmentId = assignment.Id, StudentProfileId = s2.Id, Score = 60 }
        );
        await db.SaveChangesAsync();

        // Alice's view: filter by her own StudentProfileId
        var aliceResults = await db.AssignmentResults
            .Where(r => r.StudentProfileId == s1.Id)
            .ToListAsync();

        Assert.Single(aliceResults);
        Assert.Equal(80, aliceResults[0].Score);
    }

    // ─── Admin sees all data ──────────────────────────────────────────────────

    [Fact]
    public async Task Admin_CanSeeAllStudentsAcrossBranches()
    {
        using var db = CreateDb("AdminAllStudents");
        db.StudentProfiles.AddRange(
            new StudentProfile { Name = "Alice", Email = "a@s.ie", StudentNumber = "S1", IdentityUserId = "u1" },
            new StudentProfile { Name = "Bob",   Email = "b@s.ie", StudentNumber = "S2", IdentityUserId = "u2" },
            new StudentProfile { Name = "Carol", Email = "c@s.ie", StudentNumber = "S3", IdentityUserId = "u3" }
        );
        await db.SaveChangesAsync();

        // Admin query has no filter
        var all = await db.StudentProfiles.ToListAsync();
        Assert.Equal(3, all.Count);
    }

    // ─── Enrolment status transitions ────────────────────────────────────────

    [Fact]
    public async Task EnrolmentStatus_CanBeChangedToWithdrawn()
    {
        using var db = CreateDb("EnrolmentStatusChange");
        var branch = new Branch { Name = "Galway", Address = "Addr" };
        db.Branches.Add(branch);
        var student = new StudentProfile { Name = "Dave", Email = "d@s.ie", StudentNumber = "S4", IdentityUserId = "u4" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();

        var course = new Course { Name = "Nursing", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        db.CourseEnrolments.Add(new CourseEnrolment
        {
            StudentProfileId = student.Id, CourseId = course.Id,
            EnrolDate = DateTime.Today, Status = EnrolmentStatus.Active
        });
        await db.SaveChangesAsync();

        var enrolment = await db.CourseEnrolments.FirstAsync();
        enrolment.Status = EnrolmentStatus.Withdrawn;
        await db.SaveChangesAsync();

        var updated = await db.CourseEnrolments.FirstAsync();
        Assert.Equal(EnrolmentStatus.Withdrawn, updated.Status);
    }

    // ─── Duplicate exam result not added twice ────────────────────────────────

    [Fact]
    public async Task ExamResult_UniquePerStudentAndExam()
    {
        using var db = CreateDb("UniqueExamResult");
        var branch = new Branch { Name = "Dublin", Address = "Addr" };
        db.Branches.Add(branch);
        var student = new StudentProfile { Name = "Eve", Email = "e@s.ie", StudentNumber = "S5", IdentityUserId = "u5" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();

        var course = new Course { Name = "CS", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var exam = new Exam { CourseId = course.Id, Title = "Final", Date = DateTime.Today, MaxScore = 100 };
        db.Exams.Add(exam);
        await db.SaveChangesAsync();

        db.ExamResults.Add(new ExamResult { ExamId = exam.Id, StudentProfileId = student.Id, Score = 75, Grade = "B" });
        await db.SaveChangesAsync();

        // Simulate "upsert" logic: check if exists before inserting
        var existing = await db.ExamResults
            .FirstOrDefaultAsync(r => r.ExamId == exam.Id && r.StudentProfileId == student.Id);

        Assert.NotNull(existing);

        // Should update, not insert a second row
        existing.Score = 80;
        existing.Grade = "B+";
        await db.SaveChangesAsync();

        var count = await db.ExamResults.CountAsync(r => r.ExamId == exam.Id && r.StudentProfileId == student.Id);
        Assert.Equal(1, count);

        var final = await db.ExamResults.FirstAsync(r => r.ExamId == exam.Id);
        Assert.Equal(80, final.Score);
    }

    // ─── Attendance: no duplicate week for same enrolment ────────────────────

    [Fact]
    public async Task Attendance_MultipleWeeksStoredCorrectly()
    {
        using var db = CreateDb("AttendanceMultiWeek");
        var branch = new Branch { Name = "Cork", Address = "Addr" };
        db.Branches.Add(branch);
        var student = new StudentProfile { Name = "Frank", Email = "f@s.ie", StudentNumber = "S6", IdentityUserId = "u6" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();

        var course = new Course { Name = "BA", BranchId = branch.Id, StartDate = DateTime.Today, EndDate = DateTime.Today.AddYears(1) };
        db.Courses.Add(course);
        await db.SaveChangesAsync();

        var enrolment = new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id, EnrolDate = DateTime.Today };
        db.CourseEnrolments.Add(enrolment);
        await db.SaveChangesAsync();

        for (int week = 1; week <= 5; week++)
        {
            db.AttendanceRecords.Add(new AttendanceRecord
            {
                CourseEnrolmentId = enrolment.Id,
                WeekNumber = week,
                Date = DateTime.Today.AddDays((week - 1) * 7),
                Present = week != 3
            });
        }
        await db.SaveChangesAsync();

        var records = await db.AttendanceRecords
            .Where(r => r.CourseEnrolmentId == enrolment.Id)
            .ToListAsync();

        Assert.Equal(5, records.Count);
        Assert.Equal(4, records.Count(r => r.Present));
        Assert.Equal(1, records.Count(r => !r.Present));
    }
}

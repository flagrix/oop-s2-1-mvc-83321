using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;
using Xunit;

namespace VgcCollege.Tests;

// ─── Helpers ──────────────────────────────────────────────────────────────────

public static class DbContextFactory
{
    public static ApplicationDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }
}

// ─── Branch CRUD ──────────────────────────────────────────────────────────────

public class BranchDbTests
{
    [Fact]
    public async Task CanAdd_AndRetrieve_Branch()
    {
        using var ctx = DbContextFactory.Create(nameof(CanAdd_AndRetrieve_Branch));
        ctx.Branches.Add(new Branch { Name = "Science", Address = "123 St" });
        await ctx.SaveChangesAsync();
        Assert.Equal(1, await ctx.Branches.CountAsync());
    }

    [Fact]
    public async Task CanUpdate_Branch()
    {
        using var ctx = DbContextFactory.Create(nameof(CanUpdate_Branch));
        ctx.Branches.Add(new Branch { Name = "Old Name", Address = "123 St" });
        await ctx.SaveChangesAsync();

        var branch = await ctx.Branches.FirstAsync();
        branch.Name = "New Name";
        await ctx.SaveChangesAsync();

        var updated = await ctx.Branches.FirstAsync();
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task CanDelete_Branch()
    {
        using var ctx = DbContextFactory.Create(nameof(CanDelete_Branch));
        ctx.Branches.Add(new Branch { Name = "ToDelete", Address = "123 St" });
        await ctx.SaveChangesAsync();

        var branch = await ctx.Branches.FirstAsync();
        ctx.Branches.Remove(branch);
        await ctx.SaveChangesAsync();

        Assert.Equal(0, await ctx.Branches.CountAsync());
    }

    [Fact]
    public async Task CanAdd_MultipleBranches()
    {
        using var ctx = DbContextFactory.Create(nameof(CanAdd_MultipleBranches));
        ctx.Branches.AddRange(
            new Branch { Name = "Science", Address = "1 St" },
            new Branch { Name = "Arts", Address = "2 St" },
            new Branch { Name = "Engineering", Address = "3 St" }
        );
        await ctx.SaveChangesAsync();
        Assert.Equal(3, await ctx.Branches.CountAsync());
    }

    [Fact]
    public async Task CanQuery_Branch_ByName()
    {
        using var ctx = DbContextFactory.Create(nameof(CanQuery_Branch_ByName));
        ctx.Branches.AddRange(
            new Branch { Name = "Science", Address = "1 St" },
            new Branch { Name = "Arts", Address = "2 St" }
        );
        await ctx.SaveChangesAsync();

        var result = await ctx.Branches.FirstOrDefaultAsync(b => b.Name == "Arts");
        Assert.NotNull(result);
        Assert.Equal("2 St", result!.Address);
    }
}

// ─── Course CRUD ──────────────────────────────────────────────────────────────

public class CourseDbTests
{
    [Fact]
    public async Task CanAdd_AndRetrieve_Course()
    {
        using var ctx = DbContextFactory.Create(nameof(CanAdd_AndRetrieve_Course));
        var branch = new Branch { Name = "Science", Address = "123 St" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        ctx.Courses.Add(new Course { Name = "Math", BranchId = branch.Id });
        await ctx.SaveChangesAsync();

        Assert.Equal(1, await ctx.Courses.CountAsync());
    }

    [Fact]
    public async Task CanLoad_Course_WithBranch()
    {
        using var ctx = DbContextFactory.Create(nameof(CanLoad_Course_WithBranch));
        var branch = new Branch { Name = "Science", Address = "123 St" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        ctx.Courses.Add(new Course { Name = "Physics", BranchId = branch.Id });
        await ctx.SaveChangesAsync();

        var course = await ctx.Courses.Include(c => c.Branch).FirstAsync();
        Assert.NotNull(course.Branch);
        Assert.Equal("Science", course.Branch!.Name);
    }

    [Fact]
    public async Task CanUpdate_Course()
    {
        using var ctx = DbContextFactory.Create(nameof(CanUpdate_Course));
        var branch = new Branch { Name = "Science", Address = "123 St" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        ctx.Courses.Add(new Course { Name = "Old Course", BranchId = branch.Id });
        await ctx.SaveChangesAsync();

        var course = await ctx.Courses.FirstAsync();
        course.Name = "Updated Course";
        await ctx.SaveChangesAsync();

        Assert.Equal("Updated Course", (await ctx.Courses.FirstAsync()).Name);
    }

    [Fact]
    public async Task CanDelete_Course()
    {
        using var ctx = DbContextFactory.Create(nameof(CanDelete_Course));
        var branch = new Branch { Name = "Science", Address = "123 St" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        ctx.Courses.Add(new Course { Name = "ToDelete", BranchId = branch.Id });
        await ctx.SaveChangesAsync();

        var course = await ctx.Courses.FirstAsync();
        ctx.Courses.Remove(course);
        await ctx.SaveChangesAsync();

        Assert.Equal(0, await ctx.Courses.CountAsync());
    }

    [Fact]
    public async Task CanQuery_Courses_ByBranch()
    {
        using var ctx = DbContextFactory.Create(nameof(CanQuery_Courses_ByBranch));
        var b1 = new Branch { Name = "Science", Address = "1 St" };
        var b2 = new Branch { Name = "Arts", Address = "2 St" };
        ctx.Branches.AddRange(b1, b2);
        await ctx.SaveChangesAsync();

        ctx.Courses.AddRange(
            new Course { Name = "Math", BranchId = b1.Id },
            new Course { Name = "Physics", BranchId = b1.Id },
            new Course { Name = "History", BranchId = b2.Id }
        );
        await ctx.SaveChangesAsync();

        var scienceCourses = await ctx.Courses.Where(c => c.BranchId == b1.Id).ToListAsync();
        Assert.Equal(2, scienceCourses.Count);
    }
}

// ─── StudentProfile CRUD ──────────────────────────────────────────────────────

public class StudentProfileDbTests
{
    [Fact]
    public async Task CanAdd_AndRetrieve_StudentProfile()
    {
        using var ctx = DbContextFactory.Create(nameof(CanAdd_AndRetrieve_StudentProfile));
        ctx.StudentProfiles.Add(new StudentProfile
        {
            Name = "Alice", Email = "alice@college.ie",
            StudentNumber = "S001", IdentityUserId = "uid1"
        });
        await ctx.SaveChangesAsync();
        Assert.Equal(1, await ctx.StudentProfiles.CountAsync());
    }

    [Fact]
    public async Task CanUpdate_StudentProfile()
    {
        using var ctx = DbContextFactory.Create(nameof(CanUpdate_StudentProfile));
        ctx.StudentProfiles.Add(new StudentProfile
        {
            Name = "Alice", Email = "alice@college.ie",
            StudentNumber = "S001", IdentityUserId = "uid1"
        });
        await ctx.SaveChangesAsync();

        var student = await ctx.StudentProfiles.FirstAsync();
        student.Phone = "0851234567";
        await ctx.SaveChangesAsync();

        Assert.Equal("0851234567", (await ctx.StudentProfiles.FirstAsync()).Phone);
    }

    [Fact]
    public async Task CanDelete_StudentProfile()
    {
        using var ctx = DbContextFactory.Create(nameof(CanDelete_StudentProfile));
        ctx.StudentProfiles.Add(new StudentProfile
        {
            Name = "Bob", Email = "bob@college.ie",
            StudentNumber = "S002", IdentityUserId = "uid2"
        });
        await ctx.SaveChangesAsync();

        ctx.StudentProfiles.Remove(await ctx.StudentProfiles.FirstAsync());
        await ctx.SaveChangesAsync();

        Assert.Equal(0, await ctx.StudentProfiles.CountAsync());
    }

    [Fact]
    public async Task CanQuery_Student_ByStudentNumber()
    {
        using var ctx = DbContextFactory.Create(nameof(CanQuery_Student_ByStudentNumber));
        ctx.StudentProfiles.AddRange(
            new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S001", IdentityUserId = "u1" },
            new StudentProfile { Name = "Bob", Email = "b@b.com", StudentNumber = "S002", IdentityUserId = "u2" }
        );
        await ctx.SaveChangesAsync();

        var result = await ctx.StudentProfiles.FirstOrDefaultAsync(s => s.StudentNumber == "S002");
        Assert.NotNull(result);
        Assert.Equal("Bob", result!.Name);
    }
}

// ─── FacultyProfile CRUD ──────────────────────────────────────────────────────

public class FacultyProfileDbTests
{
    [Fact]
    public async Task CanAdd_AndRetrieve_FacultyProfile()
    {
        using var ctx = DbContextFactory.Create(nameof(CanAdd_AndRetrieve_FacultyProfile));
        ctx.FacultyProfiles.Add(new FacultyProfile
        {
            Name = "Dr. Smith", Email = "smith@college.ie", IdentityUserId = "fuid1"
        });
        await ctx.SaveChangesAsync();
        Assert.Equal(1, await ctx.FacultyProfiles.CountAsync());
    }

    [Fact]
    public async Task CanUpdate_FacultyProfile()
    {
        using var ctx = DbContextFactory.Create(nameof(CanUpdate_FacultyProfile));
        ctx.FacultyProfiles.Add(new FacultyProfile
        {
            Name = "Dr. Smith", Email = "smith@college.ie", IdentityUserId = "fuid1"
        });
        await ctx.SaveChangesAsync();

        var faculty = await ctx.FacultyProfiles.FirstAsync();
        faculty.Phone = "0861234567";
        await ctx.SaveChangesAsync();

        Assert.Equal("0861234567", (await ctx.FacultyProfiles.FirstAsync()).Phone);
    }

    [Fact]
    public async Task CanDelete_FacultyProfile()
    {
        using var ctx = DbContextFactory.Create(nameof(CanDelete_FacultyProfile));
        ctx.FacultyProfiles.Add(new FacultyProfile
        {
            Name = "Dr. X", Email = "x@college.ie", IdentityUserId = "fuid2"
        });
        await ctx.SaveChangesAsync();

        ctx.FacultyProfiles.Remove(await ctx.FacultyProfiles.FirstAsync());
        await ctx.SaveChangesAsync();

        Assert.Equal(0, await ctx.FacultyProfiles.CountAsync());
    }
}

// ─── CourseEnrolment CRUD + Unique Index ──────────────────────────────────────

public class CourseEnrolmentDbTests
{
    private async Task<(ApplicationDbContext ctx, StudentProfile student, Course course)> SeedAsync(string dbName)
    {
        var ctx = DbContextFactory.Create(dbName);
        var branch = new Branch { Name = "Science", Address = "1 St" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        var course = new Course { Name = "Math", BranchId = branch.Id };
        ctx.Courses.Add(course);
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        ctx.StudentProfiles.Add(student);
        await ctx.SaveChangesAsync();
        return (ctx, student, course);
    }

    [Fact]
    public async Task CanAdd_CourseEnrolment()
    {
        var (ctx, student, course) = await SeedAsync(nameof(CanAdd_CourseEnrolment));
        ctx.CourseEnrolments.Add(new CourseEnrolment
        {
            StudentProfileId = student.Id,
            CourseId = course.Id,
            EnrolDate = DateTime.Today
        });
        await ctx.SaveChangesAsync();
        Assert.Equal(1, await ctx.CourseEnrolments.CountAsync());
    }

    [Fact]
    public async Task CanLoad_Enrolment_WithStudentAndCourse()
    {
        var (ctx, student, course) = await SeedAsync(nameof(CanLoad_Enrolment_WithStudentAndCourse));
        ctx.CourseEnrolments.Add(new CourseEnrolment
        {
            StudentProfileId = student.Id,
            CourseId = course.Id
        });
        await ctx.SaveChangesAsync();

        var enrolment = await ctx.CourseEnrolments
            .Include(e => e.StudentProfile)
            .Include(e => e.Course)
            .FirstAsync();

        Assert.Equal("Alice", enrolment.StudentProfile.Name);
        Assert.Equal("Math", enrolment.Course.Name);
    }

    [Fact]
    public async Task CanUpdate_EnrolmentStatus()
    {
        var (ctx, student, course) = await SeedAsync(nameof(CanUpdate_EnrolmentStatus));
        ctx.CourseEnrolments.Add(new CourseEnrolment
        {
            StudentProfileId = student.Id,
            CourseId = course.Id
        });
        await ctx.SaveChangesAsync();

        var enrolment = await ctx.CourseEnrolments.FirstAsync();
        enrolment.Status = EnrolmentStatus.Completed;
        await ctx.SaveChangesAsync();

        Assert.Equal(EnrolmentStatus.Completed, (await ctx.CourseEnrolments.FirstAsync()).Status);
    }

    [Fact]
    public async Task Duplicate_Enrolment_ThrowsException()
    {
        var (ctx, student, course) = await SeedAsync(nameof(Duplicate_Enrolment_ThrowsException));
        ctx.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id });
        await ctx.SaveChangesAsync();

        ctx.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id });
        await Assert.ThrowsAsync<InvalidOperationException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task CanDelete_Enrolment()
    {
        var (ctx, student, course) = await SeedAsync(nameof(CanDelete_Enrolment));
        ctx.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id });
        await ctx.SaveChangesAsync();

        ctx.CourseEnrolments.Remove(await ctx.CourseEnrolments.FirstAsync());
        await ctx.SaveChangesAsync();

        Assert.Equal(0, await ctx.CourseEnrolments.CountAsync());
    }
}

// ─── AttendanceRecord CRUD ────────────────────────────────────────────────────

public class AttendanceRecordDbTests
{
    [Fact]
    public async Task CanAdd_AttendanceRecord()
    {
        using var ctx = DbContextFactory.Create(nameof(CanAdd_AttendanceRecord));

        var branch = new Branch { Name = "Science", Address = "1 St" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        var course = new Course { Name = "Math", BranchId = branch.Id };
        ctx.Courses.Add(course);
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        ctx.StudentProfiles.Add(student);
        await ctx.SaveChangesAsync();

        var enrolment = new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id };
        ctx.CourseEnrolments.Add(enrolment);
        await ctx.SaveChangesAsync();

        ctx.AttendanceRecords.Add(new AttendanceRecord
        {
            CourseEnrolmentId = enrolment.Id,
            WeekNumber = 1,
            Present = true,
            Date = DateTime.Today
        });
        await ctx.SaveChangesAsync();

        Assert.Equal(1, await ctx.AttendanceRecords.CountAsync());
    }

    [Fact]
    public async Task CanAdd_MultipleAttendanceRecords_ForSameEnrolment()
    {
        using var ctx = DbContextFactory.Create(nameof(CanAdd_MultipleAttendanceRecords_ForSameEnrolment));

        var branch = new Branch { Name = "Science", Address = "1 St" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        var course = new Course { Name = "Math", BranchId = branch.Id };
        ctx.Courses.Add(course);
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        ctx.StudentProfiles.Add(student);
        await ctx.SaveChangesAsync();

        var enrolment = new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id };
        ctx.CourseEnrolments.Add(enrolment);
        await ctx.SaveChangesAsync();

        for (int week = 1; week <= 5; week++)
        {
            ctx.AttendanceRecords.Add(new AttendanceRecord
            {
                CourseEnrolmentId = enrolment.Id,
                WeekNumber = week,
                Present = week % 2 == 0
            });
        }
        await ctx.SaveChangesAsync();

        Assert.Equal(5, await ctx.AttendanceRecords.CountAsync());
    }
}

// ─── Assignment CRUD ──────────────────────────────────────────────────────────

public class AssignmentDbTests
{
    private async Task<(ApplicationDbContext ctx, Course course)> SeedCourseAsync(string dbName)
    {
        var ctx = DbContextFactory.Create(dbName);
        var branch = new Branch { Name = "Science", Address = "1 St" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();
        var course = new Course { Name = "Math", BranchId = branch.Id };
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();
        return (ctx, course);
    }

    [Fact]
    public async Task CanAdd_Assignment()
    {
        var (ctx, course) = await SeedCourseAsync(nameof(CanAdd_Assignment));
        ctx.Assignments.Add(new Assignment { Title = "Essay 1", MaxScore = 100, CourseId = course.Id });
        await ctx.SaveChangesAsync();
        Assert.Equal(1, await ctx.Assignments.CountAsync());
    }

    [Fact]
    public async Task CanLoad_Assignment_WithCourse()
    {
        var (ctx, course) = await SeedCourseAsync(nameof(CanLoad_Assignment_WithCourse));
        ctx.Assignments.Add(new Assignment { Title = "Essay 1", MaxScore = 100, CourseId = course.Id });
        await ctx.SaveChangesAsync();

        var assignment = await ctx.Assignments.Include(a => a.Course).FirstAsync();
        Assert.Equal("Math", assignment.Course.Name);
    }

    [Fact]
    public async Task CanUpdate_Assignment()
    {
        var (ctx, course) = await SeedCourseAsync(nameof(CanUpdate_Assignment));
        ctx.Assignments.Add(new Assignment { Title = "Old Title", MaxScore = 50, CourseId = course.Id });
        await ctx.SaveChangesAsync();

        var assignment = await ctx.Assignments.FirstAsync();
        assignment.Title = "New Title";
        assignment.MaxScore = 100;
        await ctx.SaveChangesAsync();

        var updated = await ctx.Assignments.FirstAsync();
        Assert.Equal("New Title", updated.Title);
        Assert.Equal(100, updated.MaxScore);
    }

    [Fact]
    public async Task CanDelete_Assignment()
    {
        var (ctx, course) = await SeedCourseAsync(nameof(CanDelete_Assignment));
        ctx.Assignments.Add(new Assignment { Title = "ToDelete", MaxScore = 50, CourseId = course.Id });
        await ctx.SaveChangesAsync();

        ctx.Assignments.Remove(await ctx.Assignments.FirstAsync());
        await ctx.SaveChangesAsync();

        Assert.Equal(0, await ctx.Assignments.CountAsync());
    }
}

// ─── AssignmentResult CRUD + Unique Index ────────────────────────────────────

public class AssignmentResultDbTests
{
    private async Task<(ApplicationDbContext ctx, Assignment assignment, StudentProfile student)> SeedAsync(string dbName)
    {
        var ctx = DbContextFactory.Create(dbName);
        var branch = new Branch { Name = "Science", Address = "1 St" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        var course = new Course { Name = "Math", BranchId = branch.Id };
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        var assignment = new Assignment { Title = "Essay", MaxScore = 100, CourseId = course.Id };
        ctx.Assignments.Add(assignment);
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        ctx.StudentProfiles.Add(student);
        await ctx.SaveChangesAsync();
        return (ctx, assignment, student);
    }

    [Fact]
    public async Task CanAdd_AssignmentResult()
    {
        var (ctx, assignment, student) = await SeedAsync(nameof(CanAdd_AssignmentResult));
        ctx.AssignmentResults.Add(new AssignmentResult
        {
            AssignmentId = assignment.Id,
            StudentProfileId = student.Id,
            Score = 85,
            Feedback = "Good work"
        });
        await ctx.SaveChangesAsync();
        Assert.Equal(1, await ctx.AssignmentResults.CountAsync());
    }

    [Fact]
    public async Task CanLoad_AssignmentResult_WithStudentAndAssignment()
    {
        var (ctx, assignment, student) = await SeedAsync(nameof(CanLoad_AssignmentResult_WithStudentAndAssignment));
        ctx.AssignmentResults.Add(new AssignmentResult
        {
            AssignmentId = assignment.Id,
            StudentProfileId = student.Id,
            Score = 90
        });
        await ctx.SaveChangesAsync();

        var result = await ctx.AssignmentResults
            .Include(r => r.Assignment)
            .Include(r => r.StudentProfile)
            .FirstAsync();

        Assert.Equal("Essay", result.Assignment.Title);
        Assert.Equal("Alice", result.StudentProfile.Name);
    }

    [Fact]
    public async Task Duplicate_AssignmentResult_ThrowsException()
    {
        var (ctx, assignment, student) = await SeedAsync(nameof(Duplicate_AssignmentResult_ThrowsException));
        ctx.AssignmentResults.Add(new AssignmentResult { AssignmentId = assignment.Id, StudentProfileId = student.Id, Score = 80 });
        await ctx.SaveChangesAsync();

        ctx.AssignmentResults.Add(new AssignmentResult { AssignmentId = assignment.Id, StudentProfileId = student.Id, Score = 90 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task CanUpdate_AssignmentResult()
    {
        var (ctx, assignment, student) = await SeedAsync(nameof(CanUpdate_AssignmentResult));
        ctx.AssignmentResults.Add(new AssignmentResult { AssignmentId = assignment.Id, StudentProfileId = student.Id, Score = 70 });
        await ctx.SaveChangesAsync();

        var result = await ctx.AssignmentResults.FirstAsync();
        result.Score = 95;
        result.Feedback = "Excellent!";
        await ctx.SaveChangesAsync();

        var updated = await ctx.AssignmentResults.FirstAsync();
        Assert.Equal(95, updated.Score);
        Assert.Equal("Excellent!", updated.Feedback);
    }
}

// ─── Exam CRUD ────────────────────────────────────────────────────────────────

public class ExamDbTests
{
    private async Task<(ApplicationDbContext ctx, Course course)> SeedCourseAsync(string dbName)
    {
        var ctx = DbContextFactory.Create(dbName);
        var branch = new Branch { Name = "Science", Address = "1 St" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();
        var course = new Course { Name = "Math", BranchId = branch.Id };
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();
        return (ctx, course);
    }

    [Fact]
    public async Task CanAdd_Exam()
    {
        var (ctx, course) = await SeedCourseAsync(nameof(CanAdd_Exam));
        ctx.Exams.Add(new Exam { Title = "Final Exam", MaxScore = 200, CourseId = course.Id });
        await ctx.SaveChangesAsync();
        Assert.Equal(1, await ctx.Exams.CountAsync());
    }

    [Fact]
    public async Task CanLoad_Exam_WithCourse()
    {
        var (ctx, course) = await SeedCourseAsync(nameof(CanLoad_Exam_WithCourse));
        ctx.Exams.Add(new Exam { Title = "Midterm", MaxScore = 100, CourseId = course.Id });
        await ctx.SaveChangesAsync();

        var exam = await ctx.Exams.Include(e => e.Course).FirstAsync();
        Assert.Equal("Math", exam.Course.Name);
    }

    [Fact]
    public async Task CanUpdate_Exam_ResultsReleased()
    {
        var (ctx, course) = await SeedCourseAsync(nameof(CanUpdate_Exam_ResultsReleased));
        ctx.Exams.Add(new Exam { Title = "Final", MaxScore = 100, CourseId = course.Id, ResultsReleased = false });
        await ctx.SaveChangesAsync();

        var exam = await ctx.Exams.FirstAsync();
        exam.ResultsReleased = true;
        await ctx.SaveChangesAsync();

        Assert.True((await ctx.Exams.FirstAsync()).ResultsReleased);
    }

    [Fact]
    public async Task CanDelete_Exam()
    {
        var (ctx, course) = await SeedCourseAsync(nameof(CanDelete_Exam));
        ctx.Exams.Add(new Exam { Title = "ToDelete", MaxScore = 100, CourseId = course.Id });
        await ctx.SaveChangesAsync();

        ctx.Exams.Remove(await ctx.Exams.FirstAsync());
        await ctx.SaveChangesAsync();

        Assert.Equal(0, await ctx.Exams.CountAsync());
    }
}

// ─── ExamResult CRUD + Unique Index ──────────────────────────────────────────

public class ExamResultDbTests
{
    private async Task<(ApplicationDbContext ctx, Exam exam, StudentProfile student)> SeedAsync(string dbName)
    {
        var ctx = DbContextFactory.Create(dbName);
        var branch = new Branch { Name = "Science", Address = "1 St" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        var course = new Course { Name = "Math", BranchId = branch.Id };
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        var exam = new Exam { Title = "Final", MaxScore = 200, CourseId = course.Id };
        ctx.Exams.Add(exam);
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        ctx.StudentProfiles.Add(student);
        await ctx.SaveChangesAsync();
        return (ctx, exam, student);
    }

    [Fact]
    public async Task CanAdd_ExamResult()
    {
        var (ctx, exam, student) = await SeedAsync(nameof(CanAdd_ExamResult));
        ctx.ExamResults.Add(new ExamResult
        {
            ExamId = exam.Id,
            StudentProfileId = student.Id,
            Score = 180,
            Grade = "A"
        });
        await ctx.SaveChangesAsync();
        Assert.Equal(1, await ctx.ExamResults.CountAsync());
    }

    [Fact]
    public async Task CanLoad_ExamResult_WithStudentAndExam()
    {
        var (ctx, exam, student) = await SeedAsync(nameof(CanLoad_ExamResult_WithStudentAndExam));
        ctx.ExamResults.Add(new ExamResult { ExamId = exam.Id, StudentProfileId = student.Id, Score = 160 });
        await ctx.SaveChangesAsync();

        var result = await ctx.ExamResults
            .Include(r => r.Exam)
            .Include(r => r.StudentProfile)
            .FirstAsync();

        Assert.Equal("Final", result.Exam.Title);
        Assert.Equal("Alice", result.StudentProfile.Name);
    }

    [Fact]
    public async Task Duplicate_ExamResult_ThrowsException()
    {
        var (ctx, exam, student) = await SeedAsync(nameof(Duplicate_ExamResult_ThrowsException));
        ctx.ExamResults.Add(new ExamResult { ExamId = exam.Id, StudentProfileId = student.Id, Score = 150 });
        await ctx.SaveChangesAsync();

        ctx.ExamResults.Add(new ExamResult { ExamId = exam.Id, StudentProfileId = student.Id, Score = 170 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task CanUpdate_ExamResult()
    {
        var (ctx, exam, student) = await SeedAsync(nameof(CanUpdate_ExamResult));
        ctx.ExamResults.Add(new ExamResult { ExamId = exam.Id, StudentProfileId = student.Id, Score = 140, Grade = "B" });
        await ctx.SaveChangesAsync();

        var result = await ctx.ExamResults.FirstAsync();
        result.Score = 190;
        result.Grade = "A";
        await ctx.SaveChangesAsync();

        var updated = await ctx.ExamResults.FirstAsync();
        Assert.Equal(190, updated.Score);
        Assert.Equal("A", updated.Grade);
    }
}

// ─── FacultyCourseAssignment CRUD + Unique Index ──────────────────────────────

public class FacultyCourseAssignmentDbTests
{
    private async Task<(ApplicationDbContext ctx, FacultyProfile faculty, Course course)> SeedAsync(string dbName)
    {
        var ctx = DbContextFactory.Create(dbName);
        var branch = new Branch { Name = "Science", Address = "1 St" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        var course = new Course { Name = "Math", BranchId = branch.Id };
        ctx.Courses.Add(course);
        var faculty = new FacultyProfile { Name = "Dr. Smith", Email = "smith@college.ie", IdentityUserId = "fuid1" };
        ctx.FacultyProfiles.Add(faculty);
        await ctx.SaveChangesAsync();
        return (ctx, faculty, course);
    }

    [Fact]
    public async Task CanAdd_FacultyCourseAssignment()
    {
        var (ctx, faculty, course) = await SeedAsync(nameof(CanAdd_FacultyCourseAssignment));
        ctx.FacultyCourseAssignments.Add(new FacultyCourseAssignment
        {
            FacultyProfileId = faculty.Id,
            CourseId = course.Id
        });
        await ctx.SaveChangesAsync();
        Assert.Equal(1, await ctx.FacultyCourseAssignments.CountAsync());
    }

    [Fact]
    public async Task CanLoad_FacultyCourseAssignment_WithFacultyAndCourse()
    {
        var (ctx, faculty, course) = await SeedAsync(nameof(CanLoad_FacultyCourseAssignment_WithFacultyAndCourse));
        ctx.FacultyCourseAssignments.Add(new FacultyCourseAssignment
        {
            FacultyProfileId = faculty.Id,
            CourseId = course.Id
        });
        await ctx.SaveChangesAsync();

        var assignment = await ctx.FacultyCourseAssignments
            .Include(a => a.FacultyProfile)
            .Include(a => a.Course)
            .FirstAsync();

        Assert.Equal("Dr. Smith", assignment.FacultyProfile.Name);
        Assert.Equal("Math", assignment.Course.Name);
    }

    [Fact]
    public async Task Duplicate_FacultyCourseAssignment_ThrowsException()
    {
        var (ctx, faculty, course) = await SeedAsync(nameof(Duplicate_FacultyCourseAssignment_ThrowsException));
        ctx.FacultyCourseAssignments.Add(new FacultyCourseAssignment { FacultyProfileId = faculty.Id, CourseId = course.Id });
        await ctx.SaveChangesAsync();

        ctx.FacultyCourseAssignments.Add(new FacultyCourseAssignment { FacultyProfileId = faculty.Id, CourseId = course.Id });
        await Assert.ThrowsAsync<InvalidOperationException>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task CanDelete_FacultyCourseAssignment()
    {
        var (ctx, faculty, course) = await SeedAsync(nameof(CanDelete_FacultyCourseAssignment));
        ctx.FacultyCourseAssignments.Add(new FacultyCourseAssignment { FacultyProfileId = faculty.Id, CourseId = course.Id });
        await ctx.SaveChangesAsync();

        ctx.FacultyCourseAssignments.Remove(await ctx.FacultyCourseAssignments.FirstAsync());
        await ctx.SaveChangesAsync();

        Assert.Equal(0, await ctx.FacultyCourseAssignments.CountAsync());
    }

    [Fact]
    public async Task SameFaculty_CanTeach_MultipleCourses()
    {
        var (ctx, faculty, course1) = await SeedAsync(nameof(SameFaculty_CanTeach_MultipleCourses));
        var branch = await ctx.Branches.FirstAsync();
        var course2 = new Course { Name = "Physics", BranchId = branch.Id };
        ctx.Courses.Add(course2);
        await ctx.SaveChangesAsync();

        ctx.FacultyCourseAssignments.AddRange(
            new FacultyCourseAssignment { FacultyProfileId = faculty.Id, CourseId = course1.Id },
            new FacultyCourseAssignment { FacultyProfileId = faculty.Id, CourseId = course2.Id }
        );
        await ctx.SaveChangesAsync();

        Assert.Equal(2, await ctx.FacultyCourseAssignments.CountAsync());
    }
}

// ─── ApplicationDbContext Configuration Tests ─────────────────────────────────

public class ApplicationDbContextTests
{
    [Fact]
    public void DbContext_CanBeCreated_WithInMemoryOptions()
    {
        using var ctx = DbContextFactory.Create(nameof(DbContext_CanBeCreated_WithInMemoryOptions));
        Assert.NotNull(ctx);
    }

    [Fact]
    public void DbContext_AllDbSets_AreNotNull()
    {
        using var ctx = DbContextFactory.Create(nameof(DbContext_AllDbSets_AreNotNull));
        Assert.NotNull(ctx.Branches);
        Assert.NotNull(ctx.Courses);
        Assert.NotNull(ctx.StudentProfiles);
        Assert.NotNull(ctx.FacultyProfiles);
        Assert.NotNull(ctx.FacultyCourseAssignments);
        Assert.NotNull(ctx.CourseEnrolments);
        Assert.NotNull(ctx.AttendanceRecords);
        Assert.NotNull(ctx.Assignments);
        Assert.NotNull(ctx.AssignmentResults);
        Assert.NotNull(ctx.Exams);
        Assert.NotNull(ctx.ExamResults);
    }

    [Fact]
    public async Task DbContext_EmptyDatabase_HasNoCourses()
    {
        using var ctx = DbContextFactory.Create(nameof(DbContext_EmptyDatabase_HasNoCourses));
        Assert.Equal(0, await ctx.Courses.CountAsync());
    }

    [Fact]
    public async Task DbContext_CanSaveAndRetrieve_CompleteScenario()
    {
        using var ctx = DbContextFactory.Create(nameof(DbContext_CanSaveAndRetrieve_CompleteScenario));

        // Setup branch + course
        var branch = new Branch { Name = "Science", Address = "1 St" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        var course = new Course { Name = "Math", BranchId = branch.Id };
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        // Setup student + faculty
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        var faculty = new FacultyProfile { Name = "Dr. Smith", Email = "smith@college.ie", IdentityUserId = "f1" };
        ctx.StudentProfiles.Add(student);
        ctx.FacultyProfiles.Add(faculty);
        await ctx.SaveChangesAsync();

        // Enrol student, assign faculty
        ctx.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id });
        ctx.FacultyCourseAssignments.Add(new FacultyCourseAssignment { FacultyProfileId = faculty.Id, CourseId = course.Id });
        await ctx.SaveChangesAsync();

        // Add assignment and result
        var assignment = new Assignment { Title = "Essay", MaxScore = 100, CourseId = course.Id };
        ctx.Assignments.Add(assignment);
        await ctx.SaveChangesAsync();

        ctx.AssignmentResults.Add(new AssignmentResult { AssignmentId = assignment.Id, StudentProfileId = student.Id, Score = 88 });
        await ctx.SaveChangesAsync();

        // Add exam and result
        var exam = new Exam { Title = "Final", MaxScore = 200, CourseId = course.Id };
        ctx.Exams.Add(exam);
        await ctx.SaveChangesAsync();

        ctx.ExamResults.Add(new ExamResult { ExamId = exam.Id, StudentProfileId = student.Id, Score = 175, Grade = "A" });
        await ctx.SaveChangesAsync();

        // Assertions
        Assert.Equal(1, await ctx.Branches.CountAsync());
        Assert.Equal(1, await ctx.Courses.CountAsync());
        Assert.Equal(1, await ctx.StudentProfiles.CountAsync());
        Assert.Equal(1, await ctx.FacultyProfiles.CountAsync());
        Assert.Equal(1, await ctx.CourseEnrolments.CountAsync());
        Assert.Equal(1, await ctx.FacultyCourseAssignments.CountAsync());
        Assert.Equal(1, await ctx.Assignments.CountAsync());
        Assert.Equal(1, await ctx.AssignmentResults.CountAsync());
        Assert.Equal(1, await ctx.Exams.CountAsync());
        Assert.Equal(1, await ctx.ExamResults.CountAsync());
    }
}

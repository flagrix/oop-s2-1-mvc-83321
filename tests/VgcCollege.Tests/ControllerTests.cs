using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using VgcCollege.Web.Controllers;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;
using Xunit;

namespace VgcCollege.Tests;

// ─── Helpers ─────────────────────────────────────────────────────────────────

public static class TestHelpers
{
    public static ApplicationDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    public static Mock<UserManager<IdentityUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        var mgr = new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        return mgr;
    }

    public static ControllerContext CreateControllerContext(string userId, params string[] roles)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }
}

// ─── HomeController Tests ─────────────────────────────────────────────────────

public class HomeControllerTests
{
    private readonly HomeController _controller = new HomeController();

    [Fact]
    public void Index_ReturnsViewResult()
    {
        var result = _controller.Index();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        var result = _controller.Privacy();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void AccessDenied_ReturnsViewResult()
    {
        var result = _controller.AccessDenied();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void NotFoundPage_ReturnsViewResult()
    {
        var result = _controller.NotFoundPage();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Error_ReturnsViewResult()
    {
        _controller.ControllerContext = TestHelpers.CreateControllerContext("user1");
        var result = _controller.Error();
        Assert.IsType<ViewResult>(result);
    }
}

// ─── AdminController Tests ────────────────────────────────────────────────────

public class AdminControllerTests
{
    private ApplicationDbContext CreateContext() =>
        TestHelpers.CreateInMemoryContext("AdminDb_" + Guid.NewGuid());

    [Fact]
    public void Index_ReturnsView()
    {
        using var ctx = CreateContext();
        var controller = new AdminController(ctx);
        var result = controller.Index();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Branches_ReturnsViewWithBranches()
    {
        using var ctx = CreateContext();
        ctx.Branches.Add(new Branch { Name = "Science" });
        ctx.Branches.Add(new Branch { Name = "Arts" });
        await ctx.SaveChangesAsync();

        var controller = new AdminController(ctx);
        var result = await controller.Branches();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Branch>>(view.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public void CreateBranch_Get_ReturnsView()
    {
        using var ctx = CreateContext();
        var controller = new AdminController(ctx);
        var result = controller.CreateBranch();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task CreateBranch_Post_ValidModel_RedirectsToBranches()
    {
        using var ctx = CreateContext();
        var controller = new AdminController(ctx);
        var branch = new Branch { Name = "Engineering" };

        var result = await controller.CreateBranch(branch);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Branches", redirect.ActionName);
        Assert.Equal(1, ctx.Branches.Count());
    }

    [Fact]
    public async Task CreateBranch_Post_InvalidModel_ReturnsView()
    {
        using var ctx = CreateContext();
        var controller = new AdminController(ctx);
        controller.ModelState.AddModelError("Name", "Required");

        var result = await controller.CreateBranch(new Branch());

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Courses_ReturnsViewWithCourses()
    {
        using var ctx = CreateContext();
        var branch = new Branch { Name = "Science" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();
        ctx.Courses.Add(new Course { Name = "Math", BranchId = branch.Id });
        await ctx.SaveChangesAsync();

        var controller = new AdminController(ctx);
        var result = await controller.Courses();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Course>>(view.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task CreateCourse_Post_ValidModel_RedirectsToCourses()
    {
        using var ctx = CreateContext();
        var branch = new Branch { Name = "Science" };
        ctx.Branches.Add(branch);
        await ctx.SaveChangesAsync();

        var controller = new AdminController(ctx);
        var course = new Course { Name = "Physics", BranchId = branch.Id };

        var result = await controller.CreateCourse(course);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Courses", redirect.ActionName);
    }

    [Fact]
    public async Task CreateCourse_Post_InvalidModel_ReturnsView()
    {
        using var ctx = CreateContext();
        var controller = new AdminController(ctx);
        controller.ModelState.AddModelError("Name", "Required");

        var result = await controller.CreateCourse(new Course());

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task AssignFaculty_Post_DuplicateAssignment_ReturnsView()
    {
        using var ctx = CreateContext();
        var faculty = new FacultyProfile { Name = "Dr. Smith", Email = "smith@test.com", IdentityUserId = "u1" };
        var course = new Course { Name = "Math" };
        ctx.FacultyProfiles.Add(faculty);
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        ctx.FacultyCourseAssignments.Add(new FacultyCourseAssignment
        {
            FacultyProfileId = faculty.Id,
            CourseId = course.Id
        });
        await ctx.SaveChangesAsync();

        var controller = new AdminController(ctx);
        var assignment = new FacultyCourseAssignment
        {
            FacultyProfileId = faculty.Id,
            CourseId = course.Id
        };

        var result = await controller.AssignFaculty(assignment);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task AssignFaculty_Post_NewAssignment_Redirects()
    {
        using var ctx = CreateContext();
        var faculty = new FacultyProfile { Name = "Dr. Jones", Email = "jones@test.com", IdentityUserId = "u2" };
        var course = new Course { Name = "Chemistry" };
        ctx.FacultyProfiles.Add(faculty);
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        var controller = new AdminController(ctx);
        var assignment = new FacultyCourseAssignment
        {
            FacultyProfileId = faculty.Id,
            CourseId = course.Id
        };

        var result = await controller.AssignFaculty(assignment);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("FacultyAssignments", redirect.ActionName);
    }
}

// ─── EnrolmentsController Tests ───────────────────────────────────────────────

public class EnrolmentsControllerTests
{
    private ApplicationDbContext CreateContext() =>
        TestHelpers.CreateInMemoryContext("EnrolDb_" + Guid.NewGuid());

    [Fact]
    public async Task Create_Get_ReturnsView()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new EnrolmentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Create();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_Post_DuplicateEnrolment_ReturnsView()
    {
        using var ctx = CreateContext();
        var student = new StudentProfile { Name = "Alice", Email = "alice@test.com", IdentityUserId = "s1" };
        var course = new Course { Name = "Math" };
        ctx.StudentProfiles.Add(student);
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        ctx.CourseEnrolments.Add(new CourseEnrolment
        {
            StudentProfileId = student.Id,
            CourseId = course.Id,
            EnrolDate = DateTime.Today
        });
        await ctx.SaveChangesAsync();

        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new EnrolmentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Create(new CourseEnrolment
        {
            StudentProfileId = student.Id,
            CourseId = course.Id,
            EnrolDate = DateTime.Today
        });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Create_Post_ValidEnrolment_Redirects()
    {
        using var ctx = CreateContext();
        var student = new StudentProfile { Name = "Bob", Email = "bob@test.com", IdentityUserId = "s2" };
        var course = new Course { Name = "Physics" };
        ctx.StudentProfiles.Add(student);
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new EnrolmentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Create(new CourseEnrolment
        {
            StudentProfileId = student.Id,
            CourseId = course.Id,
            EnrolDate = DateTime.Today
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task Edit_Get_NotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new EnrolmentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Edit(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_IdMismatch_ReturnsBadRequest()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new EnrolmentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Edit(1, new CourseEnrolment { Id = 2 });
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Edit_Post_ValidUpdate_Redirects()
    {
        using var ctx = CreateContext();
        var student = new StudentProfile { Name = "Charlie", Email = "charlie@test.com", IdentityUserId = "s3" };
        var course = new Course { Name = "Biology" };
        ctx.StudentProfiles.Add(student);
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        var enrolment = new CourseEnrolment
        {
            StudentProfileId = student.Id,
            CourseId = course.Id,
            EnrolDate = DateTime.Today,
            Status = EnrolmentStatus.Active,
        };
        ctx.CourseEnrolments.Add(enrolment);
        await ctx.SaveChangesAsync();

        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new EnrolmentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Edit(enrolment.Id, new CourseEnrolment
        {
            Id = enrolment.Id,
            Status = EnrolmentStatus.Completed,
            EnrolDate = DateTime.Today
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task MyEnrolments_ProfileNotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("unknown_user");

        var controller = new EnrolmentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("unknown_user", "Student")
        };

        var result = await controller.MyEnrolments();
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task MyEnrolments_ValidStudent_ReturnsView()
    {
        using var ctx = CreateContext();
        var profile = new StudentProfile { Name = "Dave", Email = "dave@test.com", IdentityUserId = "s4" };
        ctx.StudentProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("s4");

        var controller = new EnrolmentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("s4", "Student")
        };

        var result = await controller.MyEnrolments();
        Assert.IsType<ViewResult>(result);
    }
}

// ─── AttendanceController Tests ───────────────────────────────────────────────

public class AttendanceControllerTests
{
    private ApplicationDbContext CreateContext() =>
        TestHelpers.CreateInMemoryContext("AttendDb_" + Guid.NewGuid());

    [Fact]
    public async Task Index_EnrolmentNotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new AttendanceController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Index(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_ValidEnrolment_AsAdmin_ReturnsView()
    {
        using var ctx = CreateContext();
        var student = new StudentProfile { Name = "Eve", Email = "eve@test.com", IdentityUserId = "s5" };
        var course = new Course { Name = "History" };
        ctx.StudentProfiles.Add(student);
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        var enrolment = new CourseEnrolment
        {
            StudentProfileId = student.Id,
            CourseId = course.Id,
            EnrolDate = DateTime.Today
        };
        ctx.CourseEnrolments.Add(enrolment);
        await ctx.SaveChangesAsync();

        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new AttendanceController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Index(enrolment.Id);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_Get_EnrolmentNotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new AttendanceController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Create(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_Post_ValidRecord_Redirects()
    {
        using var ctx = CreateContext();
        var student = new StudentProfile { Name = "Frank", Email = "frank@test.com", IdentityUserId = "s6" };
        var course = new Course { Name = "Art" };
        ctx.StudentProfiles.Add(student);
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        var enrolment = new CourseEnrolment
        {
            StudentProfileId = student.Id,
            CourseId = course.Id,
            EnrolDate = DateTime.Today
        };
        ctx.CourseEnrolments.Add(enrolment);
        await ctx.SaveChangesAsync();

        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new AttendanceController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var record = new AttendanceRecord
        {
            CourseEnrolmentId = enrolment.Id,
            Date = DateTime.Today,
            WeekNumber = 1,
            Present = true
        };

        var result = await controller.Create(record);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task Edit_Get_NotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new AttendanceController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Edit(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_IdMismatch_ReturnsBadRequest()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new AttendanceController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Edit(1, new AttendanceRecord { Id = 2 });
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task MyAttendance_ProfileNotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("unknown");

        var controller = new AttendanceController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("unknown", "Student")
        };

        var result = await controller.MyAttendance();
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task MyAttendance_ValidStudent_ReturnsView()
    {
        using var ctx = CreateContext();
        var profile = new StudentProfile { Name = "Grace", Email = "grace@test.com", IdentityUserId = "s7" };
        ctx.StudentProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("s7");

        var controller = new AttendanceController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("s7", "Student")
        };

        var result = await controller.MyAttendance();
        Assert.IsType<ViewResult>(result);
    }
}

// ─── StudentsController Tests ─────────────────────────────────────────────────

public class StudentsControllerTests
{
    private ApplicationDbContext CreateContext() =>
        TestHelpers.CreateInMemoryContext("StudentsDb_" + Guid.NewGuid());

    [Fact]
    public void Create_Get_ReturnsView()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new StudentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = controller.Create();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_Get_NotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new StudentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Edit(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_ValidStudent_ReturnsView()
    {
        using var ctx = CreateContext();
        var student = new StudentProfile { Name = "Harry", Email = "harry@test.com", IdentityUserId = "s8" };
        ctx.StudentProfiles.Add(student);
        await ctx.SaveChangesAsync();

        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new StudentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Edit(student.Id);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_Post_IdMismatch_ReturnsBadRequest()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new StudentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Edit(1, new StudentProfile { Id = 2 });
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Edit_Post_InvalidModel_ReturnsView()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new StudentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };
        controller.ModelState.AddModelError("Name", "Required");

        var result = await controller.Edit(1, new StudentProfile { Id = 1 });
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task MyProfile_ProfileNotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("unknown");

        var controller = new StudentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("unknown", "Student")
        };

        var result = await controller.MyProfile();
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_NotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("admin1");

        var controller = new StudentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Details(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_Admin_ValidStudent_ReturnsView()
    {
        using var ctx = CreateContext();
        var student = new StudentProfile { Name = "Ivy", Email = "ivy@test.com", IdentityUserId = "s9" };
        ctx.StudentProfiles.Add(student);
        await ctx.SaveChangesAsync();

        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("admin1");

        var controller = new StudentsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.Details(student.Id);
        Assert.IsType<ViewResult>(result);
    }
}

// ─── ExamsController Tests ────────────────────────────────────────────────────

public class ExamsControllerTests
{
    private ApplicationDbContext CreateContext() =>
        TestHelpers.CreateInMemoryContext("ExamsDb_" + Guid.NewGuid());

    [Fact]
    public async Task ToggleRelease_NotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new ExamsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.ToggleRelease(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ToggleRelease_ValidExam_TogglesAndRedirects()
    {
        using var ctx = CreateContext();
        var course = new Course { Name = "Math" };
        ctx.Courses.Add(course);
        await ctx.SaveChangesAsync();

        var exam = new Exam
        {
            Title = "Midterm",
            CourseId = course.Id,
            Date = DateTime.Today,
            MaxScore = 100,
            ResultsReleased = false
        };
        ctx.Exams.Add(exam);
        await ctx.SaveChangesAsync();

        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new ExamsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin"),
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
        };

        var result = await controller.ToggleRelease(exam.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.True(ctx.Exams.Find(exam.Id)!.ResultsReleased);
    }

    [Fact]
    public async Task MyExamResults_ProfileNotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("unknown");

        var controller = new ExamsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("unknown", "Student")
        };

        var result = await controller.MyExamResults();
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task MyExamResults_ValidStudent_ReturnsView()
    {
        using var ctx = CreateContext();
        var profile = new StudentProfile { Name = "Jack", Email = "jack@test.com", IdentityUserId = "s10" };
        ctx.StudentProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("s10");

        var controller = new ExamsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("s10", "Student")
        };

        var result = await controller.MyExamResults();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task ExamResults_NotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("admin1");

        var controller = new ExamsController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.ExamResults(999);
        Assert.IsType<NotFoundResult>(result);
    }
}

// ─── GradebookController Tests ────────────────────────────────────────────────

public class GradebookControllerTests
{
    private ApplicationDbContext CreateContext() =>
        TestHelpers.CreateInMemoryContext("GradebookDb_" + Guid.NewGuid());

    [Fact]
    public async Task MyGradebook_ProfileNotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("unknown");

        var controller = new GradebookController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("unknown", "Student")
        };

        var result = await controller.MyGradebook();
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task MyGradebook_ValidStudent_ReturnsView()
    {
        using var ctx = CreateContext();
        var profile = new StudentProfile { Name = "Kate", Email = "kate@test.com", IdentityUserId = "s11" };
        ctx.StudentProfiles.Add(profile);
        await ctx.SaveChangesAsync();

        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("s11");

        var controller = new GradebookController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("s11", "Student")
        };

        var result = await controller.MyGradebook();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task AssignmentResults_NotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("admin1");

        var controller = new GradebookController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.AssignmentResults(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateAssignment_Post_InvalidModel_ReturnsView()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("admin1");

        var controller = new GradebookController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };
        controller.ModelState.AddModelError("Title", "Required");

        var result = await controller.CreateAssignment(new Assignment());
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task EditAssignment_Get_NotFound_ReturnsNotFound()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        userMgr.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("admin1");

        var controller = new GradebookController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.EditAssignment(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditAssignment_Post_IdMismatch_ReturnsBadRequest()
    {
        using var ctx = CreateContext();
        var userMgr = TestHelpers.CreateUserManagerMock();
        var controller = new GradebookController(ctx, userMgr.Object)
        {
            ControllerContext = TestHelpers.CreateControllerContext("admin1", "Admin")
        };

        var result = await controller.EditAssignment(1, new Assignment { Id = 2 });
        Assert.IsType<BadRequestResult>(result);
    }
}

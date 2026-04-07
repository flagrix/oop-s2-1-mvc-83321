using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using VgcCollege.Web.Controllers;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;
using Xunit;

namespace VgcCollege.Tests;

// ─── Shared Helpers ───────────────────────────────────────────────────────────

public static class ControllerTestHelpers
{
    public static ApplicationDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new ApplicationDbContext(options);
    }

    public static Mock<UserManager<IdentityUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    /// <summary>Sets up a ClaimsPrincipal simulating an authenticated user with given roles.</summary>
    public static ClaimsPrincipal MakeUser(string userId, params string[] roles)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    /// <summary>Attaches a mocked HttpContext with the given user to a controller.</summary>
    public static void SetUser(Controller controller, ClaimsPrincipal user)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
}

// ─── HomeController Tests ─────────────────────────────────────────────────────

public class HomeControllerTests
{
    [Fact]
    public void Index_ReturnsViewResult()
    {
        var controller = new HomeController();
        var result = controller.Index();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        var controller = new HomeController();
        var result = controller.Privacy();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void AccessDenied_ReturnsViewResult()
    {
        var controller = new HomeController();
        var result = controller.AccessDenied();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void NotFoundPage_ReturnsViewResult()
    {
        var controller = new HomeController();
        var result = controller.NotFoundPage();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Error_ReturnsViewResult_WithErrorViewModel()
    {
        var controller = new HomeController();
        ControllerTestHelpers.SetUser(controller, ControllerTestHelpers.MakeUser("u1"));
        var result = controller.Error();
        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<ErrorViewModel>(view.Model);
    }
}

// ─── AdminController Tests ────────────────────────────────────────────────────

public class AdminControllerTests
{
    private (AdminController ctrl, ApplicationDbContext db) Setup(string dbName, string userId = "admin1")
    {
        var db = ControllerTestHelpers.CreateDb(dbName);
        var ctrl = new AdminController(db);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser(userId, "Admin"));
        return (ctrl, db);
    }

    [Fact]
    public void Index_ReturnsView()
    {
        var (ctrl, _) = Setup(nameof(Index_ReturnsView));
        var result = ctrl.Index();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Branches_ReturnsViewWithBranches()
    {
        var (ctrl, db) = Setup(nameof(Branches_ReturnsViewWithBranches));
        db.Branches.AddRange(
            new Branch { Name = "Science", Address = "1 St" },
            new Branch { Name = "Arts", Address = "2 St" }
        );
        await db.SaveChangesAsync();

        var result = await ctrl.Branches();
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Branch>>(view.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public void CreateBranch_GET_ReturnsView()
    {
        var (ctrl, _) = Setup(nameof(CreateBranch_GET_ReturnsView));
        var result = ctrl.CreateBranch();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task CreateBranch_POST_ValidModel_Redirects()
    {
        var (ctrl, db) = Setup(nameof(CreateBranch_POST_ValidModel_Redirects));
        var branch = new Branch { Name = "Engineering", Address = "3 St" };

        var result = await ctrl.CreateBranch(branch);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Branches", redirect.ActionName);
        Assert.Equal(1, await db.Branches.CountAsync());
    }

    [Fact]
    public async Task CreateBranch_POST_InvalidModel_ReturnsView()
    {
        var (ctrl, _) = Setup(nameof(CreateBranch_POST_InvalidModel_ReturnsView));
        ctrl.ModelState.AddModelError("Name", "Required");
        var branch = new Branch { Name = "", Address = "3 St" };

        var result = await ctrl.CreateBranch(branch);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Courses_ReturnsViewWithCourses()
    {
        var (ctrl, db) = Setup(nameof(Courses_ReturnsViewWithCourses));
        var branch = new Branch { Name = "Science", Address = "1 St" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();
        db.Courses.Add(new Course { Name = "Math", BranchId = branch.Id });
        await db.SaveChangesAsync();

        var result = await ctrl.Courses();
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Course>>(view.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task CreateCourse_GET_ReturnsView()
    {
        var (ctrl, db) = Setup(nameof(CreateCourse_GET_ReturnsView));
        db.Branches.Add(new Branch { Name = "Science", Address = "1 St" });
        await db.SaveChangesAsync();

        var result = await ctrl.CreateCourse();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task CreateCourse_POST_ValidModel_Redirects()
    {
        var (ctrl, db) = Setup(nameof(CreateCourse_POST_ValidModel_Redirects));
        var branch = new Branch { Name = "Science", Address = "1 St" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var course = new Course { Name = "Physics", BranchId = branch.Id };
        var result = await ctrl.CreateCourse(course);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Courses", redirect.ActionName);
        Assert.Equal(1, await db.Courses.CountAsync());
    }

    [Fact]
    public async Task CreateCourse_POST_InvalidModel_ReturnsView()
    {
        var (ctrl, db) = Setup(nameof(CreateCourse_POST_InvalidModel_ReturnsView));
        db.Branches.Add(new Branch { Name = "Science", Address = "1 St" });
        await db.SaveChangesAsync();
        ctrl.ModelState.AddModelError("Name", "Required");

        var result = await ctrl.CreateCourse(new Course { Name = "" });
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task FacultyAssignments_ReturnsView()
    {
        var (ctrl, _) = Setup(nameof(FacultyAssignments_ReturnsView));
        var result = await ctrl.FacultyAssignments();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task AssignFaculty_GET_ReturnsView()
    {
        var (ctrl, db) = Setup(nameof(AssignFaculty_GET_ReturnsView));
        db.FacultyProfiles.Add(new FacultyProfile { Name = "Dr. X", Email = "x@c.ie", IdentityUserId = "f1" });
        db.Courses.Add(new Course { Name = "Math", BranchId = 0 });
        await db.SaveChangesAsync();

        var result = await ctrl.AssignFaculty();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task AssignFaculty_POST_ValidNew_Redirects()
    {
        var (ctrl, db) = Setup(nameof(AssignFaculty_POST_ValidNew_Redirects));
        var branch = new Branch { Name = "Sci", Address = "1 St" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();
        var course = new Course { Name = "Math", BranchId = branch.Id };
        db.Courses.Add(course);
        var faculty = new FacultyProfile { Name = "Dr. X", Email = "x@c.ie", IdentityUserId = "f1" };
        db.FacultyProfiles.Add(faculty);
        await db.SaveChangesAsync();

        var assignment = new FacultyCourseAssignment { FacultyProfileId = faculty.Id, CourseId = course.Id };
        var result = await ctrl.AssignFaculty(assignment);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("FacultyAssignments", redirect.ActionName);
    }

    [Fact]
    public async Task AssignFaculty_POST_Duplicate_ReturnsView()
    {
        var (ctrl, db) = Setup(nameof(AssignFaculty_POST_Duplicate_ReturnsView));
        var branch = new Branch { Name = "Sci", Address = "1 St" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();
        var course = new Course { Name = "Math", BranchId = branch.Id };
        db.Courses.Add(course);
        var faculty = new FacultyProfile { Name = "Dr. X", Email = "x@c.ie", IdentityUserId = "f1" };
        db.FacultyProfiles.Add(faculty);
        await db.SaveChangesAsync();

        // First assignment
        db.FacultyCourseAssignments.Add(new FacultyCourseAssignment { FacultyProfileId = faculty.Id, CourseId = course.Id });
        await db.SaveChangesAsync();

        // Attempt duplicate
        var result = await ctrl.AssignFaculty(new FacultyCourseAssignment { FacultyProfileId = faculty.Id, CourseId = course.Id });
        Assert.IsType<ViewResult>(result);
    }
}

// ─── StudentsController Tests ──────────────────────────────────────────────────

public class StudentsControllerTests
{
    private async Task<(StudentsController ctrl, ApplicationDbContext db, StudentProfile student)> SetupWithStudentAsync(string dbName)
    {
        var db = ControllerTestHelpers.CreateDb(dbName);
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("admin1");

        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();

        var ctrl = new StudentsController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("admin1", "Admin"));
        return (ctrl, db, student);
    }

    [Fact]
    public async Task Index_AsAdmin_ReturnsAllStudents()
    {
        var (ctrl, db, _) = await SetupWithStudentAsync(nameof(Index_AsAdmin_ReturnsAllStudents));
        db.StudentProfiles.Add(new StudentProfile { Name = "Bob", Email = "b@b.com", StudentNumber = "S2", IdentityUserId = "u2" });
        await db.SaveChangesAsync();

        var result = await ctrl.Index();
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<StudentProfile>>(view.Model);
        Assert.Equal(2, model.Count());
    }

    [Fact]
    public void Create_GET_ReturnsView()
    {
        var db = ControllerTestHelpers.CreateDb(nameof(Create_GET_ReturnsView));
        var um = ControllerTestHelpers.MockUserManager();
        var ctrl = new StudentsController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("admin1", "Admin"));

        var result = ctrl.Create();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Details_AsAdmin_ValidId_ReturnsView()
    {
        var (ctrl, db, student) = await SetupWithStudentAsync(nameof(Details_AsAdmin_ValidId_ReturnsView));

        var result = await ctrl.Details(student.Id);
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<StudentProfile>(view.Model);
        Assert.Equal(student.Id, model.Id);
    }

    [Fact]
    public async Task Details_InvalidId_ReturnsNotFound()
    {
        var (ctrl, _, _) = await SetupWithStudentAsync(nameof(Details_InvalidId_ReturnsNotFound));
        var result = await ctrl.Details(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_GET_ValidId_ReturnsView()
    {
        var (ctrl, _, student) = await SetupWithStudentAsync(nameof(Edit_GET_ValidId_ReturnsView));
        var result = await ctrl.Edit(student.Id);
        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<StudentProfile>(view.Model);
    }

    [Fact]
    public async Task Edit_GET_InvalidId_ReturnsNotFound()
    {
        var (ctrl, _, _) = await SetupWithStudentAsync(nameof(Edit_GET_InvalidId_ReturnsNotFound));
        var result = await ctrl.Edit(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_POST_IdMismatch_ReturnsBadRequest()
    {
        var (ctrl, _, student) = await SetupWithStudentAsync(nameof(Edit_POST_IdMismatch_ReturnsBadRequest));
        var result = await ctrl.Edit(999, new StudentProfile { Id = student.Id, Name = "Alice", Email = "a@b.com", StudentNumber = "S1" });
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Edit_POST_InvalidModel_ReturnsView()
    {
        var (ctrl, _, student) = await SetupWithStudentAsync(nameof(Edit_POST_InvalidModel_ReturnsView));
        ctrl.ModelState.AddModelError("Name", "Required");
        var profile = new StudentProfile { Id = student.Id, Name = "", Email = "a@b.com", StudentNumber = "S1" };
        var result = await ctrl.Edit(student.Id, profile);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task MyProfile_ValidUser_ReturnsView()
    {
        var db = ControllerTestHelpers.CreateDb(nameof(MyProfile_ValidUser_ReturnsView));
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("u1");

        db.StudentProfiles.Add(new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" });
        await db.SaveChangesAsync();

        var ctrl = new StudentsController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("u1", "Student"));

        var result = await ctrl.MyProfile();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task MyProfile_NoProfile_ReturnsNotFound()
    {
        var db = ControllerTestHelpers.CreateDb(nameof(MyProfile_NoProfile_ReturnsNotFound));
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("unknown");

        var ctrl = new StudentsController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("unknown", "Student"));

        var result = await ctrl.MyProfile();
        Assert.IsType<NotFoundResult>(result);
    }
}

// ─── EnrolmentsController Tests ───────────────────────────────────────────────

public class EnrolmentsControllerTests
{
    private async Task<(EnrolmentsController ctrl, ApplicationDbContext db, StudentProfile student, Course course)> SetupAsync(string dbName)
    {
        var db = ControllerTestHelpers.CreateDb(dbName);
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("admin1");

        var branch = new Branch { Name = "Sci", Address = "1 St" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var course = new Course { Name = "Math", BranchId = branch.Id };
        db.Courses.Add(course);
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();

        var ctrl = new EnrolmentsController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("admin1", "Admin"));
        return (ctrl, db, student, course);
    }

    [Fact]
    public async Task Index_AsAdmin_ReturnsView()
    {
        var (ctrl, _, _, _) = await SetupAsync(nameof(Index_AsAdmin_ReturnsView));
        var result = await ctrl.Index(null, null);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_WithCourseFilter_ReturnsFilteredView()
    {
        var (ctrl, db, student, course) = await SetupAsync(nameof(Index_WithCourseFilter_ReturnsFilteredView));
        db.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id });
        await db.SaveChangesAsync();

        var result = await ctrl.Index(course.Id, null);
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<CourseEnrolment>>(view.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task Create_GET_ReturnsView()
    {
        var (ctrl, _, _, _) = await SetupAsync(nameof(Create_GET_ReturnsView));
        var result = await ctrl.Create();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_POST_ValidNew_Redirects()
    {
        var (ctrl, db, student, course) = await SetupAsync(nameof(Create_POST_ValidNew_Redirects));

        var enrolment = new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id, EnrolDate = DateTime.Today };
        var result = await ctrl.Create(enrolment);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(1, await db.CourseEnrolments.CountAsync());
    }

    [Fact]
    public async Task Create_POST_Duplicate_ReturnsView()
    {
        var (ctrl, db, student, course) = await SetupAsync(nameof(Create_POST_Duplicate_ReturnsView));
        db.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id });
        await db.SaveChangesAsync();

        var result = await ctrl.Create(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id });
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_GET_ValidId_ReturnsView()
    {
        var (ctrl, db, student, course) = await SetupAsync(nameof(Edit_GET_ValidId_ReturnsView));
        var enrolment = new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id };
        db.CourseEnrolments.Add(enrolment);
        await db.SaveChangesAsync();

        var result = await ctrl.Edit(enrolment.Id);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_GET_InvalidId_ReturnsNotFound()
    {
        var (ctrl, _, _, _) = await SetupAsync(nameof(Edit_GET_InvalidId_ReturnsNotFound));
        var result = await ctrl.Edit(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_POST_IdMismatch_ReturnsBadRequest()
    {
        var (ctrl, _, _, _) = await SetupAsync(nameof(Edit_POST_IdMismatch_ReturnsBadRequest));
        var result = await ctrl.Edit(1, new CourseEnrolment { Id = 99 });
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Edit_POST_ValidModel_Redirects()
    {
        var (ctrl, db, student, course) = await SetupAsync(nameof(Edit_POST_ValidModel_Redirects));
        var enrolment = new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id, Status = EnrolmentStatus.Active };
        db.CourseEnrolments.Add(enrolment);
        await db.SaveChangesAsync();

        var updated = new CourseEnrolment { Id = enrolment.Id, StudentProfileId = student.Id, CourseId = course.Id, Status = EnrolmentStatus.Completed };
        var result = await ctrl.Edit(enrolment.Id, updated);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task MyEnrolments_ValidStudent_ReturnsView()
    {
        var db = ControllerTestHelpers.CreateDb(nameof(MyEnrolments_ValidStudent_ReturnsView));
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("u1");

        var branch = new Branch { Name = "Sci", Address = "1 St" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();
        var course = new Course { Name = "Math", BranchId = branch.Id };
        db.Courses.Add(course);
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();
        db.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id });
        await db.SaveChangesAsync();

        var ctrl = new EnrolmentsController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("u1", "Student"));

        var result = await ctrl.MyEnrolments();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task MyEnrolments_NoProfile_ReturnsNotFound()
    {
        var db = ControllerTestHelpers.CreateDb(nameof(MyEnrolments_NoProfile_ReturnsNotFound));
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("nobody");

        var ctrl = new EnrolmentsController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("nobody", "Student"));

        var result = await ctrl.MyEnrolments();
        Assert.IsType<NotFoundResult>(result);
    }
}

// ─── AttendanceController Tests ───────────────────────────────────────────────

public class AttendanceControllerTests
{
    private async Task<(AttendanceController ctrl, ApplicationDbContext db, CourseEnrolment enrolment)> SetupAdminAsync(string dbName)
    {
        var db = ControllerTestHelpers.CreateDb(dbName);
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("admin1");

        var branch = new Branch { Name = "Sci", Address = "1 St" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();
        var course = new Course { Name = "Math", BranchId = branch.Id };
        db.Courses.Add(course);
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();
        var enrolment = new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id };
        db.CourseEnrolments.Add(enrolment);
        await db.SaveChangesAsync();

        var ctrl = new AttendanceController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("admin1", "Admin"));
        return (ctrl, db, enrolment);
    }

    [Fact]
    public async Task Index_ValidEnrolmentId_ReturnsView()
    {
        var (ctrl, _, enrolment) = await SetupAdminAsync(nameof(Index_ValidEnrolmentId_ReturnsView));
        var result = await ctrl.Index(enrolment.Id);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_InvalidEnrolmentId_ReturnsNotFound()
    {
        var (ctrl, _, _) = await SetupAdminAsync(nameof(Index_InvalidEnrolmentId_ReturnsNotFound));
        var result = await ctrl.Index(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_GET_ValidEnrolment_ReturnsView()
    {
        var (ctrl, _, enrolment) = await SetupAdminAsync(nameof(Create_GET_ValidEnrolment_ReturnsView));
        var result = await ctrl.Create(enrolment.Id);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_GET_InvalidEnrolment_ReturnsNotFound()
    {
        var (ctrl, _, _) = await SetupAdminAsync(nameof(Create_GET_InvalidEnrolment_ReturnsNotFound));
        var result = await ctrl.Create(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_POST_ValidRecord_Redirects()
    {
        var (ctrl, db, enrolment) = await SetupAdminAsync(nameof(Create_POST_ValidRecord_Redirects));
        var record = new AttendanceRecord { CourseEnrolmentId = enrolment.Id, WeekNumber = 1, Present = true, Date = DateTime.Today };

        var result = await ctrl.Create(record);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(1, await db.AttendanceRecords.CountAsync());
    }

    [Fact]
    public async Task Create_POST_InvalidModel_ReturnsView()
    {
        var (ctrl, _, enrolment) = await SetupAdminAsync(nameof(Create_POST_InvalidModel_ReturnsView));
        ctrl.ModelState.AddModelError("WeekNumber", "Required");
        var record = new AttendanceRecord { CourseEnrolmentId = enrolment.Id };

        var result = await ctrl.Create(record);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_GET_ValidId_ReturnsView()
    {
        var (ctrl, db, enrolment) = await SetupAdminAsync(nameof(Edit_GET_ValidId_ReturnsView));
        var record = new AttendanceRecord { CourseEnrolmentId = enrolment.Id, WeekNumber = 1, Present = true };
        db.AttendanceRecords.Add(record);
        await db.SaveChangesAsync();

        var result = await ctrl.Edit(record.Id);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_GET_InvalidId_ReturnsNotFound()
    {
        var (ctrl, _, _) = await SetupAdminAsync(nameof(Edit_GET_InvalidId_ReturnsNotFound));
        var result = await ctrl.Edit(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_POST_IdMismatch_ReturnsBadRequest()
    {
        var (ctrl, _, _) = await SetupAdminAsync(nameof(Edit_POST_IdMismatch_ReturnsBadRequest));
        var result = await ctrl.Edit(1, new AttendanceRecord { Id = 99 });
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Edit_POST_Valid_Redirects()
    {
        var (ctrl, db, enrolment) = await SetupAdminAsync(nameof(Edit_POST_Valid_Redirects));
        var record = new AttendanceRecord { CourseEnrolmentId = enrolment.Id, WeekNumber = 1, Present = false };
        db.AttendanceRecords.Add(record);
        await db.SaveChangesAsync();

        var updated = new AttendanceRecord { Id = record.Id, CourseEnrolmentId = enrolment.Id, WeekNumber = 1, Present = true, Date = DateTime.Today };
        var result = await ctrl.Edit(record.Id, updated);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
    }

    [Fact]
    public async Task Edit_POST_NotFound_ReturnsNotFound()
    {
        var (ctrl, _, _) = await SetupAdminAsync(nameof(Edit_POST_NotFound_ReturnsNotFound));
        var result = await ctrl.Edit(9999, new AttendanceRecord { Id = 9999 });
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task MyAttendance_ValidStudent_ReturnsView()
    {
        var db = ControllerTestHelpers.CreateDb(nameof(MyAttendance_ValidStudent_ReturnsView));
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("u1");

        var branch = new Branch { Name = "Sci", Address = "1 St" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();
        var course = new Course { Name = "Math", BranchId = branch.Id };
        db.Courses.Add(course);
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();
        db.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id });
        await db.SaveChangesAsync();

        var ctrl = new AttendanceController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("u1", "Student"));

        var result = await ctrl.MyAttendance();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task MyAttendance_NoProfile_ReturnsNotFound()
    {
        var db = ControllerTestHelpers.CreateDb(nameof(MyAttendance_NoProfile_ReturnsNotFound));
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("nobody");

        var ctrl = new AttendanceController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("nobody", "Student"));

        var result = await ctrl.MyAttendance();
        Assert.IsType<NotFoundResult>(result);
    }
}

// ─── GradebookController Tests ─────────────────────────────────────────────────

public class GradebookControllerTests
{
    private async Task<(GradebookController ctrl, ApplicationDbContext db, Course course, StudentProfile student, Assignment assignment)> SetupAdminAsync(string dbName)
    {
        var db = ControllerTestHelpers.CreateDb(dbName);
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("admin1");

        var branch = new Branch { Name = "Sci", Address = "1 St" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var course = new Course { Name = "Math", BranchId = branch.Id };
        db.Courses.Add(course);
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();

        var assignment = new Assignment { Title = "Essay", MaxScore = 100, CourseId = course.Id };
        db.Assignments.Add(assignment);
        db.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id });
        await db.SaveChangesAsync();

        var ctrl = new GradebookController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("admin1", "Admin"));
        return (ctrl, db, course, student, assignment);
    }

    [Fact]
    public async Task Assignments_AsAdmin_ReturnsView()
    {
        var (ctrl, _, _, _, _) = await SetupAdminAsync(nameof(Assignments_AsAdmin_ReturnsView));
        var result = await ctrl.Assignments(null);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Assignments_WithCourseFilter_ReturnsFilteredView()
    {
        var (ctrl, _, course, _, _) = await SetupAdminAsync(nameof(Assignments_WithCourseFilter_ReturnsFilteredView));
        var result = await ctrl.Assignments(course.Id);
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Assignment>>(view.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task CreateAssignment_GET_ReturnsView()
    {
        var (ctrl, _, _, _, _) = await SetupAdminAsync(nameof(CreateAssignment_GET_ReturnsView));
        var result = await ctrl.CreateAssignment();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task CreateAssignment_POST_ValidModel_Redirects()
    {
        var (ctrl, db, course, _, _) = await SetupAdminAsync(nameof(CreateAssignment_POST_ValidModel_Redirects));
        var a = new Assignment { Title = "Lab Report", MaxScore = 50, CourseId = course.Id };

        var result = await ctrl.CreateAssignment(a);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Assignments", redirect.ActionName);
        Assert.Equal(2, await db.Assignments.CountAsync());
    }

    [Fact]
    public async Task CreateAssignment_POST_InvalidModel_ReturnsView()
    {
        var (ctrl, _, course, _, _) = await SetupAdminAsync(nameof(CreateAssignment_POST_InvalidModel_ReturnsView));
        ctrl.ModelState.AddModelError("Title", "Required");
        var result = await ctrl.CreateAssignment(new Assignment { CourseId = course.Id });
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task EditAssignment_GET_ValidId_ReturnsView()
    {
        var (ctrl, _, _, _, assignment) = await SetupAdminAsync(nameof(EditAssignment_GET_ValidId_ReturnsView));
        var result = await ctrl.EditAssignment(assignment.Id);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task EditAssignment_GET_InvalidId_ReturnsNotFound()
    {
        var (ctrl, _, _, _, _) = await SetupAdminAsync(nameof(EditAssignment_GET_InvalidId_ReturnsNotFound));
        var result = await ctrl.EditAssignment(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task EditAssignment_POST_IdMismatch_ReturnsBadRequest()
    {
        var (ctrl, _, _, _, assignment) = await SetupAdminAsync(nameof(EditAssignment_POST_IdMismatch_ReturnsBadRequest));
        var result = await ctrl.EditAssignment(999, new Assignment { Id = assignment.Id, Title = "X", CourseId = assignment.CourseId });
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task AssignmentResults_ValidId_ReturnsView()
    {
        var (ctrl, _, _, _, assignment) = await SetupAdminAsync(nameof(AssignmentResults_ValidId_ReturnsView));
        var result = await ctrl.AssignmentResults(assignment.Id);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task AssignmentResults_InvalidId_ReturnsNotFound()
    {
        var (ctrl, _, _, _, _) = await SetupAdminAsync(nameof(AssignmentResults_InvalidId_ReturnsNotFound));
        var result = await ctrl.AssignmentResults(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SaveAssignmentResults_ValidScores_Redirects()
    {
        var (ctrl, _, _, student, assignment) = await SetupAdminAsync(nameof(SaveAssignmentResults_ValidScores_Redirects));
        var posts = new List<AssignmentResultPost>
        {
            new AssignmentResultPost { StudentProfileId = student.Id, Score = 75, Feedback = "Good" }
        };

        var result = await ctrl.SaveAssignmentResults(assignment.Id, posts);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AssignmentResults", redirect.ActionName);
    }

    [Fact]
    public async Task SaveAssignmentResults_ScoreOutOfRange_Redirects()
    {
        var (ctrl, _, _, student, assignment) = await SetupAdminAsync(nameof(SaveAssignmentResults_ScoreOutOfRange_Redirects));
        var posts = new List<AssignmentResultPost>
        {
            new AssignmentResultPost { StudentProfileId = student.Id, Score = 999 }
        };

        var result = await ctrl.SaveAssignmentResults(assignment.Id, posts);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("AssignmentResults", redirect.ActionName);
    }

    [Fact]
    public async Task SaveAssignmentResults_UpdatesExisting()
    {
        var (ctrl, db, _, student, assignment) = await SetupAdminAsync(nameof(SaveAssignmentResults_UpdatesExisting));
        db.AssignmentResults.Add(new AssignmentResult { AssignmentId = assignment.Id, StudentProfileId = student.Id, Score = 50 });
        await db.SaveChangesAsync();

        var posts = new List<AssignmentResultPost>
        {
            new AssignmentResultPost { StudentProfileId = student.Id, Score = 80, Feedback = "Revised" }
        };

        await ctrl.SaveAssignmentResults(assignment.Id, posts);
        var updated = await db.AssignmentResults.FirstAsync();
        Assert.Equal(80, updated.Score);
    }

    [Fact]
    public async Task MyGradebook_ValidStudent_ReturnsView()
    {
        var db = ControllerTestHelpers.CreateDb(nameof(MyGradebook_ValidStudent_ReturnsView));
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("u1");

        var branch = new Branch { Name = "Sci", Address = "1 St" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();
        var course = new Course { Name = "Math", BranchId = branch.Id };
        db.Courses.Add(course);
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();
        db.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id });
        db.Assignments.Add(new Assignment { Title = "Essay", MaxScore = 100, CourseId = course.Id });
        await db.SaveChangesAsync();

        var ctrl = new GradebookController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("u1", "Student"));

        var result = await ctrl.MyGradebook();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task MyGradebook_NoProfile_ReturnsNotFound()
    {
        var db = ControllerTestHelpers.CreateDb(nameof(MyGradebook_NoProfile_ReturnsNotFound));
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("nobody");

        var ctrl = new GradebookController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("nobody", "Student"));

        var result = await ctrl.MyGradebook();
        Assert.IsType<NotFoundResult>(result);
    }
}

// ─── ExamsController Tests ─────────────────────────────────────────────────────

public class ExamsControllerTests
{
    private async Task<(ExamsController ctrl, ApplicationDbContext db, Course course, StudentProfile student, Exam exam)> SetupAdminAsync(string dbName)
    {
        var db = ControllerTestHelpers.CreateDb(dbName);
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("admin1");

        var branch = new Branch { Name = "Sci", Address = "1 St" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var course = new Course { Name = "Math", BranchId = branch.Id };
        db.Courses.Add(course);
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();

        var exam = new Exam { Title = "Final", MaxScore = 100, CourseId = course.Id, ResultsReleased = false };
        db.Exams.Add(exam);
        db.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id });
        await db.SaveChangesAsync();

        var ctrl = new ExamsController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("admin1", "Admin"));
        return (ctrl, db, course, student, exam);
    }

    [Fact]
    public async Task Index_AsAdmin_ReturnsView()
    {
        var (ctrl, _, _, _, _) = await SetupAdminAsync(nameof(Index_AsAdmin_ReturnsView));
        var result = await ctrl.Index(null);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Index_WithCourseFilter_ReturnsFiltered()
    {
        var (ctrl, _, course, _, _) = await SetupAdminAsync(nameof(Index_WithCourseFilter_ReturnsFiltered));
        var result = await ctrl.Index(course.Id);
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Exam>>(view.Model);
        Assert.Single(model);
    }

    [Fact]
    public async Task Create_GET_ReturnsView()
    {
        var (ctrl, _, _, _, _) = await SetupAdminAsync(nameof(Create_GET_ReturnsView));
        var result = await ctrl.Create();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_POST_ValidExam_Redirects()
    {
        var (ctrl, db, course, _, _) = await SetupAdminAsync(nameof(Create_POST_ValidExam_Redirects));
        var exam = new Exam { Title = "Midterm", MaxScore = 50, CourseId = course.Id };

        var result = await ctrl.Create(exam);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal(2, await db.Exams.CountAsync());
    }

    [Fact]
    public async Task Create_POST_InvalidModel_ReturnsView()
    {
        var (ctrl, _, course, _, _) = await SetupAdminAsync(nameof(Create_POST_InvalidModel_ReturnsView));
        ctrl.ModelState.AddModelError("Title", "Required");
        var result = await ctrl.Create(new Exam { CourseId = course.Id });
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_GET_ValidId_ReturnsView()
    {
        var (ctrl, _, _, _, exam) = await SetupAdminAsync(nameof(Edit_GET_ValidId_ReturnsView));
        var result = await ctrl.Edit(exam.Id);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_GET_InvalidId_ReturnsNotFound()
    {
        var (ctrl, _, _, _, _) = await SetupAdminAsync(nameof(Edit_GET_InvalidId_ReturnsNotFound));
        var result = await ctrl.Edit(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_POST_IdMismatch_ReturnsBadRequest()
    {
        var (ctrl, _, _, _, exam) = await SetupAdminAsync(nameof(Edit_POST_IdMismatch_ReturnsBadRequest));
        var result = await ctrl.Edit(999, new Exam { Id = exam.Id, Title = "X", CourseId = exam.CourseId });
        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task ToggleRelease_ValidExam_Redirects()
    {
        var (ctrl, db, _, _, exam) = await SetupAdminAsync(nameof(ToggleRelease_ValidExam_Redirects));
        ctrl.ControllerContext.HttpContext.Features.Set<ITempDataDictionaryFactory>(null!);

        // We just check it doesn't throw and redirects
        var result = await ctrl.ToggleRelease(exam.Id);
        Assert.IsType<RedirectToActionResult>(result);
        var updated = await db.Exams.FindAsync(exam.Id);
        Assert.True(updated!.ResultsReleased); // was false, now toggled
    }

    [Fact]
    public async Task ToggleRelease_InvalidId_ReturnsNotFound()
    {
        var (ctrl, _, _, _, _) = await SetupAdminAsync(nameof(ToggleRelease_InvalidId_ReturnsNotFound));
        var result = await ctrl.ToggleRelease(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ExamResults_ValidId_ReturnsView()
    {
        var (ctrl, _, _, _, exam) = await SetupAdminAsync(nameof(ExamResults_ValidId_ReturnsView));
        var result = await ctrl.ExamResults(exam.Id);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task ExamResults_InvalidId_ReturnsNotFound()
    {
        var (ctrl, _, _, _, _) = await SetupAdminAsync(nameof(ExamResults_InvalidId_ReturnsNotFound));
        var result = await ctrl.ExamResults(9999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SaveExamResults_ValidScore_Redirects()
    {
        var (ctrl, _, _, student, exam) = await SetupAdminAsync(nameof(SaveExamResults_ValidScore_Redirects));
        var posts = new List<ExamResultPost>
        {
            new ExamResultPost { StudentProfileId = student.Id, Score = 85, Grade = "A" }
        };
        var result = await ctrl.SaveExamResults(exam.Id, posts);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ExamResults", redirect.ActionName);
    }

    [Fact]
    public async Task SaveExamResults_ScoreOutOfRange_Redirects()
    {
        var (ctrl, _, _, student, exam) = await SetupAdminAsync(nameof(SaveExamResults_ScoreOutOfRange_Redirects));
        var posts = new List<ExamResultPost>
        {
            new ExamResultPost { StudentProfileId = student.Id, Score = 999 }
        };
        var result = await ctrl.SaveExamResults(exam.Id, posts);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ExamResults", redirect.ActionName);
    }

    [Fact]
    public async Task SaveExamResults_UpdatesExisting()
    {
        var (ctrl, db, _, student, exam) = await SetupAdminAsync(nameof(SaveExamResults_UpdatesExisting));
        db.ExamResults.Add(new ExamResult { ExamId = exam.Id, StudentProfileId = student.Id, Score = 60, Grade = "C" });
        await db.SaveChangesAsync();

        var posts = new List<ExamResultPost>
        {
            new ExamResultPost { StudentProfileId = student.Id, Score = 95, Grade = "A" }
        };
        await ctrl.SaveExamResults(exam.Id, posts);
        var updated = await db.ExamResults.FirstAsync();
        Assert.Equal(95, updated.Score);
        Assert.Equal("A", updated.Grade);
    }

    [Fact]
    public async Task MyExamResults_ValidStudent_ReturnsView()
    {
        var db = ControllerTestHelpers.CreateDb(nameof(MyExamResults_ValidStudent_ReturnsView));
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("u1");

        var branch = new Branch { Name = "Sci", Address = "1 St" };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();
        var course = new Course { Name = "Math", BranchId = branch.Id };
        db.Courses.Add(course);
        var student = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync();
        db.CourseEnrolments.Add(new CourseEnrolment { StudentProfileId = student.Id, CourseId = course.Id });
        db.Exams.Add(new Exam { Title = "Final", MaxScore = 100, CourseId = course.Id, ResultsReleased = true });
        await db.SaveChangesAsync();

        var ctrl = new ExamsController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("u1", "Student"));

        var result = await ctrl.MyExamResults();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task MyExamResults_NoProfile_ReturnsNotFound()
    {
        var db = ControllerTestHelpers.CreateDb(nameof(MyExamResults_NoProfile_ReturnsNotFound));
        var um = ControllerTestHelpers.MockUserManager();
        um.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("nobody");

        var ctrl = new ExamsController(db, um.Object);
        ControllerTestHelpers.SetUser(ctrl, ControllerTestHelpers.MakeUser("nobody", "Student"));

        var result = await ctrl.MyExamResults();
        Assert.IsType<NotFoundResult>(result);
    }
}

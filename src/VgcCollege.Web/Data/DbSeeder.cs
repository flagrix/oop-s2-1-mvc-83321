using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        // Roles
        foreach (var role in new[] { "Admin", "Faculty", "Student" })
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));

        // Branches
        if (!context.Branches.Any())
        {
            context.Branches.AddRange(
                new Branch { Name = "Dublin City Centre", Address = "12 O'Connell Street, Dublin 1" },
                new Branch { Name = "Cork Campus", Address = "45 Patrick Street, Cork" },
                new Branch { Name = "Galway Campus", Address = "78 Shop Street, Galway" }
            );
            await context.SaveChangesAsync();
        }

        var branches = await context.Branches.ToListAsync();

        // Courses
        if (!context.Courses.Any())
        {
            context.Courses.AddRange(
                new Course { Name = "BSc Computer Science", BranchId = branches[0].Id, StartDate = new DateTime(2024, 9, 1), EndDate = new DateTime(2027, 6, 30) },
                new Course { Name = "BA Business Administration", BranchId = branches[0].Id, StartDate = new DateTime(2024, 9, 1), EndDate = new DateTime(2027, 6, 30) },
                new Course { Name = "BSc Nursing", BranchId = branches[1].Id, StartDate = new DateTime(2024, 9, 1), EndDate = new DateTime(2027, 6, 30) },
                new Course { Name = "BA Psychology", BranchId = branches[2].Id, StartDate = new DateTime(2024, 9, 1), EndDate = new DateTime(2027, 6, 30) }
            );
            await context.SaveChangesAsync();
        }

        var courses = await context.Courses.ToListAsync();

        // Admin user
        var admin = await CreateUserIfNotExists(userManager, "admin@vgc.ie", "Admin@123!", "Admin");

        // Faculty users
        var faculty1 = await CreateUserIfNotExists(userManager, "john.smith@vgc.ie", "Faculty@123!", "Faculty");
        var faculty2 = await CreateUserIfNotExists(userManager, "mary.jones@vgc.ie", "Faculty@123!", "Faculty");

        if (!context.FacultyProfiles.Any())
        {
            context.FacultyProfiles.AddRange(
                new FacultyProfile { IdentityUserId = faculty1!.Id, Name = "John Smith", Email = "john.smith@vgc.ie", Phone = "0851234567" },
                new FacultyProfile { IdentityUserId = faculty2!.Id, Name = "Mary Jones", Email = "mary.jones@vgc.ie", Phone = "0867654321" }
            );
            await context.SaveChangesAsync();
        }

        var facultyProfiles = await context.FacultyProfiles.ToListAsync();

        // Faculty course assignments
        if (!context.FacultyCourseAssignments.Any())
        {
            context.FacultyCourseAssignments.AddRange(
                new FacultyCourseAssignment { FacultyProfileId = facultyProfiles[0].Id, CourseId = courses[0].Id },
                new FacultyCourseAssignment { FacultyProfileId = facultyProfiles[0].Id, CourseId = courses[1].Id },
                new FacultyCourseAssignment { FacultyProfileId = facultyProfiles[1].Id, CourseId = courses[2].Id },
                new FacultyCourseAssignment { FacultyProfileId = facultyProfiles[1].Id, CourseId = courses[3].Id }
            );
            await context.SaveChangesAsync();
        }

        // Student users
        var student1 = await CreateUserIfNotExists(userManager, "alice.murphy@student.vgc.ie", "Student@123!", "Student");
        var student2 = await CreateUserIfNotExists(userManager, "bob.kelly@student.vgc.ie", "Student@123!", "Student");

        if (!context.StudentProfiles.Any())
        {
            context.StudentProfiles.AddRange(
                new StudentProfile
                {
                    IdentityUserId = student1!.Id,
                    Name = "Alice Murphy",
                    Email = "alice.murphy@student.vgc.ie",
                    Phone = "0851112233",
                    Address = "5 Main Street, Dublin 2",
                    DateOfBirth = new DateTime(2003, 4, 15),
                    StudentNumber = "VGC2024001"
                },
                new StudentProfile
                {
                    IdentityUserId = student2!.Id,
                    Name = "Bob Kelly",
                    Email = "bob.kelly@student.vgc.ie",
                    Phone = "0869998877",
                    Address = "22 Park Road, Cork",
                    DateOfBirth = new DateTime(2002, 11, 8),
                    StudentNumber = "VGC2024002"
                }
            );
            await context.SaveChangesAsync();
        }

        var students = await context.StudentProfiles.ToListAsync();

        // Enrolments
        if (!context.CourseEnrolments.Any())
        {
            context.CourseEnrolments.AddRange(
                new CourseEnrolment { StudentProfileId = students[0].Id, CourseId = courses[0].Id, EnrolDate = new DateTime(2024, 9, 1), Status = EnrolmentStatus.Active },
                new CourseEnrolment { StudentProfileId = students[1].Id, CourseId = courses[0].Id, EnrolDate = new DateTime(2024, 9, 1), Status = EnrolmentStatus.Active },
                new CourseEnrolment { StudentProfileId = students[0].Id, CourseId = courses[1].Id, EnrolDate = new DateTime(2024, 9, 1), Status = EnrolmentStatus.Active }
            );
            await context.SaveChangesAsync();
        }

        var enrolments = await context.CourseEnrolments.ToListAsync();

        // Attendance records
        if (!context.AttendanceRecords.Any())
        {
            var records = new List<AttendanceRecord>();
            foreach (var enrolment in enrolments.Take(2))
            {
                for (int week = 1; week <= 4; week++)
                {
                    records.Add(new AttendanceRecord
                    {
                        CourseEnrolmentId = enrolment.Id,
                        WeekNumber = week,
                        Date = new DateTime(2024, 9, 1).AddDays((week - 1) * 7),
                        Present = week != 3 // absent week 3
                    });
                }
            }
            context.AttendanceRecords.AddRange(records);
            await context.SaveChangesAsync();
        }

        // Assignments
        if (!context.Assignments.Any())
        {
            context.Assignments.AddRange(
                new Assignment { CourseId = courses[0].Id, Title = "Programming Assignment 1", MaxScore = 100, DueDate = new DateTime(2024, 10, 15) },
                new Assignment { CourseId = courses[0].Id, Title = "Database Design Project", MaxScore = 100, DueDate = new DateTime(2024, 11, 20) }
            );
            await context.SaveChangesAsync();
        }

        // Exams
        if (!context.Exams.Any())
        {
            context.Exams.AddRange(
                new Exam { CourseId = courses[0].Id, Title = "Semester 1 Exam", Date = new DateTime(2025, 1, 10), MaxScore = 100, ResultsReleased = true },
                new Exam { CourseId = courses[0].Id, Title = "Semester 2 Exam", Date = new DateTime(2025, 5, 12), MaxScore = 100, ResultsReleased = false }
            );
            await context.SaveChangesAsync();
        }

        var assignments = await context.Assignments.ToListAsync();
        var exams = await context.Exams.ToListAsync();

        // Assignment Results
        if (!context.AssignmentResults.Any() && assignments.Count >= 2 && students.Count >= 2)
        {
            context.AssignmentResults.AddRange(
                new AssignmentResult { AssignmentId = assignments[0].Id, StudentProfileId = students[0].Id, Score = 82, Feedback = "Good work, well structured." },
                new AssignmentResult { AssignmentId = assignments[0].Id, StudentProfileId = students[1].Id, Score = 74, Feedback = "Needs more detail in section 2." },
                new AssignmentResult { AssignmentId = assignments[1].Id, StudentProfileId = students[0].Id, Score = 91, Feedback = "Excellent design and documentation." }
            );
            await context.SaveChangesAsync();
        }

        // Exam Results (only for released exam so student can see them)
        if (!context.ExamResults.Any() && exams.Count >= 1 && students.Count >= 2)
        {
            context.ExamResults.AddRange(
                new ExamResult { ExamId = exams[0].Id, StudentProfileId = students[0].Id, Score = 78, Grade = "B+" },
                new ExamResult { ExamId = exams[0].Id, StudentProfileId = students[1].Id, Score = 63, Grade = "C" }
            );
            await context.SaveChangesAsync();
        }
    }

    private static async Task<IdentityUser?> CreateUserIfNotExists(
        UserManager<IdentityUser> userManager, string email, string password, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, role);
        }
        return user;
    }
}

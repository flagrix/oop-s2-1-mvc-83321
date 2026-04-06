using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Controllers;

[Authorize]
public class StudentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public StudentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Admin"))
        {
            var all = await _context.StudentProfiles.ToListAsync();
            return View(all);
        }
        // Faculty: only their students
        var userId = _userManager.GetUserId(User);
        var faculty = await _context.FacultyProfiles.FirstOrDefaultAsync(f => f.IdentityUserId == userId);
        if (faculty == null) return Forbid();

        var courseIds = await _context.FacultyCourseAssignments
            .Where(a => a.FacultyProfileId == faculty.Id)
            .Select(a => a.CourseId).ToListAsync();

        var students = await _context.CourseEnrolments
            .Where(e => courseIds.Contains(e.CourseId))
            .Select(e => e.StudentProfile)
            .Distinct().ToListAsync();

        return View(students);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(StudentProfile profile, string password)
    {
        if (!ModelState.IsValid) return View(profile);

        // Create identity user
        var user = new IdentityUser { UserName = profile.Email, Email = profile.Email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, password ?? "Student@123!");
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View(profile);
        }
        await _userManager.AddToRoleAsync(user, "Student");

        profile.IdentityUserId = user.Id;
        _context.StudentProfiles.Add(profile);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User);

        StudentProfile? student;
        if (User.IsInRole("Admin"))
        {
            student = await _context.StudentProfiles
                .Include(s => s.Enrolments).ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        else if (User.IsInRole("Faculty"))
        {
            var faculty = await _context.FacultyProfiles.FirstOrDefaultAsync(f => f.IdentityUserId == userId);
            if (faculty == null) return Forbid();
            var courseIds = await _context.FacultyCourseAssignments
                .Where(a => a.FacultyProfileId == faculty.Id).Select(a => a.CourseId).ToListAsync();
            var enrolled = await _context.CourseEnrolments
                .Where(e => courseIds.Contains(e.CourseId) && e.StudentProfileId == id).AnyAsync();
            if (!enrolled) return Forbid();
            student = await _context.StudentProfiles
                .Include(s => s.Enrolments).ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        else
        {
            // Student: only themselves
            var profile = await _context.StudentProfiles.FirstOrDefaultAsync(s => s.IdentityUserId == userId);
            if (profile == null || profile.Id != id) return Forbid();
            student = await _context.StudentProfiles
                .Include(s => s.Enrolments).ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        if (student == null) return NotFound();
        return View(student);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var student = await _context.StudentProfiles.FindAsync(id);
        if (student == null) return NotFound();
        return View(student);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, StudentProfile profile)
    {
        if (id != profile.Id) return BadRequest();
        if (!ModelState.IsValid) return View(profile);
        _context.Update(profile);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Student: view own profile
    public async Task<IActionResult> MyProfile()
    {
        var userId = _userManager.GetUserId(User);
        var profile = await _context.StudentProfiles
            .Include(s => s.Enrolments).ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(s => s.IdentityUserId == userId);
        if (profile == null) return NotFound();
        return View("Details", profile);
    }
}

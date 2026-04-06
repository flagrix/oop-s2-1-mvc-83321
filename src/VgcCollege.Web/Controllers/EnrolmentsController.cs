using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Controllers;

[Authorize]
public class EnrolmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public EnrolmentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> Index(int? courseId, int? studentId)
    {
        var query = _context.CourseEnrolments
            .Include(e => e.StudentProfile)
            .Include(e => e.Course).ThenInclude(c => c.Branch)
            .AsQueryable();

        if (User.IsInRole("Faculty"))
        {
            var userId = _userManager.GetUserId(User);
            var faculty = await _context.FacultyProfiles.FirstOrDefaultAsync(f => f.IdentityUserId == userId);
            if (faculty == null) return Forbid();
            var courseIds = await _context.FacultyCourseAssignments
                .Where(a => a.FacultyProfileId == faculty.Id).Select(a => a.CourseId).ToListAsync();
            query = query.Where(e => courseIds.Contains(e.CourseId));
        }

        if (courseId.HasValue) query = query.Where(e => e.CourseId == courseId);
        if (studentId.HasValue) query = query.Where(e => e.StudentProfileId == studentId);

        ViewBag.Courses = await _context.Courses.ToListAsync();
        return View(await query.ToListAsync());
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Students = new SelectList(await _context.StudentProfiles.ToListAsync(), "Id", "Name");
        ViewBag.Courses = new SelectList(await _context.Courses.Include(c => c.Branch).ToListAsync(), "Id", "Name");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CourseEnrolment enrolment)
    {
        // Check duplicate
        var exists = await _context.CourseEnrolments
            .AnyAsync(e => e.StudentProfileId == enrolment.StudentProfileId && e.CourseId == enrolment.CourseId);
        if (exists)
        {
            ModelState.AddModelError("", "This student is already enrolled in this course.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Students = new SelectList(await _context.StudentProfiles.ToListAsync(), "Id", "Name");
            ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name");
            return View(enrolment);
        }

        _context.CourseEnrolments.Add(enrolment);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var enrolment = await _context.CourseEnrolments
            .Include(e => e.StudentProfile).Include(e => e.Course).FirstOrDefaultAsync(e => e.Id == id);
        if (enrolment == null) return NotFound();
        return View(enrolment);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id, CourseEnrolment enrolment)
    {
        if (id != enrolment.Id) return BadRequest();
        var existing = await _context.CourseEnrolments.FindAsync(id);
        if (existing == null) return NotFound();
        existing.Status = enrolment.Status;
        existing.EnrolDate = enrolment.EnrolDate;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Student: view own enrolments
    public async Task<IActionResult> MyEnrolments()
    {
        var userId = _userManager.GetUserId(User);
        var profile = await _context.StudentProfiles.FirstOrDefaultAsync(s => s.IdentityUserId == userId);
        if (profile == null) return NotFound();
        var enrolments = await _context.CourseEnrolments
            .Include(e => e.Course).ThenInclude(c => c.Branch)
            .Where(e => e.StudentProfileId == profile.Id).ToListAsync();
        return View(enrolments);
    }
}

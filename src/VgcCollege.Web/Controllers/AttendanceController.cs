using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public AttendanceController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> Index(int enrolmentId)
    {
        var enrolment = await _context.CourseEnrolments
            .Include(e => e.StudentProfile)
            .Include(e => e.Course)
            .Include(e => e.AttendanceRecords)
            .FirstOrDefaultAsync(e => e.Id == enrolmentId);

        if (enrolment == null) return NotFound();

        // Faculty: verify they teach this course
        if (User.IsInRole("Faculty"))
        {
            var userId = _userManager.GetUserId(User);
            var faculty = await _context.FacultyProfiles.FirstOrDefaultAsync(f => f.IdentityUserId == userId);
            if (faculty == null) return Forbid();
            var teaches = await _context.FacultyCourseAssignments
                .AnyAsync(a => a.FacultyProfileId == faculty.Id && a.CourseId == enrolment.CourseId);
            if (!teaches) return Forbid();
        }

        return View(enrolment);
    }

    [Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> Create(int enrolmentId)
    {
        var enrolment = await _context.CourseEnrolments
            .Include(e => e.StudentProfile).Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.Id == enrolmentId);
        if (enrolment == null) return NotFound();

        var record = new AttendanceRecord
        {
            CourseEnrolmentId = enrolmentId,
            Date = DateTime.Today
        };
        ViewBag.Enrolment = enrolment;
        return View(record);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> Create(AttendanceRecord record)
    {
        if (!ModelState.IsValid)
        {
            var enrolment = await _context.CourseEnrolments
                .Include(e => e.StudentProfile).Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == record.CourseEnrolmentId);
            ViewBag.Enrolment = enrolment;
            return View(record);
        }
        _context.AttendanceRecords.Add(record);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { enrolmentId = record.CourseEnrolmentId });
    }

    [Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> Edit(int id)
    {
        var record = await _context.AttendanceRecords
            .Include(r => r.CourseEnrolment).ThenInclude(e => e.StudentProfile)
            .Include(r => r.CourseEnrolment).ThenInclude(e => e.Course)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (record == null) return NotFound();
        return View(record);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> Edit(int id, AttendanceRecord record)
    {
        if (id != record.Id) return BadRequest();
        var existing = await _context.AttendanceRecords.FindAsync(id);
        if (existing == null) return NotFound();
        existing.WeekNumber = record.WeekNumber;
        existing.Date = record.Date;
        existing.Present = record.Present;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { enrolmentId = existing.CourseEnrolmentId });
    }

    // Student: view own attendance
    public async Task<IActionResult> MyAttendance()
    {
        var userId = _userManager.GetUserId(User);
        var profile = await _context.StudentProfiles.FirstOrDefaultAsync(s => s.IdentityUserId == userId);
        if (profile == null) return NotFound();

        var enrolments = await _context.CourseEnrolments
            .Include(e => e.Course)
            .Include(e => e.AttendanceRecords)
            .Where(e => e.StudentProfileId == profile.Id)
            .ToListAsync();

        return View(enrolments);
    }
}

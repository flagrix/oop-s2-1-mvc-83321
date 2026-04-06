using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Controllers;

[Authorize]
public class GradebookController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public GradebookController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ─── Assignments ──────────────────────────────────────────────────────────

    [Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> Assignments(int? courseId)
    {
        var query = _context.Assignments
            .Include(a => a.Course).ThenInclude(c => c.Branch)
            .AsQueryable();

        if (User.IsInRole("Faculty"))
        {
            var userId = _userManager.GetUserId(User);
            var faculty = await _context.FacultyProfiles.FirstOrDefaultAsync(f => f.IdentityUserId == userId);
            if (faculty == null) return Forbid();
            var courseIds = await _context.FacultyCourseAssignments
                .Where(a => a.FacultyProfileId == faculty.Id).Select(a => a.CourseId).ToListAsync();
            query = query.Where(a => courseIds.Contains(a.CourseId));
        }

        if (courseId.HasValue)
            query = query.Where(a => a.CourseId == courseId);

        ViewBag.Courses = await GetAllowedCoursesSelectList();
        ViewBag.SelectedCourseId = courseId;
        return View(await query.OrderByDescending(a => a.DueDate).ToListAsync());
    }

    [Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> CreateAssignment()
    {
        ViewBag.Courses = new SelectList(await GetAllowedCourses(), "Id", "Name");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> CreateAssignment(Assignment assignment)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Courses = new SelectList(await GetAllowedCourses(), "Id", "Name");
            return View(assignment);
        }
        if (!await CanManageCourse(assignment.CourseId)) return Forbid();

        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Assignments));
    }

    [Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> EditAssignment(int id)
    {
        var assignment = await _context.Assignments.Include(a => a.Course).FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();
        if (!await CanManageCourse(assignment.CourseId)) return Forbid();
        ViewBag.Courses = new SelectList(await GetAllowedCourses(), "Id", "Name");
        return View(assignment);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> EditAssignment(int id, Assignment assignment)
    {
        if (id != assignment.Id) return BadRequest();
        if (!await CanManageCourse(assignment.CourseId)) return Forbid();
        if (!ModelState.IsValid)
        {
            ViewBag.Courses = new SelectList(await GetAllowedCourses(), "Id", "Name");
            return View(assignment);
        }
        _context.Update(assignment);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Assignments));
    }

    // ─── Assignment Results ───────────────────────────────────────────────────

    [Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> AssignmentResults(int assignmentId)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Course).FirstOrDefaultAsync(a => a.Id == assignmentId);
        if (assignment == null) return NotFound();
        if (!await CanManageCourse(assignment.CourseId)) return Forbid();

        // Get all enrolled students for this course
        var enrolled = await _context.CourseEnrolments
            .Include(e => e.StudentProfile)
            .Where(e => e.CourseId == assignment.CourseId)
            .ToListAsync();

        var results = await _context.AssignmentResults
            .Where(r => r.AssignmentId == assignmentId)
            .ToListAsync();

        var vm = new AssignmentResultsViewModel
        {
            Assignment = assignment,
            Entries = enrolled.Select(e =>
            {
                var res = results.FirstOrDefault(r => r.StudentProfileId == e.StudentProfileId);
                return new AssignmentResultEntry
                {
                    StudentProfile = e.StudentProfile,
                    ExistingResultId = res?.Id,
                    Score = res?.Score,
                    Feedback = res?.Feedback
                };
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> SaveAssignmentResults(int assignmentId, List<AssignmentResultPost> results)
    {
        var assignment = await _context.Assignments.Include(a => a.Course).FirstOrDefaultAsync(a => a.Id == assignmentId);
        if (assignment == null) return NotFound();
        if (!await CanManageCourse(assignment.CourseId)) return Forbid();

        foreach (var post in results.Where(r => r.Score.HasValue))
        {
            // Validate score range
            if (post.Score < 0 || post.Score > assignment.MaxScore)
            {
                TempData["Error"] = $"Score for student {post.StudentProfileId} is out of range (0–{assignment.MaxScore}).";
                return RedirectToAction(nameof(AssignmentResults), new { assignmentId });
            }

            var existing = await _context.AssignmentResults
                .FirstOrDefaultAsync(r => r.AssignmentId == assignmentId && r.StudentProfileId == post.StudentProfileId);
            if (existing != null)
            {
                existing.Score = post.Score!.Value;
                existing.Feedback = post.Feedback;
            }
            else
            {
                _context.AssignmentResults.Add(new AssignmentResult
                {
                    AssignmentId = assignmentId,
                    StudentProfileId = post.StudentProfileId,
                    Score = post.Score!.Value,
                    Feedback = post.Feedback
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Results saved successfully.";
        return RedirectToAction(nameof(AssignmentResults), new { assignmentId });
    }

    // ─── Student: view own gradebook ─────────────────────────────────────────

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyGradebook()
    {
        var userId = _userManager.GetUserId(User);
        var profile = await _context.StudentProfiles.FirstOrDefaultAsync(s => s.IdentityUserId == userId);
        if (profile == null) return NotFound();

        var enrolments = await _context.CourseEnrolments
            .Include(e => e.Course)
            .Where(e => e.StudentProfileId == profile.Id)
            .ToListAsync();

        var courseIds = enrolments.Select(e => e.CourseId).ToList();

        var assignments = await _context.Assignments
            .Include(a => a.Course)
            .Where(a => courseIds.Contains(a.CourseId))
            .ToListAsync();

        var myResults = await _context.AssignmentResults
            .Where(r => r.StudentProfileId == profile.Id)
            .ToListAsync();

        var vm = new StudentGradebookViewModel
        {
            StudentProfile = profile,
            Items = assignments.Select(a =>
            {
                var res = myResults.FirstOrDefault(r => r.AssignmentId == a.Id);
                return new StudentGradebookItem
                {
                    Assignment = a,
                    Score = res?.Score,
                    Feedback = res?.Feedback
                };
            }).OrderByDescending(i => i.Assignment.DueDate).ToList()
        };

        return View(vm);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<List<Course>> GetAllowedCourses()
    {
        if (User.IsInRole("Admin"))
            return await _context.Courses.Include(c => c.Branch).ToListAsync();

        var userId = _userManager.GetUserId(User);
        var faculty = await _context.FacultyProfiles.FirstOrDefaultAsync(f => f.IdentityUserId == userId);
        if (faculty == null) return new List<Course>();

        var courseIds = await _context.FacultyCourseAssignments
            .Where(a => a.FacultyProfileId == faculty.Id).Select(a => a.CourseId).ToListAsync();
        return await _context.Courses.Include(c => c.Branch).Where(c => courseIds.Contains(c.Id)).ToListAsync();
    }

    private async Task<SelectList> GetAllowedCoursesSelectList()
    {
        var courses = await GetAllowedCourses();
        return new SelectList(courses, "Id", "Name");
    }

    private async Task<bool> CanManageCourse(int courseId)
    {
        if (User.IsInRole("Admin")) return true;
        var userId = _userManager.GetUserId(User);
        var faculty = await _context.FacultyProfiles.FirstOrDefaultAsync(f => f.IdentityUserId == userId);
        if (faculty == null) return false;
        return await _context.FacultyCourseAssignments
            .AnyAsync(a => a.FacultyProfileId == faculty.Id && a.CourseId == courseId);
    }
}

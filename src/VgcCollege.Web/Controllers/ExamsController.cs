using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Controllers;

[Authorize]
public class ExamsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public ExamsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ─── Exams List ───────────────────────────────────────────────────────────

    [Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> Index(int? courseId)
    {
        var query = _context.Exams
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

        if (courseId.HasValue)
            query = query.Where(e => e.CourseId == courseId);

        ViewBag.Courses = await GetAllowedCoursesSelectList();
        ViewBag.SelectedCourseId = courseId;
        return View(await query.OrderByDescending(e => e.Date).ToListAsync());
    }

    // ─── Create Exam ──────────────────────────────────────────────────────────

    [Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Courses = new SelectList(await GetAllowedCourses(), "Id", "Name");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> Create(Exam exam)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Courses = new SelectList(await GetAllowedCourses(), "Id", "Name");
            return View(exam);
        }
        if (!await CanManageCourse(exam.CourseId)) return Forbid();

        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // ─── Edit Exam ────────────────────────────────────────────────────────────

    [Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> Edit(int id)
    {
        var exam = await _context.Exams.Include(e => e.Course).FirstOrDefaultAsync(e => e.Id == id);
        if (exam == null) return NotFound();
        if (!await CanManageCourse(exam.CourseId)) return Forbid();
        ViewBag.Courses = new SelectList(await GetAllowedCourses(), "Id", "Name");
        return View(exam);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> Edit(int id, Exam exam)
    {
        if (id != exam.Id) return BadRequest();
        if (!await CanManageCourse(exam.CourseId)) return Forbid();
        if (!ModelState.IsValid)
        {
            ViewBag.Courses = new SelectList(await GetAllowedCourses(), "Id", "Name");
            return View(exam);
        }
        _context.Update(exam);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // ─── Release results toggle (Admin only) ─────────────────────────────────

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleRelease(int id)
    {
        var exam = await _context.Exams.FindAsync(id);
        if (exam == null) return NotFound();
        exam.ResultsReleased = !exam.ResultsReleased;
        await _context.SaveChangesAsync();
        TempData["Success"] = exam.ResultsReleased
            ? $"Results for \"{exam.Title}\" are now RELEASED."
            : $"Results for \"{exam.Title}\" are now PROVISIONAL (hidden from students).";
        return RedirectToAction(nameof(Index));
    }

    // ─── Exam Results (Admin/Faculty) ─────────────────────────────────────────

    [Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> ExamResults(int examId)
    {
        var exam = await _context.Exams.Include(e => e.Course).FirstOrDefaultAsync(e => e.Id == examId);
        if (exam == null) return NotFound();
        if (!await CanManageCourse(exam.CourseId)) return Forbid();

        var enrolled = await _context.CourseEnrolments
            .Include(e => e.StudentProfile)
            .Where(e => e.CourseId == exam.CourseId)
            .ToListAsync();

        var results = await _context.ExamResults
            .Where(r => r.ExamId == examId)
            .ToListAsync();

        var vm = new ExamResultsViewModel
        {
            Exam = exam,
            Entries = enrolled.Select(e =>
            {
                var res = results.FirstOrDefault(r => r.StudentProfileId == e.StudentProfileId);
                return new ExamResultEntry
                {
                    StudentProfile = e.StudentProfile,
                    ExistingResultId = res?.Id,
                    Score = res?.Score,
                    Grade = res?.Grade
                };
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Faculty")]
    public async Task<IActionResult> SaveExamResults(int examId, List<ExamResultPost> results)
    {
        var exam = await _context.Exams.Include(e => e.Course).FirstOrDefaultAsync(e => e.Id == examId);
        if (exam == null) return NotFound();
        if (!await CanManageCourse(exam.CourseId)) return Forbid();

        foreach (var post in results.Where(r => r.Score.HasValue))
        {
            if (post.Score < 0 || post.Score > exam.MaxScore)
            {
                TempData["Error"] = $"Score for student {post.StudentProfileId} is out of range (0–{exam.MaxScore}).";
                return RedirectToAction(nameof(ExamResults), new { examId });
            }

            var existing = await _context.ExamResults
                .FirstOrDefaultAsync(r => r.ExamId == examId && r.StudentProfileId == post.StudentProfileId);
            if (existing != null)
            {
                existing.Score = post.Score!.Value;
                existing.Grade = post.Grade;
            }
            else
            {
                _context.ExamResults.Add(new ExamResult
                {
                    ExamId = examId,
                    StudentProfileId = post.StudentProfileId,
                    Score = post.Score!.Value,
                    Grade = post.Grade
                });
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Exam results saved.";
        return RedirectToAction(nameof(ExamResults), new { examId });
    }

    // ─── Student: view own exam results ──────────────────────────────────────

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyExamResults()
    {
        var userId = _userManager.GetUserId(User);
        var profile = await _context.StudentProfiles.FirstOrDefaultAsync(s => s.IdentityUserId == userId);
        if (profile == null) return NotFound();

        var enrolments = await _context.CourseEnrolments
            .Include(e => e.Course)
            .Where(e => e.StudentProfileId == profile.Id)
            .ToListAsync();

        var courseIds = enrolments.Select(e => e.CourseId).ToList();

        // Only show RELEASED exams to students
        var exams = await _context.Exams
            .Include(e => e.Course)
            .Where(e => courseIds.Contains(e.CourseId) && e.ResultsReleased)
            .ToListAsync();

        var myResults = await _context.ExamResults
            .Where(r => r.StudentProfileId == profile.Id)
            .ToListAsync();

        var vm = new StudentExamResultsViewModel
        {
            StudentProfile = profile,
            Items = exams.Select(exam =>
            {
                var res = myResults.FirstOrDefault(r => r.ExamId == exam.Id);
                return new StudentExamResultItem
                {
                    Exam = exam,
                    Score = res?.Score,
                    Grade = res?.Grade
                };
            }).OrderByDescending(i => i.Exam.Date).ToList()
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

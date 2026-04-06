using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VgcCollege.Web.Data;
using VgcCollege.Web.Models;

namespace VgcCollege.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    public AdminController(ApplicationDbContext context) => _context = context;

    public IActionResult Index() => View();

    // Branches
    public async Task<IActionResult> Branches() =>
        View(await _context.Branches.Include(b => b.Courses).ToListAsync());

    public IActionResult CreateBranch() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateBranch(Branch branch)
    {
        if (!ModelState.IsValid) return View(branch);
        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Branches));
    }

    // Courses
    public async Task<IActionResult> Courses() =>
        View(await _context.Courses.Include(c => c.Branch).ToListAsync());

    public async Task<IActionResult> CreateCourse()
    {
        ViewBag.Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "Name");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCourse(Course course)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Branches = new SelectList(await _context.Branches.ToListAsync(), "Id", "Name");
            return View(course);
        }
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Courses));
    }

    // Faculty assignments
    public async Task<IActionResult> FacultyAssignments() =>
        View(await _context.FacultyCourseAssignments
            .Include(a => a.FacultyProfile).Include(a => a.Course).ToListAsync());

    public async Task<IActionResult> AssignFaculty()
    {
        ViewBag.Faculty = new SelectList(await _context.FacultyProfiles.ToListAsync(), "Id", "Name");
        ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignFaculty(FacultyCourseAssignment assignment)
    {
        var exists = await _context.FacultyCourseAssignments
            .AnyAsync(a => a.FacultyProfileId == assignment.FacultyProfileId && a.CourseId == assignment.CourseId);
        if (exists) ModelState.AddModelError("", "Already assigned.");
        if (!ModelState.IsValid)
        {
            ViewBag.Faculty = new SelectList(await _context.FacultyProfiles.ToListAsync(), "Id", "Name");
            ViewBag.Courses = new SelectList(await _context.Courses.ToListAsync(), "Id", "Name");
            return View(assignment);
        }
        _context.FacultyCourseAssignments.Add(assignment);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(FacultyAssignments));
    }
}

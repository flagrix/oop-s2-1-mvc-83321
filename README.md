
## How to run locally

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Git

### Steps

```bash
# Clone the repo
git clone https://github.com/<your-username>/oop-s2-1-mvc-83321.git
cd oop-s2-1-mvc-<student-number>

# Restore packages
dotnet restore

# Run the application (database is created + seeded automatically on first launch)
cd src/VgcCollege.Web
dotnet run
```

Then open: **https://localhost:5001** or **http://localhost:5000**

> The SQLite database (`vgc.db`) is created automatically in the project folder on first run. No manual migration steps needed — `DbSeeder.SeedAsync()` runs at startup and is fully idempotent.

---

## Seeded Demo Accounts

| Role    | Email                          | Password     |
|---------|-------------------------------|--------------|
| Admin   | admin@vgc.ie                  | Admin@123!   |
| Faculty | john.smith@vgc.ie             | Faculty@123! |
| Faculty | mary.jones@vgc.ie             | Faculty@123! |
| Student | alice.murphy@student.vgc.ie   | Student@123! |
| Student | bob.kelly@student.vgc.ie      | Student@123! |

---

## How to run tests

```bash
# From the repo root
dotnet test --verbosity normal
```

Tests are in `tests/VgcCollege.Tests/` and use the EF Core **InMemory** provider — no database setup required.

### Test coverage (19 tests across 3 files)

**`EnrolmentServiceTests.cs`** (9 tests)
- Duplicate enrolment prevention
- Multi-course enrolment allowed
- Enrolment status defaults to Active
- Attendance percentage calculation (normal + zero records)
- Exam result hidden when not released / visible when released
- Assignment score validation (valid, negative)
- Faculty data access filtering (only sees their students)

**`GradebookServiceTests.cs`** (7 tests)
- Assignment score within range / exceeds max / zero score
- Percentage calculation (whole numbers, fractions)
- Student cannot see unreleased exam results
- Student can see released exam results
- ResultsReleased toggle changes flag
- Exam result created with grade
- Assignment result can be updated (upsert logic)
- Faculty only sees assignments for their courses

**`AuthorizationQueryTests.cs`** (6 tests)
- Student cannot access another student's profile
- Faculty cannot see exams outside their courses
- Student cannot see another student's assignment results
- Admin sees all students across branches
- Enrolment status can be changed to Withdrawn
- Exam result is unique per student/exam (upsert, not duplicate)
- Attendance multi-week storage

**`ValidationTests.cs`** (13 tests)
- StudentProfile, Assignment, Exam, Branch, Course — valid model passes, missing required fields fail
- Score boundary checks via `[Theory]` with `[InlineData]`
- Grade string values are valid
- Exam.ResultsReleased defaults to false

---

## Project Structure

```
VgcCollege/
├── src/
│   └── VgcCollege.Web/               # ASP.NET Core MVC application
│       ├── Controllers/
│       │   ├── AdminController.cs        # Branch, Course, FacultyAssignment management
│       │   ├── AttendanceController.cs   # Attendance CRUD
│       │   ├── EnrolmentsController.cs   # Enrolment CRUD
│       │   ├── ExamsController.cs        # Exams + results + release toggle
│       │   ├── GradebookController.cs    # Assignments + results + student view
│       │   ├── HomeController.cs         # Dashboard + error pages
│       │   └── StudentsController.cs     # Student profile CRUD
│       ├── Data/
│       │   ├── ApplicationDbContext.cs
│       │   ├── DbSeeder.cs               # Idempotent seed: branches, courses, users, results
│       │   └── Migrations/
│       ├── Models/
│       │   ├── Branch.cs, Course.cs, StudentProfile.cs, FacultyProfile.cs
│       │   ├── CourseEnrolment.cs, AttendanceRecord.cs
│       │   ├── Assignment.cs, AssignmentResult.cs
│       │   ├── Exam.cs, ExamResult.cs
│       │   ├── FacultyCourseAssignment.cs
│       │   ├── GradebookViewModels.cs    # ViewModels for Gradebook + Exams
│       │   └── ErrorViewModel.cs
│       └── Views/
│           ├── Admin/        Branches, Courses, FacultyAssignments
│           ├── Attendance/   Index, Create, Edit, MyAttendance
│           ├── Enrolments/   Index, Create, Edit, MyEnrolments
│           ├── Exams/        Index, Create, Edit, ExamResults, MyExamResults
│           ├── Gradebook/    Assignments, CreateAssignment, EditAssignment,
│           │                 AssignmentResults, MyGradebook
│           ├── Home/         Index (role dashboards), Privacy, NotFound
│           ├── Students/     Index, Create, Edit, Details
│           └── Shared/       _Layout, _LoginPartial, Error, AccessDenied
├── tests/
│   └── VgcCollege.Tests/
│       ├── EnrolmentServiceTests.cs
│       ├── GradebookServiceTests.cs
│       ├── AuthorizationQueryTests.cs
│       └── ValidationTests.cs
├── .github/
│   └── workflows/ci.yml              # Restore → Build (Release) → Test
└── README.md
```

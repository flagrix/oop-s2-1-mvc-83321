using System.ComponentModel.DataAnnotations;
using VgcCollege.Web.Models;
using Xunit;

namespace VgcCollege.Tests;

// ─── Branch Tests ─────────────────────────────────────────────────────────────

public class BranchTests
{
    [Fact]
    public void Branch_DefaultValues_AreCorrect()
    {
        var branch = new Branch();
        Assert.Equal(0, branch.Id);
        Assert.Equal(string.Empty, branch.Name);
        Assert.Equal(string.Empty, branch.Address);
        Assert.NotNull(branch.Courses);
        Assert.Empty(branch.Courses);
    }

    [Fact]
    public void Branch_SetProperties_WorkCorrectly()
    {
        var branch = new Branch { Id = 1, Name = "Science", Address = "123 Main St" };
        Assert.Equal(1, branch.Id);
        Assert.Equal("Science", branch.Name);
        Assert.Equal("123 Main St", branch.Address);
    }

    [Fact]
    public void Branch_NameRequired_ValidationFails_WhenEmpty()
    {
        var branch = new Branch { Name = "", Address = "123 St" };
        var results = ModelValidator.ValidateModel(branch);
        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Branch_AddressRequired_ValidationFails_WhenEmpty()
    {
        var branch = new Branch { Name = "Science", Address = "" };
        var results = ModelValidator.ValidateModel(branch);
        Assert.Contains(results, r => r.MemberNames.Contains("Address"));
    }

    [Fact]
    public void Branch_NameTooLong_ValidationFails()
    {
        var branch = new Branch { Name = new string('A', 101), Address = "Valid Address" };
        var results = ModelValidator.ValidateModel(branch);
        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Branch_CanAddCourses()
    {
        var branch = new Branch { Name = "Science", Address = "123 St" };
        branch.Courses.Add(new Course { Name = "Math" });
        Assert.Single(branch.Courses);
    }
}

// ─── Course Tests ─────────────────────────────────────────────────────────────

public class CourseTests
{
    [Fact]
    public void Course_DefaultValues_AreCorrect()
    {
        var course = new Course();
        Assert.Equal(0, course.Id);
        Assert.Equal(string.Empty, course.Name);
        Assert.NotNull(course.Enrolments);
        Assert.NotNull(course.FacultyAssignments);
        Assert.NotNull(course.Assignments);
        Assert.NotNull(course.Exams);
    }

    [Fact]
    public void Course_SetProperties_WorkCorrectly()
    {
        var start = new DateTime(2024, 1, 1);
        var end = new DateTime(2024, 6, 30);
        var course = new Course { Id = 1, Name = "Mathematics", BranchId = 2, StartDate = start, EndDate = end };

        Assert.Equal(1, course.Id);
        Assert.Equal("Mathematics", course.Name);
        Assert.Equal(2, course.BranchId);
        Assert.Equal(start, course.StartDate);
        Assert.Equal(end, course.EndDate);
    }

    [Fact]
    public void Course_NameRequired_ValidationFails_WhenEmpty()
    {
        var course = new Course { Name = "" };
        var results = ModelValidator.ValidateModel(course);
        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Course_NameTooLong_ValidationFails()
    {
        var course = new Course { Name = new string('A', 151) };
        var results = ModelValidator.ValidateModel(course);
        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Course_ValidName_PassesValidation()
    {
        var course = new Course { Name = "Physics" };
        var results = ModelValidator.ValidateModel(course);
        Assert.DoesNotContain(results, r => r.MemberNames.Contains("Name"));
    }
}

// ─── StudentProfile Tests ─────────────────────────────────────────────────────

public class StudentProfileTests
{
    [Fact]
    public void StudentProfile_DefaultValues_AreCorrect()
    {
        var profile = new StudentProfile();
        Assert.Equal(0, profile.Id);
        Assert.Equal(string.Empty, profile.Name);
        Assert.Equal(string.Empty, profile.Email);
        Assert.Equal(string.Empty, profile.StudentNumber);
        Assert.Null(profile.Phone);
        Assert.Null(profile.Address);
        Assert.Null(profile.DateOfBirth);
        Assert.NotNull(profile.Enrolments);
        Assert.NotNull(profile.AssignmentResults);
        Assert.NotNull(profile.ExamResults);
    }

    [Fact]
    public void StudentProfile_SetProperties_WorkCorrectly()
    {
        var dob = new DateTime(2000, 5, 15);
        var profile = new StudentProfile
        {
            Id = 1,
            IdentityUserId = "uid1",
            Name = "Alice Smith",
            Email = "alice@test.com",
            Phone = "0851234567",
            Address = "1 College Rd",
            DateOfBirth = dob,
            StudentNumber = "S001"
        };

        Assert.Equal(1, profile.Id);
        Assert.Equal("uid1", profile.IdentityUserId);
        Assert.Equal("Alice Smith", profile.Name);
        Assert.Equal("alice@test.com", profile.Email);
        Assert.Equal("0851234567", profile.Phone);
        Assert.Equal("1 College Rd", profile.Address);
        Assert.Equal(dob, profile.DateOfBirth);
        Assert.Equal("S001", profile.StudentNumber);
    }

    [Fact]
    public void StudentProfile_NameRequired_ValidationFails_WhenEmpty()
    {
        var profile = new StudentProfile { Name = "", Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        var results = ModelValidator.ValidateModel(profile);
        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void StudentProfile_InvalidEmail_ValidationFails()
    {
        var profile = new StudentProfile { Name = "Alice", Email = "not-an-email", StudentNumber = "S1", IdentityUserId = "u1" };
        var results = ModelValidator.ValidateModel(profile);
        Assert.Contains(results, r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void StudentProfile_ValidEmail_PassesValidation()
    {
        var profile = new StudentProfile { Name = "Alice", Email = "alice@college.ie", StudentNumber = "S1", IdentityUserId = "u1" };
        var results = ModelValidator.ValidateModel(profile);
        Assert.DoesNotContain(results, r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void StudentProfile_StudentNumberTooLong_ValidationFails()
    {
        var profile = new StudentProfile { Name = "Alice", Email = "a@b.com", StudentNumber = new string('S', 21), IdentityUserId = "u1" };
        var results = ModelValidator.ValidateModel(profile);
        Assert.Contains(results, r => r.MemberNames.Contains("StudentNumber"));
    }

    [Fact]
    public void StudentProfile_NameTooLong_ValidationFails()
    {
        var profile = new StudentProfile { Name = new string('A', 101), Email = "a@b.com", StudentNumber = "S1", IdentityUserId = "u1" };
        var results = ModelValidator.ValidateModel(profile);
        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }
}

// ─── FacultyProfile Tests ─────────────────────────────────────────────────────

public class FacultyProfileTests
{
    [Fact]
    public void FacultyProfile_DefaultValues_AreCorrect()
    {
        var faculty = new FacultyProfile();
        Assert.Equal(0, faculty.Id);
        Assert.Equal(string.Empty, faculty.Name);
        Assert.Equal(string.Empty, faculty.Email);
        Assert.Null(faculty.Phone);
        Assert.NotNull(faculty.CourseAssignments);
        Assert.Empty(faculty.CourseAssignments);
    }

    [Fact]
    public void FacultyProfile_SetProperties_WorkCorrectly()
    {
        var faculty = new FacultyProfile
        {
            Id = 1,
            IdentityUserId = "fuid1",
            Name = "Dr. Brown",
            Email = "brown@college.ie",
            Phone = "0861234567"
        };

        Assert.Equal(1, faculty.Id);
        Assert.Equal("fuid1", faculty.IdentityUserId);
        Assert.Equal("Dr. Brown", faculty.Name);
        Assert.Equal("brown@college.ie", faculty.Email);
        Assert.Equal("0861234567", faculty.Phone);
    }

    [Fact]
    public void FacultyProfile_InvalidEmail_ValidationFails()
    {
        var faculty = new FacultyProfile { Name = "Dr. Brown", Email = "invalid", IdentityUserId = "u1" };
        var results = ModelValidator.ValidateModel(faculty);
        Assert.Contains(results, r => r.MemberNames.Contains("Email"));
    }

    [Fact]
    public void FacultyProfile_NameTooLong_ValidationFails()
    {
        var faculty = new FacultyProfile { Name = new string('A', 101), Email = "a@b.com", IdentityUserId = "u1" };
        var results = ModelValidator.ValidateModel(faculty);
        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void FacultyProfile_ValidModel_PassesValidation()
    {
        var faculty = new FacultyProfile { Name = "Dr. Green", Email = "green@college.ie", IdentityUserId = "u2" };
        var results = ModelValidator.ValidateModel(faculty);
        Assert.Empty(results);
    }
}

// ─── CourseEnrolment Tests ────────────────────────────────────────────────────

public class CourseEnrolmentTests
{
    [Fact]
    public void CourseEnrolment_DefaultStatus_IsActive()
    {
        var enrolment = new CourseEnrolment();
        Assert.Equal(EnrolmentStatus.Active, enrolment.Status);
    }

    [Fact]
    public void CourseEnrolment_DefaultValues_AreCorrect()
    {
        var enrolment = new CourseEnrolment();
        Assert.Equal(0, enrolment.Id);
        Assert.NotNull(enrolment.AttendanceRecords);
        Assert.Empty(enrolment.AttendanceRecords);
    }

    [Fact]
    public void CourseEnrolment_SetProperties_WorkCorrectly()
    {
        var date = new DateTime(2024, 9, 1);
        var enrolment = new CourseEnrolment
        {
            Id = 1,
            StudentProfileId = 2,
            CourseId = 3,
            EnrolDate = date,
            Status = EnrolmentStatus.Completed
        };

        Assert.Equal(1, enrolment.Id);
        Assert.Equal(2, enrolment.StudentProfileId);
        Assert.Equal(3, enrolment.CourseId);
        Assert.Equal(date, enrolment.EnrolDate);
        Assert.Equal(EnrolmentStatus.Completed, enrolment.Status);
    }

    [Fact]
    public void EnrolmentStatus_AllValues_Exist()
    {
        Assert.True(Enum.IsDefined(typeof(EnrolmentStatus), EnrolmentStatus.Active));
        Assert.True(Enum.IsDefined(typeof(EnrolmentStatus), EnrolmentStatus.Withdrawn));
        Assert.True(Enum.IsDefined(typeof(EnrolmentStatus), EnrolmentStatus.Completed));
    }

    [Fact]
    public void CourseEnrolment_CanAddAttendanceRecords()
    {
        var enrolment = new CourseEnrolment();
        enrolment.AttendanceRecords.Add(new AttendanceRecord { WeekNumber = 1, Present = true });
        Assert.Single(enrolment.AttendanceRecords);
    }
}

// ─── AttendanceRecord Tests ───────────────────────────────────────────────────

public class AttendanceRecordTests
{
    [Fact]
    public void AttendanceRecord_DefaultValues_AreCorrect()
    {
        var record = new AttendanceRecord();
        Assert.Equal(0, record.Id);
        Assert.Equal(0, record.WeekNumber);
        Assert.False(record.Present);
    }

    [Fact]
    public void AttendanceRecord_SetProperties_WorkCorrectly()
    {
        var date = new DateTime(2024, 10, 7);
        var record = new AttendanceRecord
        {
            Id = 1,
            CourseEnrolmentId = 2,
            WeekNumber = 5,
            Date = date,
            Present = true
        };

        Assert.Equal(1, record.Id);
        Assert.Equal(2, record.CourseEnrolmentId);
        Assert.Equal(5, record.WeekNumber);
        Assert.Equal(date, record.Date);
        Assert.True(record.Present);
    }

    [Fact]
    public void AttendanceRecord_Absent_SetsPresentFalse()
    {
        var record = new AttendanceRecord { Present = false };
        Assert.False(record.Present);
    }
}

// ─── Assignment Tests ─────────────────────────────────────────────────────────

public class AssignmentTests
{
    [Fact]
    public void Assignment_DefaultValues_AreCorrect()
    {
        var assignment = new Assignment();
        Assert.Equal(0, assignment.Id);
        Assert.Equal(string.Empty, assignment.Title);
        Assert.Equal(0, assignment.MaxScore);
        Assert.NotNull(assignment.Results);
        Assert.Empty(assignment.Results);
    }

    [Fact]
    public void Assignment_SetProperties_WorkCorrectly()
    {
        var due = new DateTime(2024, 11, 30);
        var assignment = new Assignment
        {
            Id = 1,
            CourseId = 2,
            Title = "Essay 1",
            MaxScore = 100,
            DueDate = due
        };

        Assert.Equal(1, assignment.Id);
        Assert.Equal(2, assignment.CourseId);
        Assert.Equal("Essay 1", assignment.Title);
        Assert.Equal(100, assignment.MaxScore);
        Assert.Equal(due, assignment.DueDate);
    }

    [Fact]
    public void Assignment_TitleRequired_ValidationFails_WhenEmpty()
    {
        var assignment = new Assignment { Title = "" };
        var results = ModelValidator.ValidateModel(assignment);
        Assert.Contains(results, r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void Assignment_TitleTooLong_ValidationFails()
    {
        var assignment = new Assignment { Title = new string('A', 201) };
        var results = ModelValidator.ValidateModel(assignment);
        Assert.Contains(results, r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void Assignment_ValidTitle_PassesValidation()
    {
        var assignment = new Assignment { Title = "Midterm Essay" };
        var results = ModelValidator.ValidateModel(assignment);
        Assert.DoesNotContain(results, r => r.MemberNames.Contains("Title"));
    }
}

// ─── AssignmentResult Tests ───────────────────────────────────────────────────

public class AssignmentResultTests
{
    [Fact]
    public void AssignmentResult_DefaultValues_AreCorrect()
    {
        var result = new AssignmentResult();
        Assert.Equal(0, result.Id);
        Assert.Equal(0, result.Score);
        Assert.Null(result.Feedback);
    }

    [Fact]
    public void AssignmentResult_SetProperties_WorkCorrectly()
    {
        var result = new AssignmentResult
        {
            Id = 1,
            AssignmentId = 2,
            StudentProfileId = 3,
            Score = 85.5m,
            Feedback = "Good work!"
        };

        Assert.Equal(1, result.Id);
        Assert.Equal(2, result.AssignmentId);
        Assert.Equal(3, result.StudentProfileId);
        Assert.Equal(85.5m, result.Score);
        Assert.Equal("Good work!", result.Feedback);
    }

    [Fact]
    public void AssignmentResult_FeedbackTooLong_ValidationFails()
    {
        var result = new AssignmentResult { Score = 50, Feedback = new string('A', 501) };
        var results = ModelValidator.ValidateModel(result);
        Assert.Contains(results, r => r.MemberNames.Contains("Feedback"));
    }

    [Fact]
    public void AssignmentResult_NullFeedback_PassesValidation()
    {
        var result = new AssignmentResult { Score = 50, Feedback = null };
        var results = ModelValidator.ValidateModel(result);
        Assert.DoesNotContain(results, r => r.MemberNames.Contains("Feedback"));
    }
}

// ─── Exam Tests ───────────────────────────────────────────────────────────────

public class ExamTests
{
    [Fact]
    public void Exam_DefaultValues_AreCorrect()
    {
        var exam = new Exam();
        Assert.Equal(0, exam.Id);
        Assert.Equal(string.Empty, exam.Title);
        Assert.Equal(0, exam.MaxScore);
        Assert.False(exam.ResultsReleased);
        Assert.NotNull(exam.Results);
        Assert.Empty(exam.Results);
    }

    [Fact]
    public void Exam_SetProperties_WorkCorrectly()
    {
        var date = new DateTime(2024, 12, 15);
        var exam = new Exam
        {
            Id = 1,
            CourseId = 2,
            Title = "Final Exam",
            Date = date,
            MaxScore = 200,
            ResultsReleased = true
        };

        Assert.Equal(1, exam.Id);
        Assert.Equal(2, exam.CourseId);
        Assert.Equal("Final Exam", exam.Title);
        Assert.Equal(date, exam.Date);
        Assert.Equal(200, exam.MaxScore);
        Assert.True(exam.ResultsReleased);
    }

    [Fact]
    public void Exam_TitleRequired_ValidationFails_WhenEmpty()
    {
        var exam = new Exam { Title = "" };
        var results = ModelValidator.ValidateModel(exam);
        Assert.Contains(results, r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void Exam_TitleTooLong_ValidationFails()
    {
        var exam = new Exam { Title = new string('A', 201) };
        var results = ModelValidator.ValidateModel(exam);
        Assert.Contains(results, r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void Exam_ToggleResultsReleased_Works()
    {
        var exam = new Exam { ResultsReleased = false };
        exam.ResultsReleased = !exam.ResultsReleased;
        Assert.True(exam.ResultsReleased);
        exam.ResultsReleased = !exam.ResultsReleased;
        Assert.False(exam.ResultsReleased);
    }
}

// ─── ExamResult Tests ─────────────────────────────────────────────────────────

public class ExamResultTests
{
    [Fact]
    public void ExamResult_DefaultValues_AreCorrect()
    {
        var result = new ExamResult();
        Assert.Equal(0, result.Id);
        Assert.Equal(0, result.Score);
        Assert.Null(result.Grade);
    }

    [Fact]
    public void ExamResult_SetProperties_WorkCorrectly()
    {
        var result = new ExamResult
        {
            Id = 1,
            ExamId = 2,
            StudentProfileId = 3,
            Score = 92,
            Grade = "A"
        };

        Assert.Equal(1, result.Id);
        Assert.Equal(2, result.ExamId);
        Assert.Equal(3, result.StudentProfileId);
        Assert.Equal(92, result.Score);
        Assert.Equal("A", result.Grade);
    }

    [Fact]
    public void ExamResult_GradeTooLong_ValidationFails()
    {
        var result = new ExamResult { Score = 90, Grade = "A+++++" };
        var results = ModelValidator.ValidateModel(result);
        Assert.Contains(results, r => r.MemberNames.Contains("Grade"));
    }

    [Fact]
    public void ExamResult_NullGrade_PassesValidation()
    {
        var result = new ExamResult { Score = 90, Grade = null };
        var results = ModelValidator.ValidateModel(result);
        Assert.DoesNotContain(results, r => r.MemberNames.Contains("Grade"));
    }
}

// ─── FacultyCourseAssignment Tests ────────────────────────────────────────────

public class FacultyCourseAssignmentTests
{
    [Fact]
    public void FacultyCourseAssignment_DefaultValues_AreCorrect()
    {
        var assignment = new FacultyCourseAssignment();
        Assert.Equal(0, assignment.Id);
        Assert.Equal(0, assignment.FacultyProfileId);
        Assert.Equal(0, assignment.CourseId);
    }

    [Fact]
    public void FacultyCourseAssignment_SetProperties_WorkCorrectly()
    {
        var assignment = new FacultyCourseAssignment
        {
            Id = 1,
            FacultyProfileId = 2,
            CourseId = 3
        };

        Assert.Equal(1, assignment.Id);
        Assert.Equal(2, assignment.FacultyProfileId);
        Assert.Equal(3, assignment.CourseId);
    }
}

// ─── ErrorViewModel Tests ─────────────────────────────────────────────────────

public class ErrorViewModelTests
{
    [Fact]
    public void ShowRequestId_ReturnsFalse_WhenRequestIdIsNull()
    {
        var vm = new ErrorViewModel { RequestId = null };
        Assert.False(vm.ShowRequestId);
    }

    [Fact]
    public void ShowRequestId_ReturnsFalse_WhenRequestIdIsEmpty()
    {
        var vm = new ErrorViewModel { RequestId = string.Empty };
        Assert.False(vm.ShowRequestId);
    }

    [Fact]
    public void ShowRequestId_ReturnsTrue_WhenRequestIdHasValue()
    {
        var vm = new ErrorViewModel { RequestId = "abc-123" };
        Assert.True(vm.ShowRequestId);
    }
}

// ─── GradebookViewModels Tests ────────────────────────────────────────────────

public class StudentGradebookItemTests
{
    [Fact]
    public void Percentage_ReturnsNull_WhenScoreIsNull()
    {
        var item = new StudentGradebookItem
        {
            Assignment = new Assignment { MaxScore = 100 },
            Score = null
        };
        Assert.Null(item.Percentage);
    }

    [Fact]
    public void Percentage_ReturnsNull_WhenMaxScoreIsZero()
    {
        var item = new StudentGradebookItem
        {
            Assignment = new Assignment { MaxScore = 0 },
            Score = 50
        };
        Assert.Null(item.Percentage);
    }

    [Fact]
    public void Percentage_ReturnsCorrectValue()
    {
        var item = new StudentGradebookItem
        {
            Assignment = new Assignment { MaxScore = 100 },
            Score = 75
        };
        Assert.Equal(75.0m, item.Percentage);
    }

    [Fact]
    public void Percentage_RoundsToOneDecimal()
    {
        var item = new StudentGradebookItem
        {
            Assignment = new Assignment { MaxScore = 3 },
            Score = 1
        };
        Assert.Equal(33.3m, item.Percentage);
    }

    [Fact]
    public void Percentage_ReturnsHundred_WhenPerfectScore()
    {
        var item = new StudentGradebookItem
        {
            Assignment = new Assignment { MaxScore = 50 },
            Score = 50
        };
        Assert.Equal(100.0m, item.Percentage);
    }
}

public class StudentGradebookViewModelTests
{
    [Fact]
    public void AverageScore_ReturnsNull_WhenNoItems()
    {
        var vm = new StudentGradebookViewModel
        {
            StudentProfile = new StudentProfile(),
            Items = new List<StudentGradebookItem>()
        };
        Assert.Null(vm.AverageScore);
    }

    [Fact]
    public void AverageScore_ReturnsNull_WhenNoScores()
    {
        var vm = new StudentGradebookViewModel
        {
            StudentProfile = new StudentProfile(),
            Items = new List<StudentGradebookItem>
            {
                new StudentGradebookItem { Assignment = new Assignment { MaxScore = 100 }, Score = null }
            }
        };
        Assert.Null(vm.AverageScore);
    }

    [Fact]
    public void AverageScore_ReturnsCorrectAverage()
    {
        var vm = new StudentGradebookViewModel
        {
            StudentProfile = new StudentProfile(),
            Items = new List<StudentGradebookItem>
            {
                new StudentGradebookItem { Assignment = new Assignment { MaxScore = 100 }, Score = 80 },
                new StudentGradebookItem { Assignment = new Assignment { MaxScore = 100 }, Score = 60 }
            }
        };
        Assert.Equal(70.0m, vm.AverageScore);
    }

    [Fact]
    public void AverageScore_IgnoresItemsWithNullScore()
    {
        var vm = new StudentGradebookViewModel
        {
            StudentProfile = new StudentProfile(),
            Items = new List<StudentGradebookItem>
            {
                new StudentGradebookItem { Assignment = new Assignment { MaxScore = 100 }, Score = 80 },
                new StudentGradebookItem { Assignment = new Assignment { MaxScore = 100 }, Score = null }
            }
        };
        Assert.Equal(80.0m, vm.AverageScore);
    }
}

public class StudentExamResultItemTests
{
    [Fact]
    public void Percentage_ReturnsNull_WhenScoreIsNull()
    {
        var item = new StudentExamResultItem
        {
            Exam = new Exam { MaxScore = 100 },
            Score = null
        };
        Assert.Null(item.Percentage);
    }

    [Fact]
    public void Percentage_ReturnsNull_WhenMaxScoreIsZero()
    {
        var item = new StudentExamResultItem
        {
            Exam = new Exam { MaxScore = 0 },
            Score = 50
        };
        Assert.Null(item.Percentage);
    }

    [Fact]
    public void Percentage_ReturnsCorrectValue()
    {
        var item = new StudentExamResultItem
        {
            Exam = new Exam { MaxScore = 200 },
            Score = 150
        };
        Assert.Equal(75.0m, item.Percentage);
    }

    [Fact]
    public void Percentage_RoundsToOneDecimal()
    {
        var item = new StudentExamResultItem
        {
            Exam = new Exam { MaxScore = 3 },
            Score = 2
        };
        Assert.Equal(66.7m, item.Percentage);
    }
}

public class GradebookViewModelTests
{
    [Fact]
    public void AssignmentResultsViewModel_DefaultValues_AreCorrect()
    {
        var vm = new AssignmentResultsViewModel();
        Assert.NotNull(vm.Entries);
        Assert.Empty(vm.Entries);
    }

    [Fact]
    public void ExamResultsViewModel_DefaultValues_AreCorrect()
    {
        var vm = new ExamResultsViewModel();
        Assert.NotNull(vm.Entries);
        Assert.Empty(vm.Entries);
    }

    [Fact]
    public void AssignmentResultEntry_DefaultValues_AreCorrect()
    {
        var entry = new AssignmentResultEntry();
        Assert.Null(entry.ExistingResultId);
        Assert.Null(entry.Score);
        Assert.Null(entry.Feedback);
    }

    [Fact]
    public void ExamResultEntry_DefaultValues_AreCorrect()
    {
        var entry = new ExamResultEntry();
        Assert.Null(entry.ExistingResultId);
        Assert.Null(entry.Score);
        Assert.Null(entry.Grade);
    }

    [Fact]
    public void AssignmentResultPost_SetProperties_WorkCorrectly()
    {
        var post = new AssignmentResultPost
        {
            StudentProfileId = 1,
            Score = 88.5m,
            Feedback = "Well done"
        };
        Assert.Equal(1, post.StudentProfileId);
        Assert.Equal(88.5m, post.Score);
        Assert.Equal("Well done", post.Feedback);
    }

    [Fact]
    public void ExamResultPost_SetProperties_WorkCorrectly()
    {
        var post = new ExamResultPost
        {
            StudentProfileId = 2,
            Score = 95m,
            Grade = "A"
        };
        Assert.Equal(2, post.StudentProfileId);
        Assert.Equal(95m, post.Score);
        Assert.Equal("A", post.Grade);
    }

    [Fact]
    public void StudentExamResultsViewModel_DefaultValues_AreCorrect()
    {
        var vm = new StudentExamResultsViewModel();
        Assert.NotNull(vm.Items);
        Assert.Empty(vm.Items);
    }
}

// ─── Helper ───────────────────────────────────────────────────────────────────

public static class ModelValidator
{
    public static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var ctx = new ValidationContext(model);
        Validator.TryValidateObject(model, ctx, results, true);
        return results;
    }
}

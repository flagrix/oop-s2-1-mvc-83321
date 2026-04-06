using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VgcCollege.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ASP.NET Core Identity tables
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_AspNetRoles", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: true),
                    SecurityStamp = table.Column<string>(type: "TEXT", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_AspNetUsers", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey("FK_AspNetRoleClaims_AspNetRoles_RoleId", x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimType = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey("FK_AspNetUserClaims_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey("FK_AspNetUserLogins_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    RoleId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey("FK_AspNetUserRoles_AspNetRoles_RoleId", x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_AspNetUserRoles_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    LoginProvider = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey("FK_AspNetUserTokens_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            // Identity indexes
            migrationBuilder.CreateIndex("IX_AspNetRoleClaims_RoleId", "AspNetRoleClaims", "RoleId");
            migrationBuilder.CreateIndex("RoleNameIndex", "AspNetRoles", "NormalizedName", unique: true);
            migrationBuilder.CreateIndex("IX_AspNetUserClaims_UserId", "AspNetUserClaims", "UserId");
            migrationBuilder.CreateIndex("IX_AspNetUserLogins_UserId", "AspNetUserLogins", "UserId");
            migrationBuilder.CreateIndex("IX_AspNetUserRoles_RoleId", "AspNetUserRoles", "RoleId");
            migrationBuilder.CreateIndex("EmailIndex", "AspNetUsers", "NormalizedEmail");
            migrationBuilder.CreateIndex("UserNameIndex", "AspNetUsers", "NormalizedUserName", unique: true);

            // VGC domain tables
            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Branches", x => x.Id));

            migrationBuilder.CreateTable(
                name: "StudentProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    IdentityUserId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: true),
                    StudentNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_StudentProfiles", x => x.Id));

            migrationBuilder.CreateTable(
                name: "FacultyProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    IdentityUserId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_FacultyProfiles", x => x.Id));

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    BranchId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey("FK_Courses_Branches_BranchId", x => x.BranchId, "Branches", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FacultyCourseAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    FacultyProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyCourseAssignments", x => x.Id);
                    table.ForeignKey("FK_FacultyCourseAssignments_FacultyProfiles_FacultyProfileId", x => x.FacultyProfileId, "FacultyProfiles", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_FacultyCourseAssignments_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MaxScore = table.Column<decimal>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assignments", x => x.Id);
                    table.ForeignKey("FK_Assignments_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    MaxScore = table.Column<decimal>(type: "TEXT", nullable: false),
                    ResultsReleased = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.Id);
                    table.ForeignKey("FK_Exams_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseEnrolments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    StudentProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: false),
                    EnrolDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEnrolments", x => x.Id);
                    table.ForeignKey("FK_CourseEnrolments_StudentProfiles_StudentProfileId", x => x.StudentProfileId, "StudentProfiles", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_CourseEnrolments_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    CourseEnrolmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    WeekNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Present = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey("FK_AttendanceRecords_CourseEnrolments_CourseEnrolmentId", x => x.CourseEnrolmentId, "CourseEnrolments", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    AssignmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<decimal>(type: "TEXT", nullable: false),
                    Feedback = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentResults", x => x.Id);
                    table.ForeignKey("FK_AssignmentResults_Assignments_AssignmentId", x => x.AssignmentId, "Assignments", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_AssignmentResults_StudentProfiles_StudentProfileId", x => x.StudentProfileId, "StudentProfiles", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false).Annotation("Sqlite:Autoincrement", true),
                    ExamId = table.Column<int>(type: "INTEGER", nullable: false),
                    StudentProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Score = table.Column<decimal>(type: "TEXT", nullable: false),
                    Grade = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamResults", x => x.Id);
                    table.ForeignKey("FK_ExamResults_Exams_ExamId", x => x.ExamId, "Exams", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_ExamResults_StudentProfiles_StudentProfileId", x => x.StudentProfileId, "StudentProfiles", "Id", onDelete: ReferentialAction.Cascade);
                });

            // Domain indexes
            migrationBuilder.CreateIndex("IX_Courses_BranchId", "Courses", "BranchId");
            migrationBuilder.CreateIndex("IX_Assignments_CourseId", "Assignments", "CourseId");
            migrationBuilder.CreateIndex("IX_Exams_CourseId", "Exams", "CourseId");
            migrationBuilder.CreateIndex("IX_FacultyCourseAssignments_Unique", "FacultyCourseAssignments", new[] { "FacultyProfileId", "CourseId" }, unique: true);
            migrationBuilder.CreateIndex("IX_CourseEnrolments_Unique", "CourseEnrolments", new[] { "StudentProfileId", "CourseId" }, unique: true);
            migrationBuilder.CreateIndex("IX_AssignmentResults_Unique", "AssignmentResults", new[] { "AssignmentId", "StudentProfileId" }, unique: true);
            migrationBuilder.CreateIndex("IX_ExamResults_Unique", "ExamResults", new[] { "ExamId", "StudentProfileId" }, unique: true);
            migrationBuilder.CreateIndex("IX_AttendanceRecords_CourseEnrolmentId", "AttendanceRecords", "CourseEnrolmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("AttendanceRecords");
            migrationBuilder.DropTable("AssignmentResults");
            migrationBuilder.DropTable("ExamResults");
            migrationBuilder.DropTable("CourseEnrolments");
            migrationBuilder.DropTable("FacultyCourseAssignments");
            migrationBuilder.DropTable("Assignments");
            migrationBuilder.DropTable("Exams");
            migrationBuilder.DropTable("FacultyProfiles");
            migrationBuilder.DropTable("StudentProfiles");
            migrationBuilder.DropTable("Courses");
            migrationBuilder.DropTable("Branches");
            migrationBuilder.DropTable("AspNetUserTokens");
            migrationBuilder.DropTable("AspNetUserRoles");
            migrationBuilder.DropTable("AspNetUserLogins");
            migrationBuilder.DropTable("AspNetUserClaims");
            migrationBuilder.DropTable("AspNetRoleClaims");
            migrationBuilder.DropTable("AspNetUsers");
            migrationBuilder.DropTable("AspNetRoles");
        }
    }
}

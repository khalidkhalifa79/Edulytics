using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase13PostgreSqlBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SchoolCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedSchoolCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    City = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DefaultCulture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcademicYears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYears", x => x.Id);
                    table.UniqueConstraint("AK_AcademicYears_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_AcademicYears_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumFrameworks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerSchoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    ProviderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumFrameworks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumFrameworks_Schools_OwnerSchoolId",
                        column: x => x.OwnerSchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GradeLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeLevels", x => x.Id);
                    table.UniqueConstraint("AK_GradeLevels_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_GradeLevels_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvailableAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboxMessages_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                    table.UniqueConstraint("AK_Subjects_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_Subjects_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolAnalyticsSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallMasteryPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    StudentsWithEvidence = table.Column<int>(type: "integer", nullable: false),
                    AtRiskStudents = table.Column<int>(type: "integer", nullable: false),
                    CriticalOutcomeCount = table.Column<int>(type: "integer", nullable: false),
                    WeakTopicCount = table.Column<int>(type: "integer", nullable: false),
                    LatestSourceUpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolAnalyticsSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolAnalyticsSnapshots_AcademicYears_SchoolId_AcademicYea~",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolAnalyticsSnapshots_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Terms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terms", x => x.Id);
                    table.UniqueConstraint("AK_Terms_SchoolId_AcademicYearId_Id", x => new { x.SchoolId, x.AcademicYearId, x.Id });
                    table.ForeignKey(
                        name: "FK_Terms_AcademicYears_SchoolId_AcademicYearId",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Terms_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RowsJson = table.Column<string>(type: "text", nullable: false),
                    RowCount = table.Column<int>(type: "integer", nullable: false),
                    ValidRowCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                    table.UniqueConstraint("AK_ImportBatches_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_ImportBatches_AspNetUsers_CompletedByUserId",
                        column: x => x.CompletedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportBatches_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ImportBatches_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    StudentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedStudentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(205)", maxLength: 205, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentProfiles", x => x.Id);
                    table.UniqueConstraint("AK_StudentProfiles_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_StudentProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentProfiles_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumFrameworkVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedVersionCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumFrameworkVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumFrameworkVersions_CurriculumFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "CurriculumFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradeLevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassGroups", x => x.Id);
                    table.UniqueConstraint("AK_ClassGroups_SchoolId_AcademicYearId_Id", x => new { x.SchoolId, x.AcademicYearId, x.Id });
                    table.UniqueConstraint("AK_ClassGroups_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_ClassGroups_AcademicYears_SchoolId_AcademicYearId",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassGroups_GradeLevels_SchoolId_GradeLevelId",
                        columns: x => new { x.SchoolId, x.GradeLevelId },
                        principalTable: "GradeLevels",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassGroups_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportValidationErrors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    ColumnName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RawValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportValidationErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportValidationErrors_ImportBatches_SchoolId_ImportBatchId",
                        columns: x => new { x.SchoolId, x.ImportBatchId },
                        principalTable: "ImportBatches",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ImportValidationErrors_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumTopics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradeLevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumTopics", x => x.Id);
                    table.UniqueConstraint("AK_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_Grad~", x => new { x.SchoolId, x.FrameworkVersionId, x.SubjectId, x.GradeLevelId, x.Id });
                    table.UniqueConstraint("AK_CurriculumTopics_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_CurriculumTopics_CurriculumFrameworkVersions_FrameworkVersi~",
                        column: x => x.FrameworkVersionId,
                        principalTable: "CurriculumFrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumTopics_GradeLevels_SchoolId_GradeLevelId",
                        columns: x => new { x.SchoolId, x.GradeLevelId },
                        principalTable: "GradeLevels",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumTopics_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumTopics_Subjects_SchoolId_SubjectId",
                        columns: x => new { x.SchoolId, x.SubjectId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolCurriculumAdoptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: true),
                    GradeLevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolCurriculumAdoptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolCurriculumAdoptions_AcademicYears_SchoolId_AcademicYe~",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolCurriculumAdoptions_CurriculumFrameworkVersions_Frame~",
                        column: x => x.FrameworkVersionId,
                        principalTable: "CurriculumFrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolCurriculumAdoptions_GradeLevels_SchoolId_GradeLevelId",
                        columns: x => new { x.SchoolId, x.GradeLevelId },
                        principalTable: "GradeLevels",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolCurriculumAdoptions_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolCurriculumAdoptions_Subjects_SchoolId_SubjectId",
                        columns: x => new { x.SchoolId, x.SubjectId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    TermId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AssessmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.Id);
                    table.UniqueConstraint("AK_Assessments_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_Assessments_AcademicYears_SchoolId_AcademicYearId",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assessments_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assessments_ClassGroups_SchoolId_AcademicYearId_ClassGroupId",
                        columns: x => new { x.SchoolId, x.AcademicYearId, x.ClassGroupId },
                        principalTable: "ClassGroups",
                        principalColumns: new[] { "SchoolId", "AcademicYearId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assessments_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assessments_Subjects_SchoolId_SubjectId",
                        columns: x => new { x.SchoolId, x.SubjectId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assessments_Terms_SchoolId_AcademicYearId_TermId",
                        columns: x => new { x.SchoolId, x.AcademicYearId, x.TermId },
                        principalTable: "Terms",
                        principalColumns: new[] { "SchoolId", "AcademicYearId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrolledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_AcademicYears_SchoolId_AcademicYearId",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_ClassGroups_SchoolId_AcademicYearId_Clas~",
                        columns: x => new { x.SchoolId, x.AcademicYearId, x.ClassGroupId },
                        principalTable: "ClassGroups",
                        principalColumns: new[] { "SchoolId", "AcademicYearId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollments_StudentProfiles_SchoolId_StudentProfileId",
                        columns: x => new { x.SchoolId, x.StudentProfileId },
                        principalTable: "StudentProfiles",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeacherUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_AcademicYears_SchoolId_AcademicYearId",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_AspNetUsers_TeacherUserId",
                        column: x => x.TeacherUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_ClassGroups_SchoolId_AcademicYearId_Clas~",
                        columns: x => new { x.SchoolId, x.AcademicYearId, x.ClassGroupId },
                        principalTable: "ClassGroups",
                        principalColumns: new[] { "SchoolId", "AcademicYearId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherAssignments_Subjects_SchoolId_SubjectId",
                        columns: x => new { x.SchoolId, x.SubjectId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassTopicSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurriculumTopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    MasteryPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    OutcomeCount = table.Column<int>(type: "integer", nullable: false),
                    WeakOutcomeCount = table.Column<int>(type: "integer", nullable: false),
                    StudentCount = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassTopicSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassTopicSummaries_AcademicYears_SchoolId_AcademicYearId",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassTopicSummaries_ClassGroups_SchoolId_AcademicYearId_Cla~",
                        columns: x => new { x.SchoolId, x.AcademicYearId, x.ClassGroupId },
                        principalTable: "ClassGroups",
                        principalColumns: new[] { "SchoolId", "AcademicYearId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassTopicSummaries_CurriculumTopics_SchoolId_CurriculumTop~",
                        columns: x => new { x.SchoolId, x.CurriculumTopicId },
                        principalTable: "CurriculumTopics",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassTopicSummaries_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassTopicSummaries_Subjects_SchoolId_SubjectId",
                        columns: x => new { x.SchoolId, x.SubjectId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LearningOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradeLevelId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningOutcomes", x => x.Id);
                    table.UniqueConstraint("AK_LearningOutcomes_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_LearningOutcomes_CurriculumTopics_SchoolId_FrameworkVersion~",
                        columns: x => new { x.SchoolId, x.FrameworkVersionId, x.SubjectId, x.GradeLevelId, x.TopicId },
                        principalTable: "CurriculumTopics",
                        principalColumns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningOutcomes_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Prompt = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentQuestions", x => x.Id);
                    table.UniqueConstraint("AK_AssessmentQuestions_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_AssessmentQuestions_Assessments_SchoolId_AssessmentId",
                        columns: x => new { x.SchoolId, x.AssessmentId },
                        principalTable: "Assessments",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentQuestions_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    EnteredByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnteredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentResults", x => x.Id);
                    table.UniqueConstraint("AK_AssessmentResults_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_AssessmentResults_AspNetUsers_EnteredByUserId",
                        column: x => x.EnteredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentResults_Assessments_SchoolId_AssessmentId",
                        columns: x => new { x.SchoolId, x.AssessmentId },
                        principalTable: "Assessments",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentResults_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentResults_StudentProfiles_SchoolId_StudentProfileId",
                        columns: x => new { x.SchoolId, x.StudentProfileId },
                        principalTable: "StudentProfiles",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassAssessmentTrends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AssessmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AveragePercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    StudentCount = table.Column<int>(type: "integer", nullable: false),
                    AtRiskStudentCount = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassAssessmentTrends", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassAssessmentTrends_AcademicYears_SchoolId_AcademicYearId",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassAssessmentTrends_Assessments_SchoolId_AssessmentId",
                        columns: x => new { x.SchoolId, x.AssessmentId },
                        principalTable: "Assessments",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassAssessmentTrends_ClassGroups_SchoolId_AcademicYearId_C~",
                        columns: x => new { x.SchoolId, x.AcademicYearId, x.ClassGroupId },
                        principalTable: "ClassGroups",
                        principalColumns: new[] { "SchoolId", "AcademicYearId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassAssessmentTrends_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassAssessmentTrends_Subjects_SchoolId_SubjectId",
                        columns: x => new { x.SchoolId, x.SubjectId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClassOutcomeSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EarnedScore = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    PossibleScore = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    AverageMasteryPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    StudentCount = table.Column<int>(type: "integer", nullable: false),
                    AtRiskStudentCount = table.Column<int>(type: "integer", nullable: false),
                    EvidenceCount = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassOutcomeSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClassOutcomeSummaries_AcademicYears_SchoolId_AcademicYearId",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassOutcomeSummaries_ClassGroups_SchoolId_AcademicYearId_C~",
                        columns: x => new { x.SchoolId, x.AcademicYearId, x.ClassGroupId },
                        principalTable: "ClassGroups",
                        principalColumns: new[] { "SchoolId", "AcademicYearId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassOutcomeSummaries_LearningOutcomes_SchoolId_LearningOut~",
                        columns: x => new { x.SchoolId, x.LearningOutcomeId },
                        principalTable: "LearningOutcomes",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassOutcomeSummaries_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassOutcomeSummaries_Subjects_SchoolId_SubjectId",
                        columns: x => new { x.SchoolId, x.SubjectId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentOutcomeMasteries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EarnedScore = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    PossibleScore = table.Column<decimal>(type: "numeric(12,4)", precision: 12, scale: 4, nullable: false),
                    MasteryPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    EvidenceCount = table.Column<int>(type: "integer", nullable: false),
                    Band = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentOutcomeMasteries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentOutcomeMasteries_AcademicYears_SchoolId_AcademicYear~",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentOutcomeMasteries_ClassGroups_SchoolId_AcademicYearId~",
                        columns: x => new { x.SchoolId, x.AcademicYearId, x.ClassGroupId },
                        principalTable: "ClassGroups",
                        principalColumns: new[] { "SchoolId", "AcademicYearId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentOutcomeMasteries_LearningOutcomes_SchoolId_LearningO~",
                        columns: x => new { x.SchoolId, x.LearningOutcomeId },
                        principalTable: "LearningOutcomes",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentOutcomeMasteries_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentOutcomeMasteries_StudentProfiles_SchoolId_StudentPro~",
                        columns: x => new { x.SchoolId, x.StudentProfileId },
                        principalTable: "StudentProfiles",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentOutcomeMasteries_Subjects_SchoolId_SubjectId",
                        columns: x => new { x.SchoolId, x.SubjectId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuestionLearningOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionLearningOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionLearningOutcomes_AssessmentQuestions_SchoolId_Asses~",
                        columns: x => new { x.SchoolId, x.AssessmentQuestionId },
                        principalTable: "AssessmentQuestions",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionLearningOutcomes_LearningOutcomes_SchoolId_Learning~",
                        columns: x => new { x.SchoolId, x.LearningOutcomeId },
                        principalTable: "LearningOutcomes",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionLearningOutcomes_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentQuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAnswers_AssessmentQuestions_SchoolId_AssessmentQuest~",
                        columns: x => new { x.SchoolId, x.AssessmentQuestionId },
                        principalTable: "AssessmentQuestions",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAnswers_AssessmentResults_SchoolId_AssessmentResultId",
                        columns: x => new { x.SchoolId, x.AssessmentResultId },
                        principalTable: "AssessmentResults",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentAnswers_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYears_SchoolId_Name",
                table: "AcademicYears",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true,
                filter: "\"NormalizedEmail\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SchoolId",
                table: "AspNetUsers",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentQuestions_SchoolId_AssessmentId_Order",
                table: "AssessmentQuestions",
                columns: new[] { "SchoolId", "AssessmentId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_EnteredByUserId",
                table: "AssessmentResults",
                column: "EnteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_SchoolId_AssessmentId_StudentProfileId",
                table: "AssessmentResults",
                columns: new[] { "SchoolId", "AssessmentId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentResults_SchoolId_StudentProfileId",
                table: "AssessmentResults",
                columns: new[] { "SchoolId", "StudentProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_CreatedByUserId",
                table: "Assessments",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_SchoolId_AcademicYearId_ClassGroupId",
                table: "Assessments",
                columns: new[] { "SchoolId", "AcademicYearId", "ClassGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_SchoolId_AcademicYearId_TermId",
                table: "Assessments",
                columns: new[] { "SchoolId", "AcademicYearId", "TermId" });

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_SchoolId_ClassGroupId_TermId_Title",
                table: "Assessments",
                columns: new[] { "SchoolId", "ClassGroupId", "TermId", "Title" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_SchoolId_SubjectId",
                table: "Assessments",
                columns: new[] { "SchoolId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassAssessmentTrends_SchoolId_AcademicYearId_ClassGroupId_~",
                table: "ClassAssessmentTrends",
                columns: new[] { "SchoolId", "AcademicYearId", "ClassGroupId", "SubjectId", "AssessmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassAssessmentTrends_SchoolId_AssessmentId",
                table: "ClassAssessmentTrends",
                columns: new[] { "SchoolId", "AssessmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassAssessmentTrends_SchoolId_SubjectId",
                table: "ClassAssessmentTrends",
                columns: new[] { "SchoolId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroups_SchoolId_AcademicYearId_NormalizedCode",
                table: "ClassGroups",
                columns: new[] { "SchoolId", "AcademicYearId", "NormalizedCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroups_SchoolId_GradeLevelId",
                table: "ClassGroups",
                columns: new[] { "SchoolId", "GradeLevelId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassOutcomeSummaries_SchoolId_AcademicYearId_ClassGroupId_~",
                table: "ClassOutcomeSummaries",
                columns: new[] { "SchoolId", "AcademicYearId", "ClassGroupId", "SubjectId", "LearningOutcomeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassOutcomeSummaries_SchoolId_LearningOutcomeId",
                table: "ClassOutcomeSummaries",
                columns: new[] { "SchoolId", "LearningOutcomeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassOutcomeSummaries_SchoolId_SubjectId",
                table: "ClassOutcomeSummaries",
                columns: new[] { "SchoolId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassTopicSummaries_SchoolId_AcademicYearId_ClassGroupId_Su~",
                table: "ClassTopicSummaries",
                columns: new[] { "SchoolId", "AcademicYearId", "ClassGroupId", "SubjectId", "CurriculumTopicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassTopicSummaries_SchoolId_CurriculumTopicId",
                table: "ClassTopicSummaries",
                columns: new[] { "SchoolId", "CurriculumTopicId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassTopicSummaries_SchoolId_SubjectId",
                table: "ClassTopicSummaries",
                columns: new[] { "SchoolId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumFrameworks_OwnerSchoolId_NormalizedCode",
                table: "CurriculumFrameworks",
                columns: new[] { "OwnerSchoolId", "NormalizedCode" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumFrameworkVersions_FrameworkId_NormalizedVersionCo~",
                table: "CurriculumFrameworkVersions",
                columns: new[] { "FrameworkId", "NormalizedVersionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_FrameworkVersionId",
                table: "CurriculumTopics",
                column: "FrameworkVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_Gra~1",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_Grad~",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_SchoolId_GradeLevelId",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "GradeLevelId" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_SchoolId_SubjectId",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_GradeLevels_SchoolId_Name",
                table: "GradeLevels",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GradeLevels_SchoolId_Order",
                table: "GradeLevels",
                columns: new[] { "SchoolId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_CompletedByUserId",
                table: "ImportBatches",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_SchoolId_CreatedAtUtc",
                table: "ImportBatches",
                columns: new[] { "SchoolId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_SchoolId_UploadedByUserId_ImportType_FileHash",
                table: "ImportBatches",
                columns: new[] { "SchoolId", "UploadedByUserId", "ImportType", "FileHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_UploadedByUserId",
                table: "ImportBatches",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportValidationErrors_SchoolId_ImportBatchId_RowNumber",
                table: "ImportValidationErrors",
                columns: new[] { "SchoolId", "ImportBatchId", "RowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningOutcomes_SchoolId_FrameworkVersionId_SubjectId_Gra~1",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "TopicId" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningOutcomes_SchoolId_FrameworkVersionId_SubjectId_Grad~",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningOutcomes_SchoolId_TopicId_Order",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "TopicId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CorrelationId",
                table: "OutboxMessages",
                column: "CorrelationId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_AvailableAtUtc_OccurredAtUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "AvailableAtUtc", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_SchoolId_ProcessedAtUtc",
                table: "OutboxMessages",
                columns: new[] { "SchoolId", "ProcessedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionLearningOutcomes_SchoolId_AssessmentQuestionId_Lear~",
                table: "QuestionLearningOutcomes",
                columns: new[] { "SchoolId", "AssessmentQuestionId", "LearningOutcomeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionLearningOutcomes_SchoolId_LearningOutcomeId",
                table: "QuestionLearningOutcomes",
                columns: new[] { "SchoolId", "LearningOutcomeId" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolAnalyticsSnapshots_SchoolId_AcademicYearId",
                table: "SchoolAnalyticsSnapshots",
                columns: new[] { "SchoolId", "AcademicYearId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurriculumAdoptions_FrameworkVersionId",
                table: "SchoolCurriculumAdoptions",
                column: "FrameworkVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_AcademicYearId_GradeLev~1",
                table: "SchoolCurriculumAdoptions",
                columns: new[] { "SchoolId", "AcademicYearId", "GradeLevelId", "SubjectId", "FrameworkVersionId" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_AcademicYearId_GradeLeve~",
                table: "SchoolCurriculumAdoptions",
                columns: new[] { "SchoolId", "AcademicYearId", "GradeLevelId", "SubjectId" },
                unique: true,
                filter: "\"IsPrimary\" = TRUE")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_GradeLevelId",
                table: "SchoolCurriculumAdoptions",
                columns: new[] { "SchoolId", "GradeLevelId" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_SubjectId",
                table: "SchoolCurriculumAdoptions",
                columns: new[] { "SchoolId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Schools_NormalizedSchoolCode",
                table: "Schools",
                column: "NormalizedSchoolCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_SchoolId_AssessmentQuestionId",
                table: "StudentAnswers",
                columns: new[] { "SchoolId", "AssessmentQuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_SchoolId_AssessmentResultId_AssessmentQuesti~",
                table: "StudentAnswers",
                columns: new[] { "SchoolId", "AssessmentResultId", "AssessmentQuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_SchoolId_AcademicYearId_ClassGroupId",
                table: "StudentEnrollments",
                columns: new[] { "SchoolId", "AcademicYearId", "ClassGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_SchoolId_AcademicYearId_StudentProfileId",
                table: "StudentEnrollments",
                columns: new[] { "SchoolId", "AcademicYearId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollments_SchoolId_StudentProfileId",
                table: "StudentEnrollments",
                columns: new[] { "SchoolId", "StudentProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentOutcomeMasteries_SchoolId_AcademicYearId_ClassGroupI~",
                table: "StudentOutcomeMasteries",
                columns: new[] { "SchoolId", "AcademicYearId", "ClassGroupId", "SubjectId", "StudentProfileId", "LearningOutcomeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentOutcomeMasteries_SchoolId_LearningOutcomeId",
                table: "StudentOutcomeMasteries",
                columns: new[] { "SchoolId", "LearningOutcomeId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentOutcomeMasteries_SchoolId_StudentProfileId",
                table: "StudentOutcomeMasteries",
                columns: new[] { "SchoolId", "StudentProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentOutcomeMasteries_SchoolId_SubjectId",
                table: "StudentOutcomeMasteries",
                columns: new[] { "SchoolId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_SchoolId_NormalizedStudentNumber",
                table: "StudentProfiles",
                columns: new[] { "SchoolId", "NormalizedStudentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_UserId",
                table: "StudentProfiles",
                column: "UserId",
                unique: true,
                filter: "\"UserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_SchoolId_NormalizedCode",
                table: "Subjects",
                columns: new[] { "SchoolId", "NormalizedCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_SchoolId_AcademicYearId_ClassGroupId",
                table: "TeacherAssignments",
                columns: new[] { "SchoolId", "AcademicYearId", "ClassGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_SchoolId_SubjectId",
                table: "TeacherAssignments",
                columns: new[] { "SchoolId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_SchoolId_TeacherUserId_ClassGroupId_Subj~",
                table: "TeacherAssignments",
                columns: new[] { "SchoolId", "TeacherUserId", "ClassGroupId", "SubjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeacherAssignments_TeacherUserId",
                table: "TeacherAssignments",
                column: "TeacherUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Terms_SchoolId_AcademicYearId_Name",
                table: "Terms",
                columns: new[] { "SchoolId", "AcademicYearId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ClassAssessmentTrends");

            migrationBuilder.DropTable(
                name: "ClassOutcomeSummaries");

            migrationBuilder.DropTable(
                name: "ClassTopicSummaries");

            migrationBuilder.DropTable(
                name: "ImportValidationErrors");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "QuestionLearningOutcomes");

            migrationBuilder.DropTable(
                name: "SchoolAnalyticsSnapshots");

            migrationBuilder.DropTable(
                name: "SchoolCurriculumAdoptions");

            migrationBuilder.DropTable(
                name: "StudentAnswers");

            migrationBuilder.DropTable(
                name: "StudentEnrollments");

            migrationBuilder.DropTable(
                name: "StudentOutcomeMasteries");

            migrationBuilder.DropTable(
                name: "TeacherAssignments");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "ImportBatches");

            migrationBuilder.DropTable(
                name: "AssessmentQuestions");

            migrationBuilder.DropTable(
                name: "AssessmentResults");

            migrationBuilder.DropTable(
                name: "LearningOutcomes");

            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropTable(
                name: "StudentProfiles");

            migrationBuilder.DropTable(
                name: "CurriculumTopics");

            migrationBuilder.DropTable(
                name: "ClassGroups");

            migrationBuilder.DropTable(
                name: "Terms");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CurriculumFrameworkVersions");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "GradeLevels");

            migrationBuilder.DropTable(
                name: "AcademicYears");

            migrationBuilder.DropTable(
                name: "CurriculumFrameworks");

            migrationBuilder.DropTable(
                name: "Schools");
        }
    }
}

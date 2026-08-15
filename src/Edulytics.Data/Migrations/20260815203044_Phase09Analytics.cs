using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase09Analytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassAssessmentTrends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssessmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AveragePercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    StudentCount = table.Column<int>(type: "int", nullable: false),
                    AtRiskStudentCount = table.Column<int>(type: "int", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                        name: "FK_ClassAssessmentTrends_ClassGroups_SchoolId_AcademicYearId_ClassGroupId",
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EarnedScore = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    PossibleScore = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    AverageMasteryPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    StudentCount = table.Column<int>(type: "int", nullable: false),
                    AtRiskStudentCount = table.Column<int>(type: "int", nullable: false),
                    EvidenceCount = table.Column<int>(type: "int", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                        name: "FK_ClassOutcomeSummaries_ClassGroups_SchoolId_AcademicYearId_ClassGroupId",
                        columns: x => new { x.SchoolId, x.AcademicYearId, x.ClassGroupId },
                        principalTable: "ClassGroups",
                        principalColumns: new[] { "SchoolId", "AcademicYearId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassOutcomeSummaries_LearningOutcomes_SchoolId_LearningOutcomeId",
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
                name: "ClassTopicSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurriculumTopicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MasteryPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OutcomeCount = table.Column<int>(type: "int", nullable: false),
                    WeakOutcomeCount = table.Column<int>(type: "int", nullable: false),
                    StudentCount = table.Column<int>(type: "int", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                        name: "FK_ClassTopicSummaries_ClassGroups_SchoolId_AcademicYearId_ClassGroupId",
                        columns: x => new { x.SchoolId, x.AcademicYearId, x.ClassGroupId },
                        principalTable: "ClassGroups",
                        principalColumns: new[] { "SchoolId", "AcademicYearId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClassTopicSummaries_CurriculumTopics_SchoolId_CurriculumTopicId",
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
                name: "SchoolAnalyticsSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OverallMasteryPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    StudentsWithEvidence = table.Column<int>(type: "int", nullable: false),
                    AtRiskStudents = table.Column<int>(type: "int", nullable: false),
                    CriticalOutcomeCount = table.Column<int>(type: "int", nullable: false),
                    WeakTopicCount = table.Column<int>(type: "int", nullable: false),
                    LatestSourceUpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolAnalyticsSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolAnalyticsSnapshots_AcademicYears_SchoolId_AcademicYearId",
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
                name: "StudentOutcomeMasteries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EarnedScore = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    PossibleScore = table.Column<decimal>(type: "decimal(12,4)", precision: 12, scale: 4, nullable: false),
                    MasteryPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    EvidenceCount = table.Column<int>(type: "int", nullable: false),
                    Band = table.Column<int>(type: "int", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentOutcomeMasteries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentOutcomeMasteries_AcademicYears_SchoolId_AcademicYearId",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentOutcomeMasteries_ClassGroups_SchoolId_AcademicYearId_ClassGroupId",
                        columns: x => new { x.SchoolId, x.AcademicYearId, x.ClassGroupId },
                        principalTable: "ClassGroups",
                        principalColumns: new[] { "SchoolId", "AcademicYearId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentOutcomeMasteries_LearningOutcomes_SchoolId_LearningOutcomeId",
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
                        name: "FK_StudentOutcomeMasteries_StudentProfiles_SchoolId_StudentProfileId",
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

            migrationBuilder.CreateIndex(
                name: "IX_ClassAssessmentTrends_SchoolId_AcademicYearId_ClassGroupId_SubjectId_AssessmentDate",
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
                name: "IX_ClassOutcomeSummaries_SchoolId_AcademicYearId_ClassGroupId_SubjectId_LearningOutcomeId",
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
                name: "IX_ClassTopicSummaries_SchoolId_AcademicYearId_ClassGroupId_SubjectId_CurriculumTopicId",
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
                name: "IX_SchoolAnalyticsSnapshots_SchoolId_AcademicYearId",
                table: "SchoolAnalyticsSnapshots",
                columns: new[] { "SchoolId", "AcademicYearId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentOutcomeMasteries_SchoolId_AcademicYearId_ClassGroupId_SubjectId_StudentProfileId_LearningOutcomeId",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassAssessmentTrends");

            migrationBuilder.DropTable(
                name: "ClassOutcomeSummaries");

            migrationBuilder.DropTable(
                name: "ClassTopicSummaries");

            migrationBuilder.DropTable(
                name: "SchoolAnalyticsSnapshots");

            migrationBuilder.DropTable(
                name: "StudentOutcomeMasteries");
        }
    }
}

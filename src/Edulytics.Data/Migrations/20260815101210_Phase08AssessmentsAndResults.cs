using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase08AssessmentsAndResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Terms_SchoolId_AcademicYearId_Id",
                table: "Terms",
                columns: new[] { "SchoolId", "AcademicYearId", "Id" });

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssessmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                name: "AssessmentQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    EnteredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                name: "QuestionLearningOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionLearningOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionLearningOutcomes_AssessmentQuestions_SchoolId_AssessmentQuestionId",
                        columns: x => new { x.SchoolId, x.AssessmentQuestionId },
                        principalTable: "AssessmentQuestions",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuestionLearningOutcomes_LearningOutcomes_SchoolId_LearningOutcomeId",
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentResultId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAnswers_AssessmentQuestions_SchoolId_AssessmentQuestionId",
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
                name: "IX_QuestionLearningOutcomes_SchoolId_AssessmentQuestionId_LearningOutcomeId",
                table: "QuestionLearningOutcomes",
                columns: new[] { "SchoolId", "AssessmentQuestionId", "LearningOutcomeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionLearningOutcomes_SchoolId_LearningOutcomeId",
                table: "QuestionLearningOutcomes",
                columns: new[] { "SchoolId", "LearningOutcomeId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_SchoolId_AssessmentQuestionId",
                table: "StudentAnswers",
                columns: new[] { "SchoolId", "AssessmentQuestionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_SchoolId_AssessmentResultId_AssessmentQuestionId",
                table: "StudentAnswers",
                columns: new[] { "SchoolId", "AssessmentResultId", "AssessmentQuestionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestionLearningOutcomes");

            migrationBuilder.DropTable(
                name: "StudentAnswers");

            migrationBuilder.DropTable(
                name: "AssessmentQuestions");

            migrationBuilder.DropTable(
                name: "AssessmentResults");

            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Terms_SchoolId_AcademicYearId_Id",
                table: "Terms");
        }
    }
}

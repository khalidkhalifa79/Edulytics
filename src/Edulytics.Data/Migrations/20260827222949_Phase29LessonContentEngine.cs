using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase29LessonContentEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LearningLessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PublishedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningLessons", x => x.Id);
                    table.UniqueConstraint("AK_LearningLessons_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_LearningLessons_CurriculumTopics_SchoolId_TopicId",
                        columns: x => new { x.SchoolId, x.TopicId },
                        principalTable: "CurriculumTopics",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningLessons_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LearningLessonOutcomes",
                columns: table => new
                {
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    LearningOutcomeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningLessonOutcomes", x => new { x.SchoolId, x.LessonId, x.LearningOutcomeId });
                    table.ForeignKey(
                        name: "FK_LearningLessonOutcomes_LearningLessons_SchoolId_LessonId",
                        columns: x => new { x.SchoolId, x.LessonId },
                        principalTable: "LearningLessons",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningLessonOutcomes_LearningOutcomes_SchoolId_LearningOu~",
                        columns: x => new { x.SchoolId, x.LearningOutcomeId },
                        principalTable: "LearningOutcomes",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningLessonOutcomes_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LearningLessonTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    CultureCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Explanation = table.Column<string>(type: "text", nullable: false),
                    KeyConceptsAndRules = table.Column<string>(type: "text", nullable: false),
                    WorkedExamples = table.Column<string>(type: "text", nullable: false),
                    StepByStepSolutions = table.Column<string>(type: "text", nullable: false),
                    CommonMistakes = table.Column<string>(type: "text", nullable: false),
                    QuickSummary = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LearningLessonTranslations", x => x.Id);
                    table.UniqueConstraint("AK_LearningLessonTranslations_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_LearningLessonTranslations_LearningLessons_SchoolId_LessonId",
                        columns: x => new { x.SchoolId, x.LessonId },
                        principalTable: "LearningLessons",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LearningLessonTranslations_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LearningLessonOutcomes_SchoolId_LearningOutcomeId",
                table: "LearningLessonOutcomes",
                columns: new[] { "SchoolId", "LearningOutcomeId" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningLessons_SchoolId_Status",
                table: "LearningLessons",
                columns: new[] { "SchoolId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningLessons_SchoolId_TopicId_Order",
                table: "LearningLessons",
                columns: new[] { "SchoolId", "TopicId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningLessonTranslations_SchoolId_LessonId_CultureCode",
                table: "LearningLessonTranslations",
                columns: new[] { "SchoolId", "LessonId", "CultureCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LearningLessonOutcomes");

            migrationBuilder.DropTable(
                name: "LearningLessonTranslations");

            migrationBuilder.DropTable(
                name: "LearningLessons");
        }
    }
}

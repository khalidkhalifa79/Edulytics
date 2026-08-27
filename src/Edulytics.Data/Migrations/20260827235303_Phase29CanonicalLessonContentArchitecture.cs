using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase29CanonicalLessonContentArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurriculumLessonContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ContentVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumLessonContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumLessonContents_CurriculumFrameworkVersions_Framew~",
                        column: x => x.FrameworkVersionId,
                        principalTable: "CurriculumFrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumLessonContents_CurriculumPackContentNodes_LessonN~",
                        column: x => x.LessonNodeId,
                        principalTable: "CurriculumPackContentNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumLessonContentTranslations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurriculumLessonContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CultureCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
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
                    table.PrimaryKey("PK_CurriculumLessonContentTranslations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumLessonContentTranslations_CurriculumLessonContent~",
                        column: x => x.CurriculumLessonContentId,
                        principalTable: "CurriculumLessonContents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumLessonContents_FrameworkVersionId_Status",
                table: "CurriculumLessonContents",
                columns: new[] { "FrameworkVersionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumLessonContents_LessonNodeId",
                table: "CurriculumLessonContents",
                column: "LessonNodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumLessonContentTranslations_CurriculumLessonContent~",
                table: "CurriculumLessonContentTranslations",
                columns: new[] { "CurriculumLessonContentId", "CultureCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurriculumLessonContentTranslations");

            migrationBuilder.DropTable(
                name: "CurriculumLessonContents");
        }
    }
}

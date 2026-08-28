using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase29MultiProgramCurriculumStreams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LearningOutcomes_CurriculumTopics_SchoolId_FrameworkVersion~",
                table: "LearningOutcomes");

            migrationBuilder.DropIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_AcademicYearId_GradeLev~1",
                table: "SchoolCurriculumAdoptions");

            migrationBuilder.DropIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_AcademicYearId_GradeLeve~",
                table: "SchoolCurriculumAdoptions");

            migrationBuilder.DropIndex(
                name: "IX_LearningOutcomes_SchoolId_FrameworkVersionId_SubjectId_Gra~1",
                table: "LearningOutcomes");

            migrationBuilder.DropIndex(
                name: "IX_LearningOutcomes_SchoolId_FrameworkVersionId_SubjectId_Grad~",
                table: "LearningOutcomes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_Grad~",
                table: "CurriculumTopics");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_Gra~1",
                table: "CurriculumTopics");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_Grad~",
                table: "CurriculumTopics");

            migrationBuilder.DropIndex(
                name: "IX_ClassGroups_SchoolId_AcademicYearId_NormalizedCode",
                table: "ClassGroups");

            migrationBuilder.AddColumn<Guid>(
                name: "AcademicProgramId",
                table: "SchoolCurriculumAdoptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcademicProgramId",
                table: "LearningOutcomes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcademicProgramId",
                table: "CurriculumTopics",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcademicProgramId",
                table: "ClassGroups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CurriculumTopics_SchoolId_AcademicProgramId_FrameworkVersio~",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "AcademicProgramId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Id" });

            migrationBuilder.CreateTable(
                name: "AcademicPrograms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NormalizedCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicPrograms", x => x.Id);
                    table.UniqueConstraint("AK_AcademicPrograms_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_AcademicPrograms_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_AcademicProgramId",
                table: "SchoolCurriculumAdoptions",
                columns: new[] { "SchoolId", "AcademicProgramId" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_AcademicYearId_Academic~1",
                table: "SchoolCurriculumAdoptions",
                columns: new[] { "SchoolId", "AcademicYearId", "AcademicProgramId", "GradeLevelId", "SubjectId", "FrameworkVersionId" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_AcademicYearId_AcademicP~",
                table: "SchoolCurriculumAdoptions",
                columns: new[] { "SchoolId", "AcademicYearId", "AcademicProgramId", "GradeLevelId", "SubjectId" },
                unique: true,
                filter: "\"IsPrimary\" = TRUE")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_LearningOutcomes_SchoolId_AcademicProgramId_FrameworkVersi~1",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "AcademicProgramId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "TopicId" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningOutcomes_SchoolId_AcademicProgramId_FrameworkVersio~",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "AcademicProgramId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_SchoolId_AcademicProgramId_FrameworkVersi~1",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "AcademicProgramId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_SchoolId_AcademicProgramId_FrameworkVersio~",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "AcademicProgramId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroups_SchoolId_AcademicProgramId",
                table: "ClassGroups",
                columns: new[] { "SchoolId", "AcademicProgramId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClassGroups_SchoolId_AcademicYearId_AcademicProgramId_Norma~",
                table: "ClassGroups",
                columns: new[] { "SchoolId", "AcademicYearId", "AcademicProgramId", "NormalizedCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPrograms_SchoolId",
                table: "AcademicPrograms",
                column: "SchoolId",
                unique: true,
                filter: "\"IsDefault\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_AcademicPrograms_SchoolId_NormalizedCode",
                table: "AcademicPrograms",
                columns: new[] { "SchoolId", "NormalizedCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClassGroups_AcademicPrograms_SchoolId_AcademicProgramId",
                table: "ClassGroups",
                columns: new[] { "SchoolId", "AcademicProgramId" },
                principalTable: "AcademicPrograms",
                principalColumns: new[] { "SchoolId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CurriculumTopics_AcademicPrograms_SchoolId_AcademicProgramId",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "AcademicProgramId" },
                principalTable: "AcademicPrograms",
                principalColumns: new[] { "SchoolId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LearningOutcomes_AcademicPrograms_SchoolId_AcademicProgramId",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "AcademicProgramId" },
                principalTable: "AcademicPrograms",
                principalColumns: new[] { "SchoolId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LearningOutcomes_CurriculumTopics_SchoolId_AcademicProgramI~",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "AcademicProgramId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "TopicId" },
                principalTable: "CurriculumTopics",
                principalColumns: new[] { "SchoolId", "AcademicProgramId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SchoolCurriculumAdoptions_AcademicPrograms_SchoolId_Academi~",
                table: "SchoolCurriculumAdoptions",
                columns: new[] { "SchoolId", "AcademicProgramId" },
                principalTable: "AcademicPrograms",
                principalColumns: new[] { "SchoolId", "Id" },
                onDelete: ReferentialAction.Restrict);
            // Phase 29 multi-program data preservation.
            migrationBuilder.Sql(
                """
                INSERT INTO "AcademicPrograms"
                    ("Id","SchoolId","Name","Code","NormalizedCode","Status","IsDefault",
                     "CreatedAtUtc","UpdatedAtUtc","RowVersion")
                SELECT
                    md5(s."Id"::text || '|EDULYTICS-MAIN-PROGRAM')::uuid,
                    s."Id",
                    'Main Program',
                    'MAIN',
                    'MAIN',
                    1,
                    TRUE,
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP,
                    decode(repeat('00',16),'hex')
                FROM "Schools" s
                ON CONFLICT ("SchoolId","NormalizedCode") DO NOTHING;

                UPDATE "ClassGroups" x
                   SET "AcademicProgramId" = p."Id"
                  FROM "AcademicPrograms" p
                 WHERE p."SchoolId" = x."SchoolId"
                   AND p."IsDefault" = TRUE
                   AND x."AcademicProgramId" IS NULL;

                UPDATE "SchoolCurriculumAdoptions" x
                   SET "AcademicProgramId" = p."Id"
                  FROM "AcademicPrograms" p
                 WHERE p."SchoolId" = x."SchoolId"
                   AND p."IsDefault" = TRUE
                   AND x."AcademicProgramId" IS NULL;

                UPDATE "CurriculumTopics" x
                   SET "AcademicProgramId" = p."Id"
                  FROM "AcademicPrograms" p
                 WHERE p."SchoolId" = x."SchoolId"
                   AND p."IsDefault" = TRUE
                   AND x."AcademicProgramId" IS NULL;

                UPDATE "LearningOutcomes" o
                   SET "AcademicProgramId" = t."AcademicProgramId"
                  FROM "CurriculumTopics" t
                 WHERE t."SchoolId" = o."SchoolId"
                   AND t."Id" = o."TopicId"
                   AND o."AcademicProgramId" IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "AcademicProgramId",
                table: "ClassGroups",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AcademicProgramId",
                table: "SchoolCurriculumAdoptions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AcademicProgramId",
                table: "CurriculumTopics",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "AcademicProgramId",
                table: "LearningOutcomes",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClassGroups_AcademicPrograms_SchoolId_AcademicProgramId",
                table: "ClassGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_CurriculumTopics_AcademicPrograms_SchoolId_AcademicProgramId",
                table: "CurriculumTopics");

            migrationBuilder.DropForeignKey(
                name: "FK_LearningOutcomes_AcademicPrograms_SchoolId_AcademicProgramId",
                table: "LearningOutcomes");

            migrationBuilder.DropForeignKey(
                name: "FK_LearningOutcomes_CurriculumTopics_SchoolId_AcademicProgramI~",
                table: "LearningOutcomes");

            migrationBuilder.DropForeignKey(
                name: "FK_SchoolCurriculumAdoptions_AcademicPrograms_SchoolId_Academi~",
                table: "SchoolCurriculumAdoptions");

            migrationBuilder.DropTable(
                name: "AcademicPrograms");

            migrationBuilder.DropIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_AcademicProgramId",
                table: "SchoolCurriculumAdoptions");

            migrationBuilder.DropIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_AcademicYearId_Academic~1",
                table: "SchoolCurriculumAdoptions");

            migrationBuilder.DropIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_AcademicYearId_AcademicP~",
                table: "SchoolCurriculumAdoptions");

            migrationBuilder.DropIndex(
                name: "IX_LearningOutcomes_SchoolId_AcademicProgramId_FrameworkVersi~1",
                table: "LearningOutcomes");

            migrationBuilder.DropIndex(
                name: "IX_LearningOutcomes_SchoolId_AcademicProgramId_FrameworkVersio~",
                table: "LearningOutcomes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CurriculumTopics_SchoolId_AcademicProgramId_FrameworkVersio~",
                table: "CurriculumTopics");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumTopics_SchoolId_AcademicProgramId_FrameworkVersi~1",
                table: "CurriculumTopics");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumTopics_SchoolId_AcademicProgramId_FrameworkVersio~",
                table: "CurriculumTopics");

            migrationBuilder.DropIndex(
                name: "IX_ClassGroups_SchoolId_AcademicProgramId",
                table: "ClassGroups");

            migrationBuilder.DropIndex(
                name: "IX_ClassGroups_SchoolId_AcademicYearId_AcademicProgramId_Norma~",
                table: "ClassGroups");

            migrationBuilder.DropColumn(
                name: "AcademicProgramId",
                table: "SchoolCurriculumAdoptions");

            migrationBuilder.DropColumn(
                name: "AcademicProgramId",
                table: "LearningOutcomes");

            migrationBuilder.DropColumn(
                name: "AcademicProgramId",
                table: "CurriculumTopics");

            migrationBuilder.DropColumn(
                name: "AcademicProgramId",
                table: "ClassGroups");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_Grad~",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Id" });

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
                name: "IX_LearningOutcomes_SchoolId_FrameworkVersionId_SubjectId_Gra~1",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "TopicId" });

            migrationBuilder.CreateIndex(
                name: "IX_LearningOutcomes_SchoolId_FrameworkVersionId_SubjectId_Grad~",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Code" },
                unique: true);

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
                name: "IX_ClassGroups_SchoolId_AcademicYearId_NormalizedCode",
                table: "ClassGroups",
                columns: new[] { "SchoolId", "AcademicYearId", "NormalizedCode" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LearningOutcomes_CurriculumTopics_SchoolId_FrameworkVersion~",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "TopicId" },
                principalTable: "CurriculumTopics",
                principalColumns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}

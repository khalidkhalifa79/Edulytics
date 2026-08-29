using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase29AcademicYearProgramOfferings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicYearProgramOfferings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: false),
                    AcademicProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsOffered = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicYearProgramOfferings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicYearProgramOfferings_AcademicPrograms_SchoolId_Acad~",
                        columns: x => new { x.SchoolId, x.AcademicProgramId },
                        principalTable: "AcademicPrograms",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademicYearProgramOfferings_AcademicYears_SchoolId_Academi~",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcademicYearProgramOfferings_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYearProgramOfferings_SchoolId_AcademicProgramId",
                table: "AcademicYearProgramOfferings",
                columns: new[] { "SchoolId", "AcademicProgramId" });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicYearProgramOfferings_SchoolId_AcademicYearId_Academ~",
                table: "AcademicYearProgramOfferings",
                columns: new[] { "SchoolId", "AcademicYearId", "AcademicProgramId" },
                unique: true);

        // Backfill annual program offerings from existing historical usage.
        //
        // Existing ClassGroups prove that a stream was offered in that
        // academic year. Active yearly curriculum adoptions are also
        // authoritative evidence.
        //
        // No AcademicProgram, ClassGroup, curriculum adoption, or
        // historical record is deleted or rewritten.
        migrationBuilder.Sql(
            """
            WITH program_year_pairs AS
            (
                SELECT DISTINCT
                    "SchoolId",
                    "AcademicYearId",
                    "AcademicProgramId"
                FROM "ClassGroups"

                UNION

                SELECT DISTINCT
                    "SchoolId",
                    "AcademicYearId",
                    "AcademicProgramId"
                FROM "SchoolCurriculumAdoptions"
                WHERE
                    "AcademicYearId" IS NOT NULL
                    AND "IsActive" = TRUE
            )
            INSERT INTO "AcademicYearProgramOfferings"
            (
                "Id",
                "SchoolId",
                "AcademicYearId",
                "AcademicProgramId",
                "IsOffered",
                "CreatedAtUtc",
                "UpdatedAtUtc",
                "RowVersion"
            )
            SELECT
                (
                    md5(
                        'academic-year-program-offering:' ||
                        "SchoolId"::text || ':' ||
                        "AcademicYearId"::text || ':' ||
                        "AcademicProgramId"::text
                    )
                )::uuid,
                "SchoolId",
                "AcademicYearId",
                "AcademicProgramId",
                TRUE,
                NOW(),
                NOW(),
                decode(
                    md5(
                        'rowversion:' ||
                        "SchoolId"::text || ':' ||
                        "AcademicYearId"::text || ':' ||
                        "AcademicProgramId"::text
                    ),
                    'hex'
                )
            FROM program_year_pairs
            ON CONFLICT
            (
                "SchoolId",
                "AcademicYearId",
                "AcademicProgramId"
            )
            DO UPDATE
            SET
                "IsOffered" = TRUE,
                "UpdatedAtUtc" = NOW();
            """);

    }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicYearProgramOfferings");
        }
    }
}

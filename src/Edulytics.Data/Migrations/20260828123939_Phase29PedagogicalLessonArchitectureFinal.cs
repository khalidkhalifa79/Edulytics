using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public partial class Phase29PedagogicalLessonArchitectureFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurriculumLessonContents_CurriculumPackContentNodes_LessonN~",
                table: "CurriculumLessonContents");

            migrationBuilder.CreateTable(
                name: "CurriculumPedagogicalLessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OfficialLessonNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    UnitKey = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    UnitTitle = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    Title = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    LogicalLevelFrom = table.Column<int>(type: "integer", nullable: false),
                    LogicalLevelTo = table.Column<int>(type: "integer", nullable: false),
                    NativeLevel = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Pathway = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumPedagogicalLessons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumPedagogicalLessons_CurriculumFrameworkVersions_Fr~",
                        column: x => x.FrameworkVersionId,
                        principalTable: "CurriculumFrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumPedagogicalLessons_CurriculumPackContentNodes_Off~",
                        column: x => x.OfficialLessonNodeId,
                        principalTable: "CurriculumPackContentNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumPedagogicalLessonOutcomes",
                columns: table => new
                {
                    PedagogicalLessonId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutcomeNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumPedagogicalLessonOutcomes", x => new { x.PedagogicalLessonId, x.OutcomeNodeId });
                    table.ForeignKey(
                        name: "FK_CurriculumPedagogicalLessonOutcomes_CurriculumPackContentNo~",
                        column: x => x.OutcomeNodeId,
                        principalTable: "CurriculumPackContentNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumPedagogicalLessonOutcomes_CurriculumPedagogicalLe~",
                        column: x => x.PedagogicalLessonId,
                        principalTable: "CurriculumPedagogicalLessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPedagogicalLessonOutcomes_FrameworkVersionId_Outc~",
                table: "CurriculumPedagogicalLessonOutcomes",
                columns: new[] { "FrameworkVersionId", "OutcomeNodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPedagogicalLessonOutcomes_OutcomeNodeId",
                table: "CurriculumPedagogicalLessonOutcomes",
                column: "OutcomeNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPedagogicalLessons_FrameworkVersionId_Code",
                table: "CurriculumPedagogicalLessons",
                columns: new[] { "FrameworkVersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPedagogicalLessons_FrameworkVersionId_LogicalLeve~",
                table: "CurriculumPedagogicalLessons",
                columns: new[] { "FrameworkVersionId", "LogicalLevelFrom", "LogicalLevelTo", "Pathway", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPedagogicalLessons_OfficialLessonNodeId",
                table: "CurriculumPedagogicalLessons",
                column: "OfficialLessonNodeId",
                unique: true);


            // Zero-loss bridge for canonical bodies from the prior Phase29 architecture.
            // Only UAE has verified official Lesson nodes in the accepted packs.
            migrationBuilder.Sql(
                """
                INSERT INTO "CurriculumPedagogicalLessons"
                (
                    "Id","FrameworkVersionId","OfficialLessonNodeId","Code",
                    "UnitKey","UnitTitle","Title","LogicalLevelFrom","LogicalLevelTo",
                    "NativeLevel","Pathway","SortOrder","CreatedAtUtc","UpdatedAtUtc","RowVersion"
                )
                SELECT
                    lesson."Id",
                    lesson."FrameworkVersionId",
                    lesson."Id",
                    LEFT('PED:' || lesson."Code",600),
                    LEFT(COALESCE(unit_node."Code",''),600),
                    LEFT(COALESCE(unit_node."Title",''),600),
                    LEFT(lesson."Title",600),
                    lesson."LogicalLevelFrom",
                    lesson."LogicalLevelTo",
                    lesson."NativeLevel",
                    lesson."Pathway",
                    lesson."SortOrder",
                    CURRENT_TIMESTAMP,
                    CURRENT_TIMESTAMP,
                    decode(repeat('00',16),'hex')
                FROM "CurriculumPackContentNodes" lesson
                LEFT JOIN "CurriculumPackContentNodes" unit_node
                    ON unit_node."Id"=lesson."ParentId"
                WHERE lesson."FrameworkCode"='UAE-MOE-MATH'
                  AND lesson."NodeKind"='Lesson'
                  AND lesson."IsActive"=TRUE
                ON CONFLICT ("Id") DO NOTHING;

                INSERT INTO "CurriculumPedagogicalLessonOutcomes"
                    ("PedagogicalLessonId","FrameworkVersionId","OutcomeNodeId","SortOrder")
                SELECT
                    link."FromNodeId",
                    link."FrameworkVersionId",
                    link."ToNodeId",
                    link."SortOrder"
                FROM "CurriculumPackNodeLinks" link
                INNER JOIN "CurriculumPackContentNodes" lesson
                    ON lesson."Id"=link."FromNodeId"
                INNER JOIN "CurriculumPackContentNodes" outcome
                    ON outcome."Id"=link."ToNodeId"
                WHERE lesson."FrameworkCode"='UAE-MOE-MATH'
                  AND lesson."NodeKind"='Lesson'
                  AND lesson."IsActive"=TRUE
                  AND outcome."IsOfficial"=TRUE
                  AND outcome."NodeKind" IN ('Standard','Outcome')
                  AND link."LinkKind"='LessonStandardAlignment'
                ON CONFLICT ("PedagogicalLessonId","OutcomeNodeId") DO NOTHING;

                DO $$
                BEGIN
                    IF EXISTS
                    (
                        SELECT 1
                        FROM "CurriculumLessonContents" content
                        LEFT JOIN "CurriculumPedagogicalLessons" lesson
                            ON lesson."Id"=content."LessonNodeId"
                        WHERE lesson."Id" IS NULL
                    )
                    THEN
                        RAISE EXCEPTION
                            'Phase29 pedagogical migration refused: an existing canonical content row does not map to a verified pedagogical lesson.';
                    END IF;
                END
                $$;
                """);

migrationBuilder.AddForeignKey(
                name: "FK_CurriculumLessonContents_CurriculumPedagogicalLessons_Lesso~",
                table: "CurriculumLessonContents",
                column: "LessonNodeId",
                principalTable: "CurriculumPedagogicalLessons",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurriculumLessonContents_CurriculumPedagogicalLessons_Lesso~",
                table: "CurriculumLessonContents");

            migrationBuilder.DropTable(
                name: "CurriculumPedagogicalLessonOutcomes");

            migrationBuilder.DropTable(
                name: "CurriculumPedagogicalLessons");

            migrationBuilder.AddForeignKey(
                name: "FK_CurriculumLessonContents_CurriculumPackContentNodes_LessonN~",
                table: "CurriculumLessonContents",
                column: "LessonNodeId",
                principalTable: "CurriculumPackContentNodes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

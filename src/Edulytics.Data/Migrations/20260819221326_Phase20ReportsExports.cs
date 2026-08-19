using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase20ReportsExports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportExportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportKind = table.Column<int>(type: "integer", nullable: false),
                    ExportFormat = table.Column<int>(type: "integer", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uuid", nullable: true),
                    ClassGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    StudentProfileId = table.Column<Guid>(type: "uuid", nullable: true),
                    LearningOutcomeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Culture = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RowCount = table.Column<int>(type: "integer", nullable: true),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    FileContent = table.Column<byte[]>(type: "bytea", nullable: true),
                    LastError = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportExportJobs", x => x.Id);
                    table.UniqueConstraint("AK_ReportExportJobs_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_ReportExportJobs_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReportExportJobs_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportExportJobs_ExpiresAtUtc",
                table: "ReportExportJobs",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExportJobs_RequestedByUserId",
                table: "ReportExportJobs",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportExportJobs_SchoolId_RequestedByUserId_CreatedAtUtc",
                table: "ReportExportJobs",
                columns: new[] { "SchoolId", "RequestedByUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ReportExportJobs_SchoolId_Status_CreatedAtUtc",
                table: "ReportExportJobs",
                columns: new[] { "SchoolId", "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportExportJobs");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase25BCustomerOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    WorkEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedWorkEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    City = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    EstimatedStudentCount = table.Column<int>(type: "integer", nullable: false),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DemoScheduledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InternalNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    PrivacyConsentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DemoSchoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProvisionedSchoolId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProvisionedSchoolAdminUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoRequests_Schools_DemoSchoolId",
                        column: x => x.DemoSchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemoRequests_Schools_ProvisionedSchoolId",
                        column: x => x.ProvisionedSchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemoAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DemoRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolAdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ConvertedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemoAccesses_DemoRequests_DemoRequestId",
                        column: x => x.DemoRequestId,
                        principalTable: "DemoRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemoAccesses_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoAccesses_DemoRequestId",
                table: "DemoAccesses",
                column: "DemoRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemoAccesses_ExpiresAtUtc_RevokedAtUtc_ConvertedAtUtc",
                table: "DemoAccesses",
                columns: new[] { "ExpiresAtUtc", "RevokedAtUtc", "ConvertedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DemoAccesses_SchoolId",
                table: "DemoAccesses",
                column: "SchoolId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemoRequests_DemoSchoolId",
                table: "DemoRequests",
                column: "DemoSchoolId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemoRequests_NormalizedWorkEmail_Status",
                table: "DemoRequests",
                columns: new[] { "NormalizedWorkEmail", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_DemoRequests_ProvisionedSchoolId",
                table: "DemoRequests",
                column: "ProvisionedSchoolId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemoRequests_Status_CreatedAtUtc",
                table: "DemoRequests",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoAccesses");

            migrationBuilder.DropTable(
                name: "DemoRequests");
        }
    }
}

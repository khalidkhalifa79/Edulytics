using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase15OutboxV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_SchoolId_ProcessedAtUtc",
                table: "OutboxMessages");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadLetteredAtUtc",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeaseOwner",
                table: "OutboxMessages",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LeaseToken",
                table: "OutboxMessages",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeaseUntilUtc",
                table: "OutboxMessages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "OutboxMessages",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "AnalyticsRefreshStates",
                columns: table => new
                {
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedVersion = table.Column<long>(type: "bigint", nullable: false),
                    CompletedVersion = table.Column<long>(type: "bigint", nullable: false),
                    FirstRequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastRequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CoalesceDeadlineUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvailableAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeaseOwner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uuid", nullable: true),
                    LeaseUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsRefreshStates", x => x.SchoolId);
                    table.ForeignKey(
                        name: "FK_AnalyticsRefreshStates_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboxRequeueAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OutboxMessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PreviousAttempts = table.Column<int>(type: "integer", nullable: false),
                    RequeuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxRequeueAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutboxRequeueAudits_OutboxMessages_OutboxMessageId",
                        column: x => x.OutboxMessageId,
                        principalTable: "OutboxMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_SchoolId_Status_OccurredAtUtc",
                table: "OutboxMessages",
                columns: new[] { "SchoolId", "Status", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status_AvailableAtUtc_LeaseUntilUtc_Occurred~",
                table: "OutboxMessages",
                columns: new[] { "Status", "AvailableAtUtc", "LeaseUntilUtc", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsRefreshStates_AvailableAtUtc_LeaseUntilUtc",
                table: "AnalyticsRefreshStates",
                columns: new[] { "AvailableAtUtc", "LeaseUntilUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsRefreshStates_RequestedVersion_CompletedVersion",
                table: "AnalyticsRefreshStates",
                columns: new[] { "RequestedVersion", "CompletedVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxRequeueAudits_ActorUserId_RequeuedAtUtc",
                table: "OutboxRequeueAudits",
                columns: new[] { "ActorUserId", "RequeuedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxRequeueAudits_OutboxMessageId_RequeuedAtUtc",
                table: "OutboxRequeueAudits",
                columns: new[] { "OutboxMessageId", "RequeuedAtUtc" });
            // PHASE15_OUTBOX_STATUS_BACKFILL
            migrationBuilder.Sql(
                "UPDATE \"OutboxMessages\" "
                + "SET \"Status\" = CASE "
                + "WHEN \"ProcessedAtUtc\" IS NULL "
                + "THEN 1 ELSE 3 END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticsRefreshStates");

            migrationBuilder.DropTable(
                name: "OutboxRequeueAudits");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_SchoolId_Status_OccurredAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Status_AvailableAtUtc_LeaseUntilUtc_Occurred~",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "DeadLetteredAtUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LeaseOwner",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LeaseToken",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "LeaseUntilUtc",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "OutboxMessages");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_SchoolId_ProcessedAtUtc",
                table: "OutboxMessages",
                columns: new[] { "SchoolId", "ProcessedAtUtc" });
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase25CSubscriptionsEntitlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAtUtc",
                table: "StudentProfiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "StudentProfiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "StudentProfiles",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);


            migrationBuilder.Sql(
                "UPDATE \"StudentProfiles\" "
                + "SET \"RowVersion\" = "
                + "decode('00112233445566778899AABBCCDDEEFF', 'hex') "
                + "WHERE octet_length(\"RowVersion\") = 0;");

migrationBuilder.CreateTable(
                name: "SchoolSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    Term = table.Column<int>(type: "integer", nullable: false),
                    BillingCadence = table.Column<int>(type: "integer", nullable: false),
                    CommercialCurrency = table.Column<int>(type: "integer", nullable: false),
                    PricePerStudentPerMonth = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    CommittedSeats = table.Column<int>(type: "integer", nullable: false),
                    PendingRenewalSeats = table.Column<int>(type: "integer", nullable: true),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    NonRenewalRequestedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentTermStartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentTermEndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspendedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolSubscriptions", x => x.Id);
                    table.UniqueConstraint("AK_SchoolSubscriptions_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_SchoolSubscriptions_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionSeatChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeType = table.Column<int>(type: "integer", nullable: false),
                    PreviousSeats = table.Column<int>(type: "integer", nullable: false),
                    NewSeats = table.Column<int>(type: "integer", nullable: false),
                    EffectiveAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionSeatChanges", x => x.Id);
                    table.UniqueConstraint("AK_SubscriptionSeatChanges_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_SubscriptionSeatChanges_SchoolSubscriptions_SchoolId_Subscr~",
                        columns: x => new { x.SchoolId, x.SubscriptionId },
                        principalTable: "SchoolSubscriptions",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionSeatChanges_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentProfiles_SchoolId_IsArchived_Status",
                table: "StudentProfiles",
                columns: new[] { "SchoolId", "IsArchived", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolSubscriptions_SchoolId",
                table: "SchoolSubscriptions",
                column: "SchoolId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolSubscriptions_Status_CurrentTermEndsAtUtc",
                table: "SchoolSubscriptions",
                columns: new[] { "Status", "CurrentTermEndsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionSeatChanges_SchoolId_EffectiveAtUtc",
                table: "SubscriptionSeatChanges",
                columns: new[] { "SchoolId", "EffectiveAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionSeatChanges_SchoolId_SubscriptionId",
                table: "SubscriptionSeatChanges",
                columns: new[] { "SchoolId", "SubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionSeatChanges_SubscriptionId_EffectiveAtUtc",
                table: "SubscriptionSeatChanges",
                columns: new[] { "SubscriptionId", "EffectiveAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionSeatChanges");

            migrationBuilder.DropTable(
                name: "SchoolSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_StudentProfiles_SchoolId_IsArchived_Status",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "ArchivedAtUtc",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "StudentProfiles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "StudentProfiles");
        }
    }
}

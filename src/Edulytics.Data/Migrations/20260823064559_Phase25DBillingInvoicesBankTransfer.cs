using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase25DBillingInvoicesBankTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillingInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InvoiceCurrency = table.Column<int>(type: "integer", nullable: false),
                    SettlementCurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    SettlementEquivalentAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    LegalNameSnapshot = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    BillingAddressSnapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CountryCodeSnapshot = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    TaxIdentifierSnapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    InvoiceEmailSnapshot = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    TaxTreatmentCodeSnapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PaymentInstructionsSnapshot = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IssueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDateUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GraceEndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingPeriodStartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BillingPeriodEndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InstallmentNumber = table.Column<int>(type: "integer", nullable: true),
                    NetAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingInvoices", x => x.Id);
                    table.UniqueConstraint("AK_BillingInvoices_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_BillingInvoices_SchoolSubscriptions_SchoolId_SubscriptionId",
                        columns: x => new { x.SchoolId, x.SubscriptionId },
                        principalTable: "SchoolSubscriptions",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingInvoices_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolBillingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    BillingAddress = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    TaxIdentifier = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    InvoiceEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    TaxTreatmentCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DefaultSettlementCurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    PaymentInstructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolBillingProfiles", x => x.Id);
                    table.UniqueConstraint("AK_SchoolBillingProfiles_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_SchoolBillingProfiles_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BankTransferPayments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationStatus = table.Column<int>(type: "integer", nullable: false),
                    PaymentReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EvidenceNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReceivedAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ReceivedCurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerifiedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankTransferPayments", x => x.Id);
                    table.UniqueConstraint("AK_BankTransferPayments_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_BankTransferPayments_BillingInvoices_SchoolId_InvoiceId",
                        columns: x => new { x.SchoolId, x.InvoiceId },
                        principalTable: "BillingInvoices",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingInvoiceLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SeatCount = table.Column<int>(type: "integer", nullable: true),
                    SeatDelta = table.Column<int>(type: "integer", nullable: true),
                    UnitMonthlyPrice = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    QuantityMonths = table.Column<int>(type: "integer", nullable: true),
                    ServicePeriodStartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ServicePeriodEndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProrationNumeratorDays = table.Column<int>(type: "integer", nullable: true),
                    ProrationDenominatorDays = table.Column<int>(type: "integer", nullable: true),
                    NetAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    SubscriptionSeatChangeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingInvoiceLines", x => x.Id);
                    table.UniqueConstraint("AK_BillingInvoiceLines_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_BillingInvoiceLines_BillingInvoices_SchoolId_InvoiceId",
                        columns: x => new { x.SchoolId, x.InvoiceId },
                        principalTable: "BillingInvoices",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingInvoiceLines_SubscriptionSeatChanges_SchoolId_Subscr~",
                        columns: x => new { x.SchoolId, x.SubscriptionSeatChangeId },
                        principalTable: "SubscriptionSeatChanges",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingRefunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingRefunds", x => x.Id);
                    table.UniqueConstraint("AK_BillingRefunds_SchoolId_Id", x => new { x.SchoolId, x.Id });
                    table.ForeignKey(
                        name: "FK_BillingRefunds_BankTransferPayments_SchoolId_PaymentId",
                        columns: x => new { x.SchoolId, x.PaymentId },
                        principalTable: "BankTransferPayments",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingRefunds_BillingInvoices_SchoolId_InvoiceId",
                        columns: x => new { x.SchoolId, x.InvoiceId },
                        principalTable: "BillingInvoices",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPayments_SchoolId_InvoiceId_ReceivedAtUtc",
                table: "BankTransferPayments",
                columns: new[] { "SchoolId", "InvoiceId", "ReceivedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransferPayments_SchoolId_PaymentReference",
                table: "BankTransferPayments",
                columns: new[] { "SchoolId", "PaymentReference" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoiceLines_SchoolId_InvoiceId",
                table: "BillingInvoiceLines",
                columns: new[] { "SchoolId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoiceLines_SchoolId_SubscriptionSeatChangeId",
                table: "BillingInvoiceLines",
                columns: new[] { "SchoolId", "SubscriptionSeatChangeId" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoiceLines_SubscriptionSeatChangeId",
                table: "BillingInvoiceLines",
                column: "SubscriptionSeatChangeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_InvoiceNumber",
                table: "BillingInvoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_SchoolId_Status_DueDateUtc",
                table: "BillingInvoices",
                columns: new[] { "SchoolId", "Status", "DueDateUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_SchoolId_SubscriptionId",
                table: "BillingInvoices",
                columns: new[] { "SchoolId", "SubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_SubscriptionId_Kind_InstallmentNumber",
                table: "BillingInvoices",
                columns: new[] { "SubscriptionId", "Kind", "InstallmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingRefunds_SchoolId_InvoiceId_RecordedAtUtc",
                table: "BillingRefunds",
                columns: new[] { "SchoolId", "InvoiceId", "RecordedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingRefunds_SchoolId_PaymentId",
                table: "BillingRefunds",
                columns: new[] { "SchoolId", "PaymentId" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolBillingProfiles_SchoolId",
                table: "SchoolBillingProfiles",
                column: "SchoolId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingInvoiceLines");

            migrationBuilder.DropTable(
                name: "BillingRefunds");

            migrationBuilder.DropTable(
                name: "SchoolBillingProfiles");

            migrationBuilder.DropTable(
                name: "BankTransferPayments");

            migrationBuilder.DropTable(
                name: "BillingInvoices");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase275VerifiedCurriculumPersistenceV19 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CurriculumPackContentNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VersionCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NodeKind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Code = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    LogicalLevelFrom = table.Column<int>(type: "integer", nullable: false),
                    LogicalLevelTo = table.Column<int>(type: "integer", nullable: false),
                    NativeLevel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Pathway = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Title = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    OfficialText = table.Column<string>(type: "text", nullable: true),
                    AuthorDescription = table.Column<string>(type: "text", nullable: true),
                    SourceAuthority = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2500)", maxLength: 2500, nullable: false),
                    SourceLocator = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Attribution = table.Column<string>(type: "character varying(2500)", maxLength: 2500, nullable: false),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumPackContentNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumPackContentNodes_CurriculumFrameworkVersions_Fram~",
                        column: x => x.FrameworkVersionId,
                        principalTable: "CurriculumFrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumPackContentNodes_CurriculumPackContentNodes_Paren~",
                        column: x => x.ParentId,
                        principalTable: "CurriculumPackContentNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumPackImportStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VersionCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SourceDigest = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContentDigest = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    NodeCount = table.Column<int>(type: "integer", nullable: false),
                    OfficialNodeCount = table.Column<int>(type: "integer", nullable: false),
                    UnitCount = table.Column<int>(type: "integer", nullable: false),
                    LessonCount = table.Column<int>(type: "integer", nullable: false),
                    LinkCount = table.Column<int>(type: "integer", nullable: false),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    ImportedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumPackImportStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumPackImportStates_CurriculumFrameworkVersions_Fram~",
                        column: x => x.FrameworkVersionId,
                        principalTable: "CurriculumFrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumPackNodeLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkKind = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AlignmentConfidence = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EvidenceNote = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumPackNodeLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumPackNodeLinks_CurriculumFrameworkVersions_Framewo~",
                        column: x => x.FrameworkVersionId,
                        principalTable: "CurriculumFrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumPackNodeLinks_CurriculumPackContentNodes_FromNode~",
                        column: x => x.FromNodeId,
                        principalTable: "CurriculumPackContentNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CurriculumPackNodeLinks_CurriculumPackContentNodes_ToNodeId",
                        column: x => x.ToNodeId,
                        principalTable: "CurriculumPackContentNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPackContentNodes_FrameworkCode_VersionCode_NodeKi~",
                table: "CurriculumPackContentNodes",
                columns: new[] { "FrameworkCode", "VersionCode", "NodeKind", "LogicalLevelFrom", "LogicalLevelTo" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPackContentNodes_FrameworkVersionId_Code",
                table: "CurriculumPackContentNodes",
                columns: new[] { "FrameworkVersionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPackContentNodes_ParentId_SortOrder",
                table: "CurriculumPackContentNodes",
                columns: new[] { "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPackImportStates_FrameworkCode_VersionCode",
                table: "CurriculumPackImportStates",
                columns: new[] { "FrameworkCode", "VersionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPackImportStates_FrameworkVersionId",
                table: "CurriculumPackImportStates",
                column: "FrameworkVersionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPackNodeLinks_FrameworkVersionId_FromNodeId_ToNod~",
                table: "CurriculumPackNodeLinks",
                columns: new[] { "FrameworkVersionId", "FromNodeId", "ToNodeId", "LinkKind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPackNodeLinks_FromNodeId_SortOrder",
                table: "CurriculumPackNodeLinks",
                columns: new[] { "FromNodeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumPackNodeLinks_ToNodeId",
                table: "CurriculumPackNodeLinks",
                column: "ToNodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CurriculumPackImportStates");

            migrationBuilder.DropTable(
                name: "CurriculumPackNodeLinks");

            migrationBuilder.DropTable(
                name: "CurriculumPackContentNodes");
        }
    }
}

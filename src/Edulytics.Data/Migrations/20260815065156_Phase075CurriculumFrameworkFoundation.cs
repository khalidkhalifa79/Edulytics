using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase075CurriculumFrameworkFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LearningOutcomes_CurriculumTopics_SchoolId_TopicId",
                table: "LearningOutcomes");

            migrationBuilder.DropIndex(
                name: "IX_LearningOutcomes_SchoolId_Code",
                table: "LearningOutcomes");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumTopics_SchoolId_SubjectId_GradeLevelId_Name",
                table: "CurriculumTopics");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumTopics_SchoolId_SubjectId_GradeLevelId_Order",
                table: "CurriculumTopics");

            migrationBuilder.AddColumn<Guid>(
                name: "FrameworkVersionId",
                table: "LearningOutcomes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "GradeLevelId",
                table: "LearningOutcomes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SubjectId",
                table: "LearningOutcomes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "FrameworkVersionId",
                table: "CurriculumTopics",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddUniqueConstraint(
                name: "AK_LearningOutcomes_SchoolId_Id",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_GradeLevelId_Id",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Id" });

            migrationBuilder.CreateTable(
                name: "CurriculumFrameworks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerSchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    ProviderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumFrameworks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumFrameworks_Schools_OwnerSchoolId",
                        column: x => x.OwnerSchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumFrameworkVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedVersionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumFrameworkVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumFrameworkVersions_CurriculumFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "CurriculumFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolCurriculumAdoptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcademicYearId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GradeLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolCurriculumAdoptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolCurriculumAdoptions_AcademicYears_SchoolId_AcademicYearId",
                        columns: x => new { x.SchoolId, x.AcademicYearId },
                        principalTable: "AcademicYears",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolCurriculumAdoptions_CurriculumFrameworkVersions_FrameworkVersionId",
                        column: x => x.FrameworkVersionId,
                        principalTable: "CurriculumFrameworkVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolCurriculumAdoptions_GradeLevels_SchoolId_GradeLevelId",
                        columns: x => new { x.SchoolId, x.GradeLevelId },
                        principalTable: "GradeLevels",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolCurriculumAdoptions_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolCurriculumAdoptions_Subjects_SchoolId_SubjectId",
                        columns: x => new { x.SchoolId, x.SubjectId },
                        principalTable: "Subjects",
                        principalColumns: new[] { "SchoolId", "Id" },
                        onDelete: ReferentialAction.Restrict);
                });


            // EDULYTICS_PHASE075_DEFAULT_BACKFILL
            migrationBuilder.Sql(
                """
                DECLARE @FrameworkId uniqueidentifier =
                    '07500000-0000-0000-0000-000000000001';
                DECLARE @VersionId uniqueidentifier =
                    '07500000-0000-0000-0000-000000000002';

                IF NOT EXISTS (
                    SELECT 1 FROM [CurriculumFrameworks]
                    WHERE [Id] = @FrameworkId
                )
                BEGIN
                    INSERT INTO [CurriculumFrameworks]
                    (
                        [Id], [OwnerSchoolId], [Code], [NormalizedCode],
                        [Name], [CountryCode], [ProviderName], [IsActive],
                        [CreatedAtUtc], [UpdatedAtUtc]
                    )
                    VALUES
                    (
                        @FrameworkId,
                        NULL,
                        N'EDULYTICS-DEFAULT',
                        N'EDULYTICS-DEFAULT',
                        N'Edulytics Default Curriculum',
                        NULL,
                        N'Edulytics platform compatibility framework',
                        CAST(1 AS bit),
                        SYSUTCDATETIME(),
                        SYSUTCDATETIME()
                    );
                END;

                IF NOT EXISTS (
                    SELECT 1 FROM [CurriculumFrameworkVersions]
                    WHERE [Id] = @VersionId
                )
                BEGIN
                    INSERT INTO [CurriculumFrameworkVersions]
                    (
                        [Id], [FrameworkId], [VersionCode],
                        [NormalizedVersionCode], [Name], [EffectiveFrom],
                        [EffectiveTo], [IsActive], [CreatedAtUtc],
                        [UpdatedAtUtc]
                    )
                    VALUES
                    (
                        @VersionId,
                        @FrameworkId,
                        N'V1',
                        N'V1',
                        N'Version 1',
                        NULL,
                        NULL,
                        CAST(1 AS bit),
                        SYSUTCDATETIME(),
                        SYSUTCDATETIME()
                    );
                END;

                UPDATE [CurriculumTopics]
                SET [FrameworkVersionId] = @VersionId
                WHERE [FrameworkVersionId] =
                    '00000000-0000-0000-0000-000000000000';

                UPDATE o
                SET
                    o.[FrameworkVersionId] = t.[FrameworkVersionId],
                    o.[SubjectId] = t.[SubjectId],
                    o.[GradeLevelId] = t.[GradeLevelId]
                FROM [LearningOutcomes] AS o
                INNER JOIN [CurriculumTopics] AS t
                    ON t.[SchoolId] = o.[SchoolId]
                    AND t.[Id] = o.[TopicId];

                INSERT INTO [SchoolCurriculumAdoptions]
                (
                    [Id], [SchoolId], [AcademicYearId], [GradeLevelId],
                    [SubjectId], [FrameworkVersionId], [IsPrimary],
                    [IsActive], [CreatedAtUtc], [UpdatedAtUtc]
                )
                SELECT
                    NEWID(),
                    scope.[SchoolId],
                    NULL,
                    scope.[GradeLevelId],
                    scope.[SubjectId],
                    @VersionId,
                    CAST(1 AS bit),
                    CAST(1 AS bit),
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME()
                FROM
                (
                    SELECT DISTINCT
                        [SchoolId], [GradeLevelId], [SubjectId]
                    FROM [CurriculumTopics]
                ) AS scope
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM [SchoolCurriculumAdoptions] AS a
                    WHERE a.[SchoolId] = scope.[SchoolId]
                      AND a.[AcademicYearId] IS NULL
                      AND a.[GradeLevelId] = scope.[GradeLevelId]
                      AND a.[SubjectId] = scope.[SubjectId]
                      AND a.[FrameworkVersionId] = @VersionId
                );

                -- Remove temporary defaults EF created for additive NOT NULL
                -- columns. New application writes must provide real scope.
                DECLARE @ConstraintName sysname;

                SELECT @ConstraintName = dc.[name]
                FROM sys.default_constraints AS dc
                INNER JOIN sys.columns AS c
                    ON c.[default_object_id] = dc.[object_id]
                INNER JOIN sys.tables AS t
                    ON t.[object_id] = c.[object_id]
                WHERE t.[name] = N'CurriculumTopics'
                  AND c.[name] = N'FrameworkVersionId';
                IF @ConstraintName IS NOT NULL
                    EXEC(N'ALTER TABLE [CurriculumTopics] DROP CONSTRAINT [' +
                         @ConstraintName + N']');

                SET @ConstraintName = NULL;
                SELECT @ConstraintName = dc.[name]
                FROM sys.default_constraints AS dc
                INNER JOIN sys.columns AS c
                    ON c.[default_object_id] = dc.[object_id]
                INNER JOIN sys.tables AS t
                    ON t.[object_id] = c.[object_id]
                WHERE t.[name] = N'LearningOutcomes'
                  AND c.[name] = N'FrameworkVersionId';
                IF @ConstraintName IS NOT NULL
                    EXEC(N'ALTER TABLE [LearningOutcomes] DROP CONSTRAINT [' +
                         @ConstraintName + N']');

                SET @ConstraintName = NULL;
                SELECT @ConstraintName = dc.[name]
                FROM sys.default_constraints AS dc
                INNER JOIN sys.columns AS c
                    ON c.[default_object_id] = dc.[object_id]
                INNER JOIN sys.tables AS t
                    ON t.[object_id] = c.[object_id]
                WHERE t.[name] = N'LearningOutcomes'
                  AND c.[name] = N'SubjectId';
                IF @ConstraintName IS NOT NULL
                    EXEC(N'ALTER TABLE [LearningOutcomes] DROP CONSTRAINT [' +
                         @ConstraintName + N']');

                SET @ConstraintName = NULL;
                SELECT @ConstraintName = dc.[name]
                FROM sys.default_constraints AS dc
                INNER JOIN sys.columns AS c
                    ON c.[default_object_id] = dc.[object_id]
                INNER JOIN sys.tables AS t
                    ON t.[object_id] = c.[object_id]
                WHERE t.[name] = N'LearningOutcomes'
                  AND c.[name] = N'GradeLevelId';
                IF @ConstraintName IS NOT NULL
                    EXEC(N'ALTER TABLE [LearningOutcomes] DROP CONSTRAINT [' +
                         @ConstraintName + N']');
                """
            );

            migrationBuilder.CreateIndex(
                name: "IX_LearningOutcomes_SchoolId_FrameworkVersionId_SubjectId_GradeLevelId_Code",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LearningOutcomes_SchoolId_FrameworkVersionId_SubjectId_GradeLevelId_TopicId",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "TopicId" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_FrameworkVersionId",
                table: "CurriculumTopics",
                column: "FrameworkVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_GradeLevelId_Name",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_GradeLevelId_Order",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_SchoolId_SubjectId",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumFrameworks_OwnerSchoolId_NormalizedCode",
                table: "CurriculumFrameworks",
                columns: new[] { "OwnerSchoolId", "NormalizedCode" },
                unique: true,
                filter: "[OwnerSchoolId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumFrameworkVersions_FrameworkId_NormalizedVersionCode",
                table: "CurriculumFrameworkVersions",
                columns: new[] { "FrameworkId", "NormalizedVersionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurriculumAdoptions_FrameworkVersionId",
                table: "SchoolCurriculumAdoptions",
                column: "FrameworkVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_AcademicYearId_GradeLevelId_SubjectId",
                table: "SchoolCurriculumAdoptions",
                columns: new[] { "SchoolId", "AcademicYearId", "GradeLevelId", "SubjectId" },
                unique: true,
                filter: "[IsPrimary] = CAST(1 AS bit)");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_AcademicYearId_GradeLevelId_SubjectId_FrameworkVersionId",
                table: "SchoolCurriculumAdoptions",
                columns: new[] { "SchoolId", "AcademicYearId", "GradeLevelId", "SubjectId", "FrameworkVersionId" },
                unique: true,
                filter: "[AcademicYearId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_GradeLevelId",
                table: "SchoolCurriculumAdoptions",
                columns: new[] { "SchoolId", "GradeLevelId" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolCurriculumAdoptions_SchoolId_SubjectId",
                table: "SchoolCurriculumAdoptions",
                columns: new[] { "SchoolId", "SubjectId" });

            migrationBuilder.AddForeignKey(
                name: "FK_CurriculumTopics_CurriculumFrameworkVersions_FrameworkVersionId",
                table: "CurriculumTopics",
                column: "FrameworkVersionId",
                principalTable: "CurriculumFrameworkVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LearningOutcomes_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_GradeLevelId_TopicId",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "TopicId" },
                principalTable: "CurriculumTopics",
                principalColumns: new[] { "SchoolId", "FrameworkVersionId", "SubjectId", "GradeLevelId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CurriculumTopics_CurriculumFrameworkVersions_FrameworkVersionId",
                table: "CurriculumTopics");

            migrationBuilder.DropForeignKey(
                name: "FK_LearningOutcomes_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_GradeLevelId_TopicId",
                table: "LearningOutcomes");

            migrationBuilder.DropTable(
                name: "SchoolCurriculumAdoptions");

            migrationBuilder.DropTable(
                name: "CurriculumFrameworkVersions");

            migrationBuilder.DropTable(
                name: "CurriculumFrameworks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_LearningOutcomes_SchoolId_Id",
                table: "LearningOutcomes");

            migrationBuilder.DropIndex(
                name: "IX_LearningOutcomes_SchoolId_FrameworkVersionId_SubjectId_GradeLevelId_Code",
                table: "LearningOutcomes");

            migrationBuilder.DropIndex(
                name: "IX_LearningOutcomes_SchoolId_FrameworkVersionId_SubjectId_GradeLevelId_TopicId",
                table: "LearningOutcomes");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_GradeLevelId_Id",
                table: "CurriculumTopics");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumTopics_FrameworkVersionId",
                table: "CurriculumTopics");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_GradeLevelId_Name",
                table: "CurriculumTopics");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumTopics_SchoolId_FrameworkVersionId_SubjectId_GradeLevelId_Order",
                table: "CurriculumTopics");

            migrationBuilder.DropIndex(
                name: "IX_CurriculumTopics_SchoolId_SubjectId",
                table: "CurriculumTopics");

            migrationBuilder.DropColumn(
                name: "FrameworkVersionId",
                table: "LearningOutcomes");

            migrationBuilder.DropColumn(
                name: "GradeLevelId",
                table: "LearningOutcomes");

            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "LearningOutcomes");

            migrationBuilder.DropColumn(
                name: "FrameworkVersionId",
                table: "CurriculumTopics");

            migrationBuilder.CreateIndex(
                name: "IX_LearningOutcomes_SchoolId_Code",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_SchoolId_SubjectId_GradeLevelId_Name",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "SubjectId", "GradeLevelId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumTopics_SchoolId_SubjectId_GradeLevelId_Order",
                table: "CurriculumTopics",
                columns: new[] { "SchoolId", "SubjectId", "GradeLevelId", "Order" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_LearningOutcomes_CurriculumTopics_SchoolId_TopicId",
                table: "LearningOutcomes",
                columns: new[] { "SchoolId", "TopicId" },
                principalTable: "CurriculumTopics",
                principalColumns: new[] { "SchoolId", "Id" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}

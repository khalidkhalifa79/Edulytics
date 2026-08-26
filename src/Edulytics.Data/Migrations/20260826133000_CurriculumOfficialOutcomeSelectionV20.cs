using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Edulytics.Data.Migrations;

[DbContext(typeof(EdulyticsDbContext))]
[Migration("20260826133000_CurriculumOfficialOutcomeSelectionV20")]
public sealed class CurriculumOfficialOutcomeSelectionV20 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Code",
            table: "LearningOutcomes",
            type: "character varying(300)",
            maxLength: 300,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(50)",
            oldMaxLength: 50);

        migrationBuilder.AddColumn<Guid>(
            name: "OfficialContentNodeId",
            table: "LearningOutcomes",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_LearningOutcomes_OfficialContentNodeId",
            table: "LearningOutcomes",
            column: "OfficialContentNodeId");

        migrationBuilder.CreateIndex(
            name: "IX_LearningOutcomes_SchoolId_TopicId_OfficialContentNodeId",
            table: "LearningOutcomes",
            columns: new[]
            {
                "SchoolId",
                "TopicId",
                "OfficialContentNodeId"
            },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_LearningOutcomes_CurriculumPackContentNodes_OfficialContentNodeId",
            table: "LearningOutcomes",
            column: "OfficialContentNodeId",
            principalTable: "CurriculumPackContentNodes",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_LearningOutcomes_CurriculumPackContentNodes_OfficialContentNodeId",
            table: "LearningOutcomes");

        migrationBuilder.DropIndex(
            name: "IX_LearningOutcomes_OfficialContentNodeId",
            table: "LearningOutcomes");

        migrationBuilder.DropIndex(
            name: "IX_LearningOutcomes_SchoolId_TopicId_OfficialContentNodeId",
            table: "LearningOutcomes");

        migrationBuilder.DropColumn(
            name: "OfficialContentNodeId",
            table: "LearningOutcomes");

        migrationBuilder.AlterColumn<string>(
            name: "Code",
            table: "LearningOutcomes",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(300)",
            oldMaxLength: 300);
    }
}

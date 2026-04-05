using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNIC.DataAccess.Migrations.MeetingDb
{
    /// <inheritdoc />
    public partial class RemoveInterviewAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EvaluationCriteria_CampaignId_DisplayOrder",
                table: "EvaluationCriteria");

            migrationBuilder.DropColumn(
                name: "AssignedCriteriaIds",
                table: "InterviewAssignments");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "EvaluationCriteria");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedCriteriaIds",
                table: "InterviewAssignments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "EvaluationCriteria",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationCriteria_CampaignId_DisplayOrder",
                table: "EvaluationCriteria",
                columns: new[] { "CampaignId", "DisplayOrder" });
        }
    }
}

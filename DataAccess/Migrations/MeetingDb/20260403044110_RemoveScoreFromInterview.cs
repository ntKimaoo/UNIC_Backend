using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNIC.DataAccess.Migrations.MeetingDb
{
    /// <inheritdoc />
    public partial class RemoveScoreFromInterview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Score",
                table: "InterviewAssignments");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "CriteriaScores");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "InterviewAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "CriteriaScores",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}

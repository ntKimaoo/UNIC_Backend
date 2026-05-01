using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNIC.DataAccess.Migrations.MeetingDb
{
    /// <inheritdoc />
    public partial class AddEvaluationCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedCriteriaIds",
                table: "InterviewAssignments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CampaignDecisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignId = table.Column<int>(type: "int", nullable: false),
                    InterviewScheduleId = table.Column<int>(type: "int", nullable: false),
                    CandidateUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ScheduledPublishAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotificationChannels = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignDecisions_InterviewSchedules_InterviewScheduleId",
                        column: x => x.InterviewScheduleId,
                        principalTable: "InterviewSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationCriteria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationCriteria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CriteriaScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewAssignmentId = table.Column<int>(type: "int", nullable: false),
                    EvaluationCriterionId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CriteriaScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CriteriaScores_EvaluationCriteria_EvaluationCriterionId",
                        column: x => x.EvaluationCriterionId,
                        principalTable: "EvaluationCriteria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CriteriaScores_InterviewAssignments_InterviewAssignmentId",
                        column: x => x.InterviewAssignmentId,
                        principalTable: "InterviewAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignDecisions_CampaignId",
                table: "CampaignDecisions",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignDecisions_CampaignId_CandidateUserId",
                table: "CampaignDecisions",
                columns: new[] { "CampaignId", "CandidateUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignDecisions_InterviewScheduleId",
                table: "CampaignDecisions",
                column: "InterviewScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaScores_EvaluationCriterionId",
                table: "CriteriaScores",
                column: "EvaluationCriterionId");

            migrationBuilder.CreateIndex(
                name: "IX_CriteriaScores_InterviewAssignmentId_EvaluationCriterionId",
                table: "CriteriaScores",
                columns: new[] { "InterviewAssignmentId", "EvaluationCriterionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationCriteria_CampaignId",
                table: "EvaluationCriteria",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationCriteria_CampaignId_DisplayOrder",
                table: "EvaluationCriteria",
                columns: new[] { "CampaignId", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignDecisions");

            migrationBuilder.DropTable(
                name: "CriteriaScores");

            migrationBuilder.DropTable(
                name: "EvaluationCriteria");

            migrationBuilder.DropColumn(
                name: "AssignedCriteriaIds",
                table: "InterviewAssignments");
        }
    }
}

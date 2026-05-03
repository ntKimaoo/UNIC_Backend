using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNIC.DataAccess.Migrations.MeetingDb
{
    /// <inheritdoc />
    public partial class addingEmailSendat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FeedbackNudgeSentAt",
                table: "InterviewSchedules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReminderSentAt",
                table: "InterviewSchedules",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RoomOpenedEmailSentAt",
                table: "InterviewSchedules",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeedbackNudgeSentAt",
                table: "InterviewSchedules");

            migrationBuilder.DropColumn(
                name: "ReminderSentAt",
                table: "InterviewSchedules");

            migrationBuilder.DropColumn(
                name: "RoomOpenedEmailSentAt",
                table: "InterviewSchedules");
        }
    }
}

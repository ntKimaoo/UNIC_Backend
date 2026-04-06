using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNIC.DataAccess.Migrations.MeetingDb
{
    /// <inheritdoc />
    public partial class GeneralizeMeetingRoom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MeetingRooms_InterviewSchedules_InterviewScheduleId",
                table: "MeetingRooms");

            migrationBuilder.DropIndex(
                name: "IX_MeetingRooms_InterviewScheduleId",
                table: "MeetingRooms");

            migrationBuilder.AlterColumn<int>(
                name: "InterviewScheduleId",
                table: "MeetingRooms",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "MeetingRooms",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "MeetingRooms",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoomType",
                table: "MeetingRooms",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledEndAt",
                table: "MeetingRooms",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledStartAt",
                table: "MeetingRooms",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "MeetingRooms",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRooms_CreatedByUserId",
                table: "MeetingRooms",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRooms_InterviewScheduleId",
                table: "MeetingRooms",
                column: "InterviewScheduleId",
                unique: true,
                filter: "[InterviewScheduleId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingRooms_InterviewSchedules_InterviewScheduleId",
                table: "MeetingRooms",
                column: "InterviewScheduleId",
                principalTable: "InterviewSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MeetingRooms_InterviewSchedules_InterviewScheduleId",
                table: "MeetingRooms");

            migrationBuilder.DropIndex(
                name: "IX_MeetingRooms_CreatedByUserId",
                table: "MeetingRooms");

            migrationBuilder.DropIndex(
                name: "IX_MeetingRooms_InterviewScheduleId",
                table: "MeetingRooms");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "MeetingRooms");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "MeetingRooms");

            migrationBuilder.DropColumn(
                name: "RoomType",
                table: "MeetingRooms");

            migrationBuilder.DropColumn(
                name: "ScheduledEndAt",
                table: "MeetingRooms");

            migrationBuilder.DropColumn(
                name: "ScheduledStartAt",
                table: "MeetingRooms");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "MeetingRooms");

            migrationBuilder.AlterColumn<int>(
                name: "InterviewScheduleId",
                table: "MeetingRooms",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRooms_InterviewScheduleId",
                table: "MeetingRooms",
                column: "InterviewScheduleId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingRooms_InterviewSchedules_InterviewScheduleId",
                table: "MeetingRooms",
                column: "InterviewScheduleId",
                principalTable: "InterviewSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNIC.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class ModifyMeetingModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewAssignment_InterviewSchedule_InterviewScheduleId",
                table: "InterviewAssignment");

            migrationBuilder.DropForeignKey(
                name: "FK_InterviewSchedule_Candidate_CandidateId",
                table: "InterviewSchedule");

            migrationBuilder.DropForeignKey(
                name: "FK_MeetingRoom_InterviewSchedule_InterviewScheduleId",
                table: "MeetingRoom");

            migrationBuilder.DropForeignKey(
                name: "FK_RoomEvent_MeetingRoom_MeetingRoomId",
                table: "RoomEvent");

            migrationBuilder.DropForeignKey(
                name: "FK_RoomParticipant_MeetingRoom_MeetingRoomId",
                table: "RoomParticipant");

            migrationBuilder.DropTable(
                name: "Candidate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoomParticipant",
                table: "RoomParticipant");

            migrationBuilder.DropIndex(
                name: "IX_RoomParticipant_CandidateId",
                table: "RoomParticipant");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoomEvent",
                table: "RoomEvent");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MeetingRoom",
                table: "MeetingRoom");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterviewSchedule",
                table: "InterviewSchedule");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSchedule_CandidateId",
                table: "InterviewSchedule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterviewAssignment",
                table: "InterviewAssignment");

            migrationBuilder.DropColumn(
                name: "CandidateId",
                table: "RoomParticipant");

            migrationBuilder.DropColumn(
                name: "ActorId",
                table: "RoomEvent");

            migrationBuilder.DropColumn(
                name: "CandidateId",
                table: "InterviewSchedule");

            migrationBuilder.RenameTable(
                name: "RoomParticipant",
                newName: "RoomParticipants");

            migrationBuilder.RenameTable(
                name: "RoomEvent",
                newName: "RoomEvents");

            migrationBuilder.RenameTable(
                name: "MeetingRoom",
                newName: "MeetingRooms");

            migrationBuilder.RenameTable(
                name: "InterviewSchedule",
                newName: "InterviewSchedules");

            migrationBuilder.RenameTable(
                name: "InterviewAssignment",
                newName: "InterviewAssignments");

            migrationBuilder.RenameIndex(
                name: "IX_RoomParticipant_UserId",
                table: "RoomParticipants",
                newName: "IX_RoomParticipants_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RoomParticipant_MeetingRoomId",
                table: "RoomParticipants",
                newName: "IX_RoomParticipants_MeetingRoomId");

            migrationBuilder.RenameIndex(
                name: "IX_RoomEvent_OccurredAt",
                table: "RoomEvents",
                newName: "IX_RoomEvents_OccurredAt");

            migrationBuilder.RenameIndex(
                name: "IX_RoomEvent_MeetingRoomId",
                table: "RoomEvents",
                newName: "IX_RoomEvents_MeetingRoomId");

            migrationBuilder.RenameIndex(
                name: "IX_MeetingRoom_RoomCode",
                table: "MeetingRooms",
                newName: "IX_MeetingRooms_RoomCode");

            migrationBuilder.RenameIndex(
                name: "IX_MeetingRoom_InterviewScheduleId",
                table: "MeetingRooms",
                newName: "IX_MeetingRooms_InterviewScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_InterviewSchedule_CreatedByUserId",
                table: "InterviewSchedules",
                newName: "IX_InterviewSchedules_CreatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_InterviewAssignment_InterviewScheduleId_InterviewerUserId",
                table: "InterviewAssignments",
                newName: "IX_InterviewAssignments_InterviewScheduleId_InterviewerUserId");

            migrationBuilder.RenameIndex(
                name: "IX_InterviewAssignment_InterviewerUserId",
                table: "InterviewAssignments",
                newName: "IX_InterviewAssignments_InterviewerUserId");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "RoomParticipants",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "RoomParticipants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ActorUserId",
                table: "RoomEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedByUserId",
                table: "InterviewSchedules",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36);

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationId",
                table: "InterviewSchedules",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CampaignId",
                table: "InterviewSchedules",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CandidateUserId",
                table: "InterviewSchedules",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "InterviewerUserId",
                table: "InterviewAssignments",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(36)",
                oldMaxLength: 36);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoomParticipants",
                table: "RoomParticipants",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoomEvents",
                table: "RoomEvents",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MeetingRooms",
                table: "MeetingRooms",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterviewSchedules",
                table: "InterviewSchedules",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterviewAssignments",
                table: "InterviewAssignments",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedules_ApplicationId",
                table: "InterviewSchedules",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedules_CampaignId",
                table: "InterviewSchedules",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedules_CandidateUserId",
                table: "InterviewSchedules",
                column: "CandidateUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewAssignments_InterviewSchedules_InterviewScheduleId",
                table: "InterviewAssignments",
                column: "InterviewScheduleId",
                principalTable: "InterviewSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingRooms_InterviewSchedules_InterviewScheduleId",
                table: "MeetingRooms",
                column: "InterviewScheduleId",
                principalTable: "InterviewSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoomEvents_MeetingRooms_MeetingRoomId",
                table: "RoomEvents",
                column: "MeetingRoomId",
                principalTable: "MeetingRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoomParticipants_MeetingRooms_MeetingRoomId",
                table: "RoomParticipants",
                column: "MeetingRoomId",
                principalTable: "MeetingRooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InterviewAssignments_InterviewSchedules_InterviewScheduleId",
                table: "InterviewAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_MeetingRooms_InterviewSchedules_InterviewScheduleId",
                table: "MeetingRooms");

            migrationBuilder.DropForeignKey(
                name: "FK_RoomEvents_MeetingRooms_MeetingRoomId",
                table: "RoomEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_RoomParticipants_MeetingRooms_MeetingRoomId",
                table: "RoomParticipants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoomParticipants",
                table: "RoomParticipants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoomEvents",
                table: "RoomEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MeetingRooms",
                table: "MeetingRooms");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterviewSchedules",
                table: "InterviewSchedules");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSchedules_ApplicationId",
                table: "InterviewSchedules");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSchedules_CampaignId",
                table: "InterviewSchedules");

            migrationBuilder.DropIndex(
                name: "IX_InterviewSchedules_CandidateUserId",
                table: "InterviewSchedules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_InterviewAssignments",
                table: "InterviewAssignments");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "RoomParticipants");

            migrationBuilder.DropColumn(
                name: "ActorUserId",
                table: "RoomEvents");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "InterviewSchedules");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "InterviewSchedules");

            migrationBuilder.DropColumn(
                name: "CandidateUserId",
                table: "InterviewSchedules");

            migrationBuilder.RenameTable(
                name: "RoomParticipants",
                newName: "RoomParticipant");

            migrationBuilder.RenameTable(
                name: "RoomEvents",
                newName: "RoomEvent");

            migrationBuilder.RenameTable(
                name: "MeetingRooms",
                newName: "MeetingRoom");

            migrationBuilder.RenameTable(
                name: "InterviewSchedules",
                newName: "InterviewSchedule");

            migrationBuilder.RenameTable(
                name: "InterviewAssignments",
                newName: "InterviewAssignment");

            migrationBuilder.RenameIndex(
                name: "IX_RoomParticipants_UserId",
                table: "RoomParticipant",
                newName: "IX_RoomParticipant_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RoomParticipants_MeetingRoomId",
                table: "RoomParticipant",
                newName: "IX_RoomParticipant_MeetingRoomId");

            migrationBuilder.RenameIndex(
                name: "IX_RoomEvents_OccurredAt",
                table: "RoomEvent",
                newName: "IX_RoomEvent_OccurredAt");

            migrationBuilder.RenameIndex(
                name: "IX_RoomEvents_MeetingRoomId",
                table: "RoomEvent",
                newName: "IX_RoomEvent_MeetingRoomId");

            migrationBuilder.RenameIndex(
                name: "IX_MeetingRooms_RoomCode",
                table: "MeetingRoom",
                newName: "IX_MeetingRoom_RoomCode");

            migrationBuilder.RenameIndex(
                name: "IX_MeetingRooms_InterviewScheduleId",
                table: "MeetingRoom",
                newName: "IX_MeetingRoom_InterviewScheduleId");

            migrationBuilder.RenameIndex(
                name: "IX_InterviewSchedules_CreatedByUserId",
                table: "InterviewSchedule",
                newName: "IX_InterviewSchedule_CreatedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_InterviewAssignments_InterviewScheduleId_InterviewerUserId",
                table: "InterviewAssignment",
                newName: "IX_InterviewAssignment_InterviewScheduleId_InterviewerUserId");

            migrationBuilder.RenameIndex(
                name: "IX_InterviewAssignments_InterviewerUserId",
                table: "InterviewAssignment",
                newName: "IX_InterviewAssignment_InterviewerUserId");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "RoomParticipant",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "CandidateId",
                table: "RoomParticipant",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActorId",
                table: "RoomEvent",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "InterviewSchedule",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "CandidateId",
                table: "InterviewSchedule",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "InterviewerUserId",
                table: "InterviewAssignment",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoomParticipant",
                table: "RoomParticipant",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoomEvent",
                table: "RoomEvent",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MeetingRoom",
                table: "MeetingRoom",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterviewSchedule",
                table: "InterviewSchedule",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_InterviewAssignment",
                table: "InterviewAssignment",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Candidate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResumeUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidate", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoomParticipant_CandidateId",
                table: "RoomParticipant",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedule_CandidateId",
                table: "InterviewSchedule",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidate_Email",
                table: "Candidate",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewAssignment_InterviewSchedule_InterviewScheduleId",
                table: "InterviewAssignment",
                column: "InterviewScheduleId",
                principalTable: "InterviewSchedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InterviewSchedule_Candidate_CandidateId",
                table: "InterviewSchedule",
                column: "CandidateId",
                principalTable: "Candidate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MeetingRoom_InterviewSchedule_InterviewScheduleId",
                table: "MeetingRoom",
                column: "InterviewScheduleId",
                principalTable: "InterviewSchedule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoomEvent_MeetingRoom_MeetingRoomId",
                table: "RoomEvent",
                column: "MeetingRoomId",
                principalTable: "MeetingRoom",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RoomParticipant_MeetingRoom_MeetingRoomId",
                table: "RoomParticipant",
                column: "MeetingRoomId",
                principalTable: "MeetingRoom",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

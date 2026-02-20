using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNIC.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class InitMeeting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Candidate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResumeUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidate", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InterviewSchedule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CancelReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewSchedule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewSchedule_Candidate_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InterviewAssignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewScheduleId = table.Column<int>(type: "int", nullable: false),
                    InterviewerUserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HasConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    FeedbackNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Result = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Score = table.Column<int>(type: "int", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FeedbackSubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterviewAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewAssignment_InterviewSchedule_InterviewScheduleId",
                        column: x => x.InterviewScheduleId,
                        principalTable: "InterviewSchedule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MeetingRoom",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InterviewScheduleId = table.Column<int>(type: "int", nullable: false),
                    RoomCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StunServerUri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TurnServerUri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TurnUsername = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TurnCredential = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TurnCredentialExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRecordingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsWaitingRoomEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetingRoom", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MeetingRoom_InterviewSchedule_InterviewScheduleId",
                        column: x => x.InterviewScheduleId,
                        principalTable: "InterviewSchedule",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomEvent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeetingRoomId = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomEvent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomEvent_MeetingRoom_MeetingRoomId",
                        column: x => x.MeetingRoomId,
                        principalTable: "MeetingRoom",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomParticipant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeetingRoomId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    CandidateId = table.Column<int>(type: "int", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PeerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ConnectionState = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomParticipant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomParticipant_MeetingRoom_MeetingRoomId",
                        column: x => x.MeetingRoomId,
                        principalTable: "MeetingRoom",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candidate_Email",
                table: "Candidate",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssignment_InterviewerUserId",
                table: "InterviewAssignment",
                column: "InterviewerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewAssignment_InterviewScheduleId_InterviewerUserId",
                table: "InterviewAssignment",
                columns: new[] { "InterviewScheduleId", "InterviewerUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedule_CandidateId",
                table: "InterviewSchedule",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewSchedule_CreatedByUserId",
                table: "InterviewSchedule",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRoom_InterviewScheduleId",
                table: "MeetingRoom",
                column: "InterviewScheduleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MeetingRoom_RoomCode",
                table: "MeetingRoom",
                column: "RoomCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomEvent_MeetingRoomId",
                table: "RoomEvent",
                column: "MeetingRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomEvent_OccurredAt",
                table: "RoomEvent",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_RoomParticipant_CandidateId",
                table: "RoomParticipant",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomParticipant_MeetingRoomId",
                table: "RoomParticipant",
                column: "MeetingRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_RoomParticipant_UserId",
                table: "RoomParticipant",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InterviewAssignment");

            migrationBuilder.DropTable(
                name: "RoomEvent");

            migrationBuilder.DropTable(
                name: "RoomParticipant");

            migrationBuilder.DropTable(
                name: "MeetingRoom");

            migrationBuilder.DropTable(
                name: "InterviewSchedule");

            migrationBuilder.DropTable(
                name: "Candidate");
        }
    }
}

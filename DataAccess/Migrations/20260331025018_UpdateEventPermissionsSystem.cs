using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEventPermissionsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventCollaborators");

            migrationBuilder.CreateTable(
                name: "EventRoles",
                columns: table => new
                {
                    EventRoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRoles", x => x.EventRoleId);
                    table.ForeignKey(
                        name: "FK_EventRoles_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventRolePolicies",
                columns: table => new
                {
                    EventRoleId = table.Column<int>(type: "int", nullable: false),
                    PolicyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRolePolicies", x => new { x.EventRoleId, x.PolicyId });
                    table.ForeignKey(
                        name: "FK_EventRolePolicies_EventRoles_EventRoleId",
                        column: x => x.EventRoleId,
                        principalTable: "EventRoles",
                        principalColumn: "EventRoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventRolePolicies_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserEventRoles",
                columns: table => new
                {
                    EventMemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    EventRoleId = table.Column<int>(type: "int", nullable: true),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEventRoles", x => x.EventMemberId);
                    table.ForeignKey(
                        name: "FK_UserEventRoles_EventRoles_EventRoleId",
                        column: x => x.EventRoleId,
                        principalTable: "EventRoles",
                        principalColumn: "EventRoleId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserEventRoles_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEventRoles_Users_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_UserEventRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "EventMemberPolicies",
                columns: table => new
                {
                    EventMemberId = table.Column<int>(type: "int", nullable: false),
                    PolicyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventMemberPolicies", x => new { x.EventMemberId, x.PolicyId });
                    table.ForeignKey(
                        name: "FK_EventMemberPolicies_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EventMemberPolicies_UserEventRoles_EventMemberId",
                        column: x => x.EventMemberId,
                        principalTable: "UserEventRoles",
                        principalColumn: "EventMemberId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventMemberPolicies_PolicyId",
                table: "EventMemberPolicies",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_EventRolePolicies_PolicyId",
                table: "EventRolePolicies",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_EventRoles_EventId",
                table: "EventRoles",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEventRoles_AssignedBy",
                table: "UserEventRoles",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserEventRoles_EventId_UserId",
                table: "UserEventRoles",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEventRoles_EventRoleId",
                table: "UserEventRoles",
                column: "EventRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserEventRoles_UserId",
                table: "UserEventRoles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventMemberPolicies");

            migrationBuilder.DropTable(
                name: "EventRolePolicies");

            migrationBuilder.DropTable(
                name: "UserEventRoles");

            migrationBuilder.DropTable(
                name: "EventRoles");

            migrationBuilder.CreateTable(
                name: "EventCollaborators",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Policies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCollaborators", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventCollaborators_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventCollaborators_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventCollaborators_EventId_UserId",
                table: "EventCollaborators",
                columns: new[] { "EventId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventCollaborators_UserId",
                table: "EventCollaborators",
                column: "UserId");
        }
    }
}

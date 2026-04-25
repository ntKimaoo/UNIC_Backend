using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClubRoleRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserClubRoles_ClubRoles_ClubRoleId",
                table: "UserClubRoles");

            migrationBuilder.DropIndex(
                name: "IX_UserClubRoles_ClubRoleId",
                table: "UserClubRoles");

            migrationBuilder.DropColumn(
                name: "ClubRoleId",
                table: "UserClubRoles");

            migrationBuilder.CreateTable(
                name: "UserClubRoleAssignments",
                columns: table => new
                {
                    ClubMemberId = table.Column<int>(type: "int", nullable: false),
                    ClubRoleId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClubRoleAssignments", x => new { x.ClubMemberId, x.ClubRoleId });
                    table.ForeignKey(
                        name: "FK_UserClubRoleAssignments_ClubRoles_ClubRoleId",
                        column: x => x.ClubRoleId,
                        principalTable: "ClubRoles",
                        principalColumn: "ClubRoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserClubRoleAssignments_UserClubRoles_ClubMemberId",
                        column: x => x.ClubMemberId,
                        principalTable: "UserClubRoles",
                        principalColumn: "ClubMemberId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserClubRoleAssignments_ClubRoleId",
                table: "UserClubRoleAssignments",
                column: "ClubRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserClubRoleAssignments");

            migrationBuilder.AddColumn<int>(
                name: "ClubRoleId",
                table: "UserClubRoles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserClubRoles_ClubRoleId",
                table: "UserClubRoles",
                column: "ClubRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserClubRoles_ClubRoles_ClubRoleId",
                table: "UserClubRoles",
                column: "ClubRoleId",
                principalTable: "ClubRoles",
                principalColumn: "ClubRoleId");
        }
    }
}

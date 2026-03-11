using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class updatepoliciesforuser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubMemberPolicies_Users_UserId",
                table: "ClubMemberPolicies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClubMemberPolicies",
                table: "ClubMemberPolicies");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ClubMemberPolicies");

            migrationBuilder.AddColumn<int>(
                name: "ClubMemberId",
                table: "ClubMemberPolicies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClubMemberPolicies",
                table: "ClubMemberPolicies",
                columns: new[] { "ClubMemberId", "PolicyId" });

            migrationBuilder.CreateTable(
                name: "UserPolicies",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPolicies", x => new { x.UserId, x.PolicyId });
                    table.ForeignKey(
                        name: "FK_UserPolicies_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserPolicies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRolePolicies",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PolicyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRolePolicies", x => new { x.RoleId, x.PolicyId });
                    table.ForeignKey(
                        name: "FK_UserRolePolicies_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRolePolicies_UserRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "UserRoles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserPolicies_PolicyId",
                table: "UserPolicies",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRolePolicies_PolicyId",
                table: "UserRolePolicies",
                column: "PolicyId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubMemberPolicies_UserClubRoles_ClubMemberId",
                table: "ClubMemberPolicies",
                column: "ClubMemberId",
                principalTable: "UserClubRoles",
                principalColumn: "ClubMemberId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubMemberPolicies_UserClubRoles_ClubMemberId",
                table: "ClubMemberPolicies");

            migrationBuilder.DropTable(
                name: "UserPolicies");

            migrationBuilder.DropTable(
                name: "UserRolePolicies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ClubMemberPolicies",
                table: "ClubMemberPolicies");

            migrationBuilder.DropColumn(
                name: "ClubMemberId",
                table: "ClubMemberPolicies");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ClubMemberPolicies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_ClubMemberPolicies",
                table: "ClubMemberPolicies",
                columns: new[] { "UserId", "PolicyId" });

            migrationBuilder.AddForeignKey(
                name: "FK_ClubMemberPolicies_Users_UserId",
                table: "ClubMemberPolicies",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class editdepartmentrole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDepartmentRoles");

            migrationBuilder.DropTable(
                name: "DepartmentRoles");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Departments",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "ManagerRoleId",
                table: "Departments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "ClubRoles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ManagerRoleId",
                table: "Departments",
                column: "ManagerRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubRoles_DepartmentId",
                table: "ClubRoles",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubRoles_Departments_DepartmentId",
                table: "ClubRoles",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_ClubRoles_ManagerRoleId",
                table: "Departments",
                column: "ManagerRoleId",
                principalTable: "ClubRoles",
                principalColumn: "ClubRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubRoles_Departments_DepartmentId",
                table: "ClubRoles");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_ClubRoles_ManagerRoleId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_ManagerRoleId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_ClubRoles_DepartmentId",
                table: "ClubRoles");

            migrationBuilder.DropColumn(
                name: "ManagerRoleId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "ClubRoles");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Departments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DepartmentRoles",
                columns: table => new
                {
                    DeptRoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Permissions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentRoles", x => x.DeptRoleId);
                });

            migrationBuilder.CreateTable(
                name: "UserDepartmentRoles",
                columns: table => new
                {
                    DeptMemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssignedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    DeptRoleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDepartmentRoles", x => x.DeptMemberId);
                    table.ForeignKey(
                        name: "FK_UserDepartmentRoles_DepartmentRoles_DeptRoleId",
                        column: x => x.DeptRoleId,
                        principalTable: "DepartmentRoles",
                        principalColumn: "DeptRoleId");
                    table.ForeignKey(
                        name: "FK_UserDepartmentRoles_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId");
                    table.ForeignKey(
                        name: "FK_UserDepartmentRoles_Users_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_UserDepartmentRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentRoles_AssignedBy",
                table: "UserDepartmentRoles",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentRoles_DepartmentId",
                table: "UserDepartmentRoles",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentRoles_DeptRoleId",
                table: "UserDepartmentRoles",
                column: "DeptRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentRoles_UserId",
                table: "UserDepartmentRoles",
                column: "UserId");
        }
    }
}

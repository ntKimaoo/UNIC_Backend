using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class adddepartmentjoin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserClubRoleDepartments",
                columns: table => new
                {
                    ClubMemberId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClubRoleDepartments", x => new { x.ClubMemberId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_UserClubRoleDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserClubRoleDepartments_UserClubRoles_ClubMemberId",
                        column: x => x.ClubMemberId,
                        principalTable: "UserClubRoles",
                        principalColumn: "ClubMemberId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserClubRoleDepartments_DepartmentId",
                table: "UserClubRoleDepartments",
                column: "DepartmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserClubRoleDepartments");
        }
    }
}

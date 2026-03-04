using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class editclubrole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "ClubRoles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubRoles_ClubId",
                table: "ClubRoles",
                column: "ClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubRoles_Clubs_ClubId",
                table: "ClubRoles",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "ClubId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubRoles_Clubs_ClubId",
                table: "ClubRoles");

            migrationBuilder.DropIndex(
                name: "IX_ClubRoles_ClubId",
                table: "ClubRoles");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "ClubRoles");
        }
    }
}

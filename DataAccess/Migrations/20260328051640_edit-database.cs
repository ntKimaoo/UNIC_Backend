using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class editdatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FundCategories_Clubs_ClubId",
                table: "FundCategories");

            migrationBuilder.DropIndex(
                name: "IX_FundCategories_ClubId",
                table: "FundCategories");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "FundCategories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClubId",
                table: "FundCategories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundCategories_ClubId",
                table: "FundCategories",
                column: "ClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_FundCategories_Clubs_ClubId",
                table: "FundCategories",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "ClubId");
        }
    }
}

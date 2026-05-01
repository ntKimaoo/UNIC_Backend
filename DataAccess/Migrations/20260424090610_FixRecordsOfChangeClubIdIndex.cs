using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class FixRecordsOfChangeClubIdIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecordsOfChange_ClubId",
                table: "RecordsOfChange");

            migrationBuilder.CreateIndex(
                name: "IX_RecordsOfChange_ClubId",
                table: "RecordsOfChange",
                column: "ClubId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecordsOfChange_ClubId",
                table: "RecordsOfChange");

            migrationBuilder.CreateIndex(
                name: "IX_RecordsOfChange_ClubId",
                table: "RecordsOfChange",
                column: "ClubId",
                unique: true,
                filter: "[ClubId] IS NOT NULL");
        }
    }
}

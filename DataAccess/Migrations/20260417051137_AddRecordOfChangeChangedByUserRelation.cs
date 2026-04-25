using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordOfChangeChangedByUserRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RecordsOfChange_ChangedBy",
                table: "RecordsOfChange",
                column: "ChangedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_RecordsOfChange_Users_ChangedBy",
                table: "RecordsOfChange",
                column: "ChangedBy",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecordsOfChange_Users_ChangedBy",
                table: "RecordsOfChange");

            migrationBuilder.DropIndex(
                name: "IX_RecordsOfChange_ChangedBy",
                table: "RecordsOfChange");
        }
    }
}

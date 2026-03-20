using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNIC.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceCheckInToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheckInToken",
                table: "Attendances",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CheckInToken",
                table: "Attendances",
                column: "CheckInToken",
                unique: true,
                filter: "[CheckInToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendances_CheckInToken",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckInToken",
                table: "Attendances");
        }
    }
}

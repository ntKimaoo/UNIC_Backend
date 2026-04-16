using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RefactorFundTypesNameOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FundTypes_Code",
                table: "FundTypes");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "FundTypes");

            migrationBuilder.RenameColumn(
                name: "NameVi",
                table: "FundTypes",
                newName: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FundTypes_Name",
                table: "FundTypes",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FundTypes_Name",
                table: "FundTypes");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "FundTypes",
                newName: "NameVi");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "FundTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FundTypes_Code",
                table: "FundTypes",
                column: "Code",
                unique: true);
        }
    }
}

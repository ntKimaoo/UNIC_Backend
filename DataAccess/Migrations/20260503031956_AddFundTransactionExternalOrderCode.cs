using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFundTransactionExternalOrderCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ExternalOrderCode",
                table: "FundTransactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundTransactions_ExternalOrderCode",
                table: "FundTransactions",
                column: "ExternalOrderCode",
                unique: true,
                filter: "[ExternalOrderCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FundTransactions_ExternalOrderCode",
                table: "FundTransactions");

            migrationBuilder.DropColumn(
                name: "ExternalOrderCode",
                table: "FundTransactions");
        }
    }
}

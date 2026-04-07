using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddClubFundExpiresAtAndMemberContribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMemberContribution",
                table: "FundTransactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "ClubFunds",
                type: "date",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE FundTransactions
                SET IsMemberContribution = 1
                WHERE TransactionType = 'INCOME'
                """);
            // sua lai neu ko chay duoc doan sql tren WHERE PaymentLinkId IS NOT NULL AND TransactionType = 'INCOME'
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMemberContribution",
                table: "FundTransactions");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "ClubFunds");
        }
    }
}

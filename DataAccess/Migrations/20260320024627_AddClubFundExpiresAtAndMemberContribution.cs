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
                IF COL_LENGTH('dbo.FundTransactions', 'PaymentLinkId') IS NOT NULL
                    EXEC(N'UPDATE dbo.FundTransactions SET IsMemberContribution = 1 WHERE PaymentLinkId IS NOT NULL AND TransactionType = N''INCOME''');
                """);
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

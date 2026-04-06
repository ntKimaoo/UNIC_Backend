using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFundTransactionCreatedUpdatedAndCreator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "FundTransactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FundTransactions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(
                "UPDATE FundTransactions SET CreatedAt = TransactionDate, UpdatedAt = TransactionDate WHERE CreatedAt < '2000-01-02';");

            migrationBuilder.CreateIndex(
                name: "IX_FundTransactions_CreatedBy",
                table: "FundTransactions",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_FundTransactions_Users_CreatedBy",
                table: "FundTransactions",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FundTransactions_Users_CreatedBy",
                table: "FundTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FundTransactions_CreatedBy",
                table: "FundTransactions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "FundTransactions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FundTransactions");
        }
    }
}

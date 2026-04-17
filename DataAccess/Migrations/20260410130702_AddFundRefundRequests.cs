using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFundRefundRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RefundForTransactionId",
                table: "FundTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FundRefundRequests",
                columns: table => new
                {
                    RefundRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    OriginalTransactionId = table.Column<int>(type: "int", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AccountHolderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TransferReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ManagerNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundRefundRequests", x => x.RefundRequestId);
                    table.ForeignKey(
                        name: "FK_FundRefundRequests_ClubFunds_FundId",
                        column: x => x.FundId,
                        principalTable: "ClubFunds",
                        principalColumn: "FundId");
                    table.ForeignKey(
                        name: "FK_FundRefundRequests_FundTransactions_OriginalTransactionId",
                        column: x => x.OriginalTransactionId,
                        principalTable: "FundTransactions",
                        principalColumn: "TransactionId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundTransactions_RefundForTransactionId",
                table: "FundTransactions",
                column: "RefundForTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_FundRefundRequests_ClubId_Status",
                table: "FundRefundRequests",
                columns: new[] { "ClubId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FundRefundRequests_FundId",
                table: "FundRefundRequests",
                column: "FundId");

            migrationBuilder.CreateIndex(
                name: "IX_FundRefundRequests_OriginalTransactionId",
                table: "FundRefundRequests",
                column: "OriginalTransactionId",
                unique: true,
                filter: "[Status] = N'PENDING'");

            migrationBuilder.AddForeignKey(
                name: "FK_FundTransactions_FundTransactions_RefundForTransactionId",
                table: "FundTransactions",
                column: "RefundForTransactionId",
                principalTable: "FundTransactions",
                principalColumn: "TransactionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FundTransactions_FundTransactions_RefundForTransactionId",
                table: "FundTransactions");

            migrationBuilder.DropTable(
                name: "FundRefundRequests");

            migrationBuilder.DropIndex(
                name: "IX_FundTransactions_RefundForTransactionId",
                table: "FundTransactions");

            migrationBuilder.DropColumn(
                name: "RefundForTransactionId",
                table: "FundTransactions");
        }
    }
}

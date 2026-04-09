using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class EnsureFundTransactionPaymentLinkId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration 20260316000000_AddFundTransactionPaymentLinkId không nằm trong chuỗi Designer nên cột chưa từng được tạo khi migrate từ đầu.
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.FundTransactions', 'PaymentLinkId') IS NULL
                BEGIN
                    ALTER TABLE [FundTransactions] ADD [PaymentLinkId] nvarchar(100) NULL;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH('dbo.FundTransactions', 'PaymentLinkId') IS NOT NULL
                BEGIN
                    ALTER TABLE [FundTransactions] DROP COLUMN [PaymentLinkId];
                END
                """);
        }
    }
}

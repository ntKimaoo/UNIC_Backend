using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFundCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FundTransactions_FundCollections_CollectionId",
                table: "FundTransactions");

            migrationBuilder.DropTable(
                name: "FundCollections");

            migrationBuilder.DropIndex(
                name: "IX_FundTransactions_CollectionId",
                table: "FundTransactions");

            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "FundTransactions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CollectionId",
                table: "FundTransactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FundCollections",
                columns: table => new
                {
                    CollectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SuggestedAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    TargetAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundCollections", x => x.CollectionId);
                    table.ForeignKey(
                        name: "FK_FundCollections_ClubFunds_FundId",
                        column: x => x.FundId,
                        principalTable: "ClubFunds",
                        principalColumn: "FundId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundTransactions_CollectionId",
                table: "FundTransactions",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_FundCollections_FundId",
                table: "FundCollections",
                column: "FundId");

            migrationBuilder.AddForeignKey(
                name: "FK_FundTransactions_FundCollections_CollectionId",
                table: "FundTransactions",
                column: "CollectionId",
                principalTable: "FundCollections",
                principalColumn: "CollectionId",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddClubFundPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PublicId",
                table: "ClubFunds",
                type: "uniqueidentifier",
                nullable: true);

            // Backfill existing rows with unique values before adding the unique index.
            migrationBuilder.Sql("""
UPDATE ClubFunds
SET PublicId = NEWID()
WHERE PublicId IS NULL
""");

            migrationBuilder.AlterColumn<Guid>(
                name: "PublicId",
                table: "ClubFunds",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()",
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubFunds_PublicId",
                table: "ClubFunds",
                column: "PublicId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClubFunds_PublicId",
                table: "ClubFunds");

            migrationBuilder.DropColumn(
                name: "PublicId",
                table: "ClubFunds");
        }
    }
}


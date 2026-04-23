using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFundTypesAndGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundTypes",
                columns: table => new
                {
                    FundTypeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameVi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundTypes", x => x.FundTypeId);
                });

            migrationBuilder.InsertData(
                table: "FundTypes",
                columns: new[] { "FundTypeId", "Code", "NameVi", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { 1, "GENERAL", "Quỹ chung", true, 0 },
                    { 2, "EVENT", "Quỹ sự kiện", true, 1 },
                    { 3, "DONATION", "Quỹ quyên góp", true, 2 }
                });

            migrationBuilder.AddColumn<int>(
                name: "FundTypeId",
                table: "ClubFunds",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "GoalAmount",
                table: "ClubFunds",
                type: "decimal(15,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubFunds_FundTypeId",
                table: "ClubFunds",
                column: "FundTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_FundTypes_Code",
                table: "FundTypes",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubFunds_FundTypes_FundTypeId",
                table: "ClubFunds",
                column: "FundTypeId",
                principalTable: "FundTypes",
                principalColumn: "FundTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubFunds_FundTypes_FundTypeId",
                table: "ClubFunds");

            migrationBuilder.DropTable(
                name: "FundTypes");

            migrationBuilder.DropIndex(
                name: "IX_ClubFunds_FundTypeId",
                table: "ClubFunds");

            migrationBuilder.DropColumn(
                name: "FundTypeId",
                table: "ClubFunds");

            migrationBuilder.DropColumn(
                name: "GoalAmount",
                table: "ClubFunds");
        }
    }
}

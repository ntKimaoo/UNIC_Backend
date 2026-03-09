using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class addpolicygroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PolicyGroupId",
                table: "Policies",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PolicyGroups",
                columns: table => new
                {
                    PolicyGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyGroups", x => x.PolicyGroupId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Policies_PolicyGroupId",
                table: "Policies",
                column: "PolicyGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Policies_PolicyGroups_PolicyGroupId",
                table: "Policies",
                column: "PolicyGroupId",
                principalTable: "PolicyGroups",
                principalColumn: "PolicyGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Policies_PolicyGroups_PolicyGroupId",
                table: "Policies");

            migrationBuilder.DropTable(
                name: "PolicyGroups");

            migrationBuilder.DropIndex(
                name: "IX_Policies_PolicyGroupId",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "PolicyGroupId",
                table: "Policies");
        }
    }
}

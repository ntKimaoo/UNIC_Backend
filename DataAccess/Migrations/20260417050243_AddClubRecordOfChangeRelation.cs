using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddClubRecordOfChangeRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecordsOfChange",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    PropertyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClubId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordsOfChange", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecordsOfChange_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "ClubId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecordsOfChange_ClubId",
                table: "RecordsOfChange",
                column: "ClubId",
                unique: true,
                filter: "[ClubId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecordsOfChange");
        }
    }
}

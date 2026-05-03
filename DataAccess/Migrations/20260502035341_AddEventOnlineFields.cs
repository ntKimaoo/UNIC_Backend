using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEventOnlineFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Events') AND name = 'IsOnline')
                    ALTER TABLE [Events] ADD [IsOnline] bit NOT NULL DEFAULT 0;
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Events') AND name = 'MeetLink')
                    ALTER TABLE [Events] ADD [MeetLink] nvarchar(max) NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MeetLink",
                table: "Events");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingEventAndAttendanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // CheckInCode already exists - skip
            // CodeExpiresAt already exists - skip
            // CheckInTime already exists - skip
            // Score already exists - skip

            // Add only missing columns to Events table
            migrationBuilder.AddColumn<int>(
                name: "MaxAttendees",
                table: "Events",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationEndDate",
                table: "Events",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegistrationStartDate",
                table: "Events",
                type: "datetime2",
                nullable: true);

            // Rename Notes to Comment in Attendances table
            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "Attendances",
                newName: "Comment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Only drop columns that were added in Up method
            migrationBuilder.DropColumn(
                name: "MaxAttendees",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegistrationEndDate",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RegistrationStartDate",
                table: "Events");

            // Rename Comment back to Notes
            migrationBuilder.RenameColumn(
                name: "Comment",
                table: "Attendances",
                newName: "Notes");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class LinkClubPostToEventAndCampaign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "ClubPosts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EventId",
                table: "ClubPosts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClubPosts_CampaignId",
                table: "ClubPosts",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubPosts_EventId",
                table: "ClubPosts",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClubPosts_Events_EventId",
                table: "ClubPosts",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "EventId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ClubPosts_RecruitmentCampaigns_CampaignId",
                table: "ClubPosts",
                column: "CampaignId",
                principalTable: "RecruitmentCampaigns",
                principalColumn: "CampaignId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClubPosts_Events_EventId",
                table: "ClubPosts");

            migrationBuilder.DropForeignKey(
                name: "FK_ClubPosts_RecruitmentCampaigns_CampaignId",
                table: "ClubPosts");

            migrationBuilder.DropIndex(
                name: "IX_ClubPosts_CampaignId",
                table: "ClubPosts");

            migrationBuilder.DropIndex(
                name: "IX_ClubPosts_EventId",
                table: "ClubPosts");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "ClubPosts");

            migrationBuilder.DropColumn(
                name: "EventId",
                table: "ClubPosts");
        }
    }
}

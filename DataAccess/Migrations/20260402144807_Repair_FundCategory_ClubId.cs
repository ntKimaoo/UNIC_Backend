using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    public partial class Repair_FundCategory_ClubId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.FundCategories', N'ClubId') IS NULL
BEGIN
    ALTER TABLE [dbo].[FundCategories] ADD [ClubId] int NULL;
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.name = N'IX_FundCategories_ClubId'
      AND i.object_id = OBJECT_ID(N'dbo.FundCategories')
)
BEGIN
    CREATE INDEX [IX_FundCategories_ClubId] ON [dbo].[FundCategories] ([ClubId]);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    WHERE fk.name = N'FK_FundCategories_Clubs_ClubId'
      AND fk.parent_object_id = OBJECT_ID(N'dbo.FundCategories')
)
BEGIN
    ALTER TABLE [dbo].[FundCategories] WITH CHECK
    ADD CONSTRAINT [FK_FundCategories_Clubs_ClubId]
    FOREIGN KEY([ClubId]) REFERENCES [dbo].[Clubs] ([ClubId]);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    WHERE fk.name = N'FK_FundCategories_Clubs_ClubId'
      AND fk.parent_object_id = OBJECT_ID(N'dbo.FundCategories')
)
BEGIN
    ALTER TABLE [dbo].[FundCategories] DROP CONSTRAINT [FK_FundCategories_Clubs_ClubId];
END
");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.name = N'IX_FundCategories_ClubId'
      AND i.object_id = OBJECT_ID(N'dbo.FundCategories')
)
BEGIN
    DROP INDEX [IX_FundCategories_ClubId] ON [dbo].[FundCategories];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH(N'dbo.FundCategories', N'ClubId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[FundCategories] DROP COLUMN [ClubId];
END
");
        }
    }
}

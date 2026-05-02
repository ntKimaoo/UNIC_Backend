using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SyncEventMemberModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old FK if it exists (Cascade → NoAction)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EventMembers_Users_UserId')
                    ALTER TABLE [EventMembers] DROP CONSTRAINT [FK_EventMembers_Users_UserId];
            ");

            // Drop old index if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventMembers_EventId' AND object_id = OBJECT_ID('EventMembers'))
                    DROP INDEX [IX_EventMembers_EventId] ON [EventMembers];
            ");

            // Create unique composite index (EventId, UserId)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventMembers_EventId_UserId' AND object_id = OBJECT_ID('EventMembers'))
                    CREATE UNIQUE INDEX [IX_EventMembers_EventId_UserId] ON [EventMembers] ([EventId], [UserId]);
            ");

            // Re-add FK without cascade
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EventMembers_Users_UserId')
                    ALTER TABLE [EventMembers] ADD CONSTRAINT [FK_EventMembers_Users_UserId]
                        FOREIGN KEY ([UserId]) REFERENCES [Users]([UserId]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EventMembers_Users_UserId')
                    ALTER TABLE [EventMembers] DROP CONSTRAINT [FK_EventMembers_Users_UserId];
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventMembers_EventId_UserId' AND object_id = OBJECT_ID('EventMembers'))
                    DROP INDEX [IX_EventMembers_EventId_UserId] ON [EventMembers];
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventMembers_EventId' AND object_id = OBJECT_ID('EventMembers'))
                    CREATE INDEX [IX_EventMembers_EventId] ON [EventMembers] ([EventId]);
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_EventMembers_Users_UserId')
                    ALTER TABLE [EventMembers] ADD CONSTRAINT [FK_EventMembers_Users_UserId]
                        FOREIGN KEY ([UserId]) REFERENCES [Users]([UserId]) ON DELETE CASCADE;
            ");
        }
    }
}

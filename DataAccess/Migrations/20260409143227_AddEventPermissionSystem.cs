using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEventPermissionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EventRoles')
BEGIN
    CREATE TABLE [EventRoles] (
        [EventRoleId] int NOT NULL IDENTITY,
        [EventId] int NOT NULL,
        [RoleName] nvarchar(50) NOT NULL,
        [Description] nvarchar(max) NULL,
        [Level] int NOT NULL,
        CONSTRAINT [PK_EventRoles] PRIMARY KEY ([EventRoleId]),
        CONSTRAINT [FK_EventRoles_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId]) ON DELETE CASCADE
    );
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventRoles_EventId' AND object_id = OBJECT_ID('EventRoles'))
    CREATE INDEX [IX_EventRoles_EventId] ON [EventRoles] ([EventId]);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EventMembers')
BEGIN
    CREATE TABLE [EventMembers] (
        [EventMemberId] int NOT NULL IDENTITY,
        [EventId] int NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [EventRoleId] int NULL,
        [AssignedBy] uniqueidentifier NULL,
        [AssignedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_EventMembers] PRIMARY KEY ([EventMemberId]),
        CONSTRAINT [FK_EventMembers_EventRoles_EventRoleId] FOREIGN KEY ([EventRoleId]) REFERENCES [EventRoles] ([EventRoleId]),
        CONSTRAINT [FK_EventMembers_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId]) ON DELETE CASCADE,
        CONSTRAINT [FK_EventMembers_Users_AssignedBy] FOREIGN KEY ([AssignedBy]) REFERENCES [Users] ([UserId]),
        CONSTRAINT [FK_EventMembers_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
    );
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventMembers_AssignedBy' AND object_id = OBJECT_ID('EventMembers'))
    CREATE INDEX [IX_EventMembers_AssignedBy] ON [EventMembers] ([AssignedBy]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventMembers_EventId_UserId' AND object_id = OBJECT_ID('EventMembers'))
    CREATE UNIQUE INDEX [IX_EventMembers_EventId_UserId] ON [EventMembers] ([EventId], [UserId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventMembers_EventRoleId' AND object_id = OBJECT_ID('EventMembers'))
    CREATE INDEX [IX_EventMembers_EventRoleId] ON [EventMembers] ([EventRoleId]);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventMembers_UserId' AND object_id = OBJECT_ID('EventMembers'))
    CREATE INDEX [IX_EventMembers_UserId] ON [EventMembers] ([UserId]);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EventRolePolicies')
BEGIN
    CREATE TABLE [EventRolePolicies] (
        [EventRoleId] int NOT NULL,
        [PolicyId] int NOT NULL,
        CONSTRAINT [PK_EventRolePolicies] PRIMARY KEY ([EventRoleId], [PolicyId]),
        CONSTRAINT [FK_EventRolePolicies_EventRoles_EventRoleId] FOREIGN KEY ([EventRoleId]) REFERENCES [EventRoles] ([EventRoleId]) ON DELETE CASCADE,
        CONSTRAINT [FK_EventRolePolicies_Policies_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [Policies] ([Id]) ON DELETE CASCADE
    );
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventRolePolicies_PolicyId' AND object_id = OBJECT_ID('EventRolePolicies'))
    CREATE INDEX [IX_EventRolePolicies_PolicyId] ON [EventRolePolicies] ([PolicyId]);
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EventMemberPolicies')
BEGIN
    CREATE TABLE [EventMemberPolicies] (
        [EventMemberId] int NOT NULL,
        [PolicyId] int NOT NULL,
        CONSTRAINT [PK_EventMemberPolicies] PRIMARY KEY ([EventMemberId], [PolicyId]),
        CONSTRAINT [FK_EventMemberPolicies_EventMembers_EventMemberId] FOREIGN KEY ([EventMemberId]) REFERENCES [EventMembers] ([EventMemberId]) ON DELETE CASCADE,
        CONSTRAINT [FK_EventMemberPolicies_Policies_PolicyId] FOREIGN KEY ([PolicyId]) REFERENCES [Policies] ([Id]) ON DELETE CASCADE
    );
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EventMemberPolicies_PolicyId' AND object_id = OBJECT_ID('EventMemberPolicies'))
    CREATE INDEX [IX_EventMemberPolicies_PolicyId] ON [EventMemberPolicies] ([PolicyId]);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventMemberPolicies");

            migrationBuilder.DropTable(
                name: "EventRolePolicies");

            migrationBuilder.DropTable(
                name: "EventMembers");

            migrationBuilder.DropTable(
                name: "EventRoles");
        }
    }
}

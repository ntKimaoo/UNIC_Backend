using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UNIC.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class initalCreateDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClubRoles",
                columns: table => new
                {
                    ClubRoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubRoles", x => x.ClubRoleId);
                });

            migrationBuilder.CreateTable(
                name: "Clubs",
                columns: table => new
                {
                    ClubId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ShortName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FoundedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FacebookUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clubs", x => x.ClubId);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentRoles",
                columns: table => new
                {
                    DeptRoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Permissions = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentRoles", x => x.DeptRoleId);
                });

            migrationBuilder.CreateTable(
                name: "FundCategories",
                columns: table => new
                {
                    CategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundCategories", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Policies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Policies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Avatar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StudentID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Major = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    JoinDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Users__0CF04B38FFE70BBA", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "ClubFunds",
                columns: table => new
                {
                    FundId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    FundName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubFunds", x => x.FundId);
                    table.ForeignKey(
                        name: "FK_ClubFunds_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "ClubId");
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.DepartmentId);
                    table.ForeignKey(
                        name: "FK_Departments_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "ClubId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RegistrationStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RegistrationEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxAttendees = table.Column<int>(type: "int", nullable: true),
                    CheckInCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CodeExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_Events_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "ClubId");
                });

            migrationBuilder.CreateTable(
                name: "RecruitmentCampaigns",
                columns: table => new
                {
                    CampaignId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    CampaignName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LinkCampaign = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruitmentCampaigns", x => x.CampaignId);
                    table.ForeignKey(
                        name: "FK_RecruitmentCampaigns_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "ClubId");
                });

            migrationBuilder.CreateTable(
                name: "ClubRolePolicies",
                columns: table => new
                {
                    ClubRoleId = table.Column<int>(type: "int", nullable: false),
                    PolicyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubRolePolicies", x => new { x.ClubRoleId, x.PolicyId });
                    table.ForeignKey(
                        name: "FK_ClubRolePolicies_ClubRoles_ClubRoleId",
                        column: x => x.ClubRoleId,
                        principalTable: "ClubRoles",
                        principalColumn: "ClubRoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClubRolePolicies_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubMemberPolicies",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubMemberPolicies", x => new { x.UserId, x.PolicyId });
                    table.ForeignKey(
                        name: "FK_ClubMemberPolicies_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClubMemberPolicies_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClubPosts",
                columns: table => new
                {
                    PostId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClubPosts", x => x.PostId);
                    table.ForeignKey(
                        name: "FK_ClubPosts_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "ClubId");
                    table.ForeignKey(
                        name: "FK_ClubPosts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EmailVerificationTokens",
                columns: table => new
                {
                    EmailVerificationTokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__EmailVer__B16196D29A5849AE", x => x.EmailVerificationTokenId);
                    table.ForeignKey(
                        name: "FK__EmailVeri__User__45F365D3",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    PasswordResetTokenId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true, defaultValueSql: "(getutcdate())"),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Password__160661284C508CB5", x => x.PasswordResetTokenId);
                    table.ForeignKey(
                        name: "FK__PasswordR__User__412EB0B6",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    RefreshTokenID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DeviceInfo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    RevokedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RefreshT__F5845E595E9566E1", x => x.RefreshTokenID);
                    table.ForeignKey(
                        name: "FK__RefreshTo__User__3C69FB99",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "UserClubRoles",
                columns: table => new
                {
                    ClubMemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClubId = table.Column<int>(type: "int", nullable: false),
                    ClubRoleId = table.Column<int>(type: "int", nullable: false),
                    JoinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClubRoles", x => x.ClubMemberId);
                    table.ForeignKey(
                        name: "FK_UserClubRoles_ClubRoles_ClubRoleId",
                        column: x => x.ClubRoleId,
                        principalTable: "ClubRoles",
                        principalColumn: "ClubRoleId");
                    table.ForeignKey(
                        name: "FK_UserClubRoles_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "ClubId");
                    table.ForeignKey(
                        name: "FK_UserClubRoles_Users_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_UserClubRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.RoleId);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    TransactionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundTransactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_FundTransactions_ClubFunds_FundId",
                        column: x => x.FundId,
                        principalTable: "ClubFunds",
                        principalColumn: "FundId");
                    table.ForeignKey(
                        name: "FK_FundTransactions_FundCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FundCategories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserDepartmentRoles",
                columns: table => new
                {
                    DeptMemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    DeptRoleId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDepartmentRoles", x => x.DeptMemberId);
                    table.ForeignKey(
                        name: "FK_UserDepartmentRoles_DepartmentRoles_DeptRoleId",
                        column: x => x.DeptRoleId,
                        principalTable: "DepartmentRoles",
                        principalColumn: "DeptRoleId");
                    table.ForeignKey(
                        name: "FK_UserDepartmentRoles_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId");
                    table.ForeignKey(
                        name: "FK_UserDepartmentRoles_Users_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_UserDepartmentRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attendances",
                columns: table => new
                {
                    AttendId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttendanceStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Score = table.Column<int>(type: "int", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendances", x => x.AttendId);
                    table.ForeignKey(
                        name: "FK_Attendances_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId");
                    table.ForeignKey(
                        name: "FK_Attendances_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "EventBudgets",
                columns: table => new
                {
                    BudgetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    SpendName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BudgetAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    SpentAmount = table.Column<decimal>(type: "decimal(15,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventBudgets", x => x.BudgetId);
                    table.ForeignKey(
                        name: "FK_EventBudgets_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventImages",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventImages", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_EventImages_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId");
                });

            migrationBuilder.CreateTable(
                name: "EventSchedules",
                columns: table => new
                {
                    ScheduleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    ScheduleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSchedules", x => x.ScheduleId);
                    table.ForeignKey(
                        name: "FK_EventSchedules_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationForms",
                columns: table => new
                {
                    FormId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignId = table.Column<int>(type: "int", nullable: false),
                    FormName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FormTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationForms", x => x.FormId);
                    table.ForeignKey(
                        name: "FK_ApplicationForms_RecruitmentCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "RecruitmentCampaigns",
                        principalColumn: "CampaignId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleDetails",
                columns: table => new
                {
                    DetailId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleId = table.Column<int>(type: "int", nullable: false),
                    ActivityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleDetails", x => x.DetailId);
                    table.ForeignKey(
                        name: "FK_ScheduleDetails_EventSchedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "EventSchedules",
                        principalColumn: "ScheduleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationQuestions",
                columns: table => new
                {
                    QuestionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationQuestions", x => x.QuestionId);
                    table.ForeignKey(
                        name: "FK_ApplicationQuestions_ApplicationForms_FormId",
                        column: x => x.FormId,
                        principalTable: "ApplicationForms",
                        principalColumn: "FormId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FormId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.ApplicationId);
                    table.ForeignKey(
                        name: "FK_Applications_ApplicationForms_FormId",
                        column: x => x.FormId,
                        principalTable: "ApplicationForms",
                        principalColumn: "FormId");
                    table.ForeignKey(
                        name: "FK_Applications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationAnswers",
                columns: table => new
                {
                    AnswerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    AnswerText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationAnswers", x => x.AnswerId);
                    table.ForeignKey(
                        name: "FK_ApplicationAnswers_ApplicationQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "ApplicationQuestions",
                        principalColumn: "QuestionId");
                    table.ForeignKey(
                        name: "FK_ApplicationAnswers_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "ApplicationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationAnswers_ApplicationId",
                table: "ApplicationAnswers",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationAnswers_QuestionId",
                table: "ApplicationAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForms_CampaignId",
                table: "ApplicationForms",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationQuestions_FormId",
                table: "ApplicationQuestions",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_FormId",
                table: "Applications",
                column: "FormId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Status",
                table: "Applications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_UserId",
                table: "Applications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EventId",
                table: "Attendances",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_UserId",
                table: "Attendances",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubFunds_ClubId",
                table: "ClubFunds",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubMemberPolicies_PolicyId",
                table: "ClubMemberPolicies",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubPosts_ClubId",
                table: "ClubPosts",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubPosts_PostDate",
                table: "ClubPosts",
                column: "PostDate");

            migrationBuilder.CreateIndex(
                name: "IX_ClubPosts_UserId",
                table: "ClubPosts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClubRolePolicies_PolicyId",
                table: "ClubRolePolicies",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_ClubName",
                table: "Clubs",
                column: "ClubName");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ClubId",
                table: "Departments",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_TokenHash",
                table: "EmailVerificationTokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationTokens_UserId",
                table: "EmailVerificationTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventBudgets_EventId",
                table: "EventBudgets",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventImages_EventId",
                table: "EventImages",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ClubId",
                table: "Events",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_StartDate",
                table: "Events",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_EventSchedules_EventId",
                table: "EventSchedules",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_FundTransactions_CategoryId",
                table: "FundTransactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FundTransactions_FundId",
                table: "FundTransactions",
                column: "FundId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenHash",
                table: "PasswordResetTokens",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_UserId",
                table: "PasswordResetTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentCampaigns_ClubId",
                table: "RecruitmentCampaigns",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserID",
                table: "RefreshTokens",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleDetails_ScheduleId",
                table: "ScheduleDetails",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClubRoles_AssignedBy",
                table: "UserClubRoles",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserClubRoles_ClubId",
                table: "UserClubRoles",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClubRoles_ClubRoleId",
                table: "UserClubRoles",
                column: "ClubRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserClubRoles_UserId",
                table: "UserClubRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentRoles_AssignedBy",
                table: "UserDepartmentRoles",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentRoles_DepartmentId",
                table: "UserDepartmentRoles",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentRoles_DeptRoleId",
                table: "UserDepartmentRoles",
                column: "DeptRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDepartmentRoles_UserId",
                table: "UserDepartmentRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__Users__A9D105345C1FCA58",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationAnswers");

            migrationBuilder.DropTable(
                name: "Attendances");

            migrationBuilder.DropTable(
                name: "ClubMemberPolicies");

            migrationBuilder.DropTable(
                name: "ClubPosts");

            migrationBuilder.DropTable(
                name: "ClubRolePolicies");

            migrationBuilder.DropTable(
                name: "EmailVerificationTokens");

            migrationBuilder.DropTable(
                name: "EventBudgets");

            migrationBuilder.DropTable(
                name: "EventImages");

            migrationBuilder.DropTable(
                name: "FundTransactions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ScheduleDetails");

            migrationBuilder.DropTable(
                name: "UserClubRoles");

            migrationBuilder.DropTable(
                name: "UserDepartmentRoles");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "ApplicationQuestions");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "Policies");

            migrationBuilder.DropTable(
                name: "ClubFunds");

            migrationBuilder.DropTable(
                name: "FundCategories");

            migrationBuilder.DropTable(
                name: "EventSchedules");

            migrationBuilder.DropTable(
                name: "ClubRoles");

            migrationBuilder.DropTable(
                name: "DepartmentRoles");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "ApplicationForms");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "RecruitmentCampaigns");

            migrationBuilder.DropTable(
                name: "Clubs");
        }
    }
}

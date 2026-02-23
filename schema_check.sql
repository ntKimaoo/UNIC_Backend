CREATE TABLE [ClubRoles] (
    [ClubRoleId] int NOT NULL IDENTITY,
    [RoleName] nvarchar(50) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Permissions] nvarchar(max) NOT NULL,
    [Level] int NOT NULL,
    CONSTRAINT [PK_ClubRoles] PRIMARY KEY ([ClubRoleId])
);
GO


CREATE TABLE [Clubs] (
    [ClubId] int NOT NULL IDENTITY,
    [ClubName] nvarchar(100) NOT NULL,
    [ShortName] nvarchar(50) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [FoundedDate] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [IsPublic] bit NOT NULL,
    [LogoUrl] nvarchar(max) NOT NULL,
    [CoverImageUrl] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(max) NOT NULL,
    [FacebookUrl] nvarchar(max) NOT NULL,
    [WebsiteUrl] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NOT NULL,
    [MemberCount] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_Clubs] PRIMARY KEY ([ClubId])
);
GO


CREATE TABLE [DepartmentRoles] (
    [DeptRoleId] int NOT NULL IDENTITY,
    [RoleName] nvarchar(50) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Permissions] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_DepartmentRoles] PRIMARY KEY ([DeptRoleId])
);
GO


CREATE TABLE [FundCategories] (
    [CategoryId] int NOT NULL IDENTITY,
    [CategoryName] nvarchar(100) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_FundCategories] PRIMARY KEY ([CategoryId])
);
GO


CREATE TABLE [Users] (
    [UserId] uniqueidentifier NOT NULL,
    [FullName] nvarchar(200) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [DateOfBirth] date NULL,
    [Gender] nvarchar(10) NULL,
    [Address] nvarchar(500) NULL,
    [Avatar] nvarchar(500) NULL,
    [StudentID] nvarchar(50) NULL,
    [Major] nvarchar(200) NULL,
    [JoinDate] date NULL,
    [Status] nvarchar(50) NULL,
    [PasswordHash] nvarchar(500) NULL,
    [CreatedAt] datetime NULL,
    [UpdatedAt] datetime NULL,
    CONSTRAINT [PK__Users__0CF04B38FFE70BBA] PRIMARY KEY ([UserId])
);
GO


CREATE TABLE [ClubFunds] (
    [FundId] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [FundName] nvarchar(100) NOT NULL,
    [TotalAmount] decimal(15,2) NOT NULL,
    [CurrentBalance] decimal(15,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ClubFunds] PRIMARY KEY ([FundId]),
    CONSTRAINT [FK_ClubFunds_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([ClubId])
);
GO


CREATE TABLE [Departments] (
    [DepartmentId] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [DepartmentName] nvarchar(100) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([DepartmentId]),
    CONSTRAINT [FK_Departments_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([ClubId]) ON DELETE CASCADE
);
GO


CREATE TABLE [Events] (
    [EventId] int NOT NULL IDENTITY,
    [ClubId] int NULL,
    [EventName] nvarchar(200) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    [Location] nvarchar(200) NOT NULL,
    [StartDate] datetime2 NULL,
    [EndDate] datetime2 NULL,
    [IsPublic] bit NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [RegistrationStartDate] datetime2 NULL,
    [RegistrationEndDate] datetime2 NULL,
    [MaxAttendees] int NULL,
    [CheckInCode] nvarchar(10) NULL,
    [CodeExpiresAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Events] PRIMARY KEY ([EventId]),
    CONSTRAINT [FK_Events_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([ClubId])
);
GO


CREATE TABLE [RecruitmentCampaigns] (
    [CampaignId] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [CampaignName] nvarchar(200) NOT NULL,
    [LinkCampaign] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [StartDate] datetime2 NULL,
    [EndDate] datetime2 NULL,
    [Status] nvarchar(20) NOT NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_RecruitmentCampaigns] PRIMARY KEY ([CampaignId]),
    CONSTRAINT [FK_RecruitmentCampaigns_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([ClubId])
);
GO


CREATE TABLE [ClubPosts] (
    [PostId] int NOT NULL IDENTITY,
    [ClubId] int NOT NULL,
    [UserId] uniqueidentifier NULL,
    [Title] nvarchar(200) NOT NULL,
    [ImageUrl] nvarchar(max) NOT NULL,
    [Caption] nvarchar(max) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [PostDate] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_ClubPosts] PRIMARY KEY ([PostId]),
    CONSTRAINT [FK_ClubPosts_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([ClubId]),
    CONSTRAINT [FK_ClubPosts_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE SET NULL
);
GO


CREATE TABLE [EmailVerificationTokens] (
    [EmailVerificationTokenId] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(255) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsUsed] bit NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NULL DEFAULT ((getutcdate())),
    [UsedAt] datetime2 NULL,
    CONSTRAINT [PK__EmailVer__B16196D29A5849AE] PRIMARY KEY ([EmailVerificationTokenId]),
    CONSTRAINT [FK__EmailVeri__User__45F365D3] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);
GO


CREATE TABLE [Notifications] (
    [NotificationId] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [Type] nvarchar(50) NOT NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationId]),
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);
GO


CREATE TABLE [PasswordResetTokens] (
    [PasswordResetTokenId] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(255) NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [IsUsed] bit NOT NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime2 NULL DEFAULT ((getutcdate())),
    [UsedAt] datetime2 NULL,
    CONSTRAINT [PK__Password__160661284C508CB5] PRIMARY KEY ([PasswordResetTokenId]),
    CONSTRAINT [FK__PasswordR__User__412EB0B6] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);
GO


CREATE TABLE [RefreshTokens] (
    [RefreshTokenID] int NOT NULL IDENTITY,
    [UserID] uniqueidentifier NOT NULL,
    [TokenHash] nvarchar(500) NOT NULL,
    [DeviceInfo] nvarchar(255) NULL,
    [IPAddress] nvarchar(50) NULL,
    [ExpiresAt] datetime NOT NULL,
    [IsRevoked] bit NULL DEFAULT CAST(0 AS bit),
    [CreatedAt] datetime NULL DEFAULT ((getdate())),
    [RevokedAt] datetime NULL,
    CONSTRAINT [PK__RefreshT__F5845E595E9566E1] PRIMARY KEY ([RefreshTokenID]),
    CONSTRAINT [FK__RefreshTo__User__3C69FB99] FOREIGN KEY ([UserID]) REFERENCES [Users] ([UserId])
);
GO


CREATE TABLE [UserClubRoles] (
    [ClubMemberId] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [ClubId] int NOT NULL,
    [ClubRoleId] int NOT NULL,
    [JoinDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [AssignedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_UserClubRoles] PRIMARY KEY ([ClubMemberId]),
    CONSTRAINT [FK_UserClubRoles_ClubRoles_ClubRoleId] FOREIGN KEY ([ClubRoleId]) REFERENCES [ClubRoles] ([ClubRoleId]),
    CONSTRAINT [FK_UserClubRoles_Clubs_ClubId] FOREIGN KEY ([ClubId]) REFERENCES [Clubs] ([ClubId]),
    CONSTRAINT [FK_UserClubRoles_Users_AssignedBy] FOREIGN KEY ([AssignedBy]) REFERENCES [Users] ([UserId]),
    CONSTRAINT [FK_UserClubRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);
GO


CREATE TABLE [UserRoles] (
    [RoleId] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [RoleName] nvarchar(50) NOT NULL,
    [AssignedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([RoleId]),
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);
GO


CREATE TABLE [FundTransactions] (
    [TransactionId] int NOT NULL IDENTITY,
    [FundId] int NOT NULL,
    [CategoryId] int NULL,
    [TransactionType] nvarchar(20) NOT NULL,
    [Amount] decimal(15,2) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [TransactionDate] datetime2 NOT NULL,
    CONSTRAINT [PK_FundTransactions] PRIMARY KEY ([TransactionId]),
    CONSTRAINT [FK_FundTransactions_ClubFunds_FundId] FOREIGN KEY ([FundId]) REFERENCES [ClubFunds] ([FundId]),
    CONSTRAINT [FK_FundTransactions_FundCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [FundCategories] ([CategoryId]) ON DELETE SET NULL
);
GO


CREATE TABLE [UserDepartmentRoles] (
    [DeptMemberId] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [DepartmentId] int NOT NULL,
    [DeptRoleId] int NOT NULL,
    [AssignedAt] datetime2 NOT NULL,
    [AssignedBy] uniqueidentifier NULL,
    CONSTRAINT [PK_UserDepartmentRoles] PRIMARY KEY ([DeptMemberId]),
    CONSTRAINT [FK_UserDepartmentRoles_DepartmentRoles_DeptRoleId] FOREIGN KEY ([DeptRoleId]) REFERENCES [DepartmentRoles] ([DeptRoleId]),
    CONSTRAINT [FK_UserDepartmentRoles_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([DepartmentId]),
    CONSTRAINT [FK_UserDepartmentRoles_Users_AssignedBy] FOREIGN KEY ([AssignedBy]) REFERENCES [Users] ([UserId]),
    CONSTRAINT [FK_UserDepartmentRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);
GO


CREATE TABLE [Attendances] (
    [AttendId] int NOT NULL IDENTITY,
    [UserId] uniqueidentifier NOT NULL,
    [EventId] int NOT NULL,
    [RegistrationDate] datetime2 NOT NULL,
    [AttendanceStatus] nvarchar(20) NOT NULL,
    [CheckInTime] datetime2 NULL,
    [Score] int NULL,
    [Comment] nvarchar(500) NULL,
    CONSTRAINT [PK_Attendances] PRIMARY KEY ([AttendId]),
    CONSTRAINT [FK_Attendances_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId]),
    CONSTRAINT [FK_Attendances_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
);
GO


CREATE TABLE [EventBudgets] (
    [BudgetId] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [SpendName] nvarchar(max) NOT NULL,
    [BudgetAmount] decimal(15,2) NOT NULL,
    [SpentAmount] decimal(15,2) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_EventBudgets] PRIMARY KEY ([BudgetId]),
    CONSTRAINT [FK_EventBudgets_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId]) ON DELETE CASCADE
);
GO


CREATE TABLE [EventImages] (
    [ImageId] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [ImageUrl] nvarchar(500) NOT NULL,
    [Caption] nvarchar(max) NOT NULL,
    [UploadedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_EventImages] PRIMARY KEY ([ImageId]),
    CONSTRAINT [FK_EventImages_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId])
);
GO


CREATE TABLE [EventSchedules] (
    [ScheduleId] int NOT NULL IDENTITY,
    [EventId] int NOT NULL,
    [ScheduleName] nvarchar(100) NOT NULL,
    [StartTime] datetime2 NULL,
    [EndTime] datetime2 NULL,
    [Description] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_EventSchedules] PRIMARY KEY ([ScheduleId]),
    CONSTRAINT [FK_EventSchedules_Events_EventId] FOREIGN KEY ([EventId]) REFERENCES [Events] ([EventId]) ON DELETE CASCADE
);
GO


CREATE TABLE [ApplicationForms] (
    [FormId] int NOT NULL IDENTITY,
    [CampaignId] int NOT NULL,
    [FormName] nvarchar(200) NOT NULL,
    [FormTitle] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_ApplicationForms] PRIMARY KEY ([FormId]),
    CONSTRAINT [FK_ApplicationForms_RecruitmentCampaigns_CampaignId] FOREIGN KEY ([CampaignId]) REFERENCES [RecruitmentCampaigns] ([CampaignId]) ON DELETE CASCADE
);
GO


CREATE TABLE [ScheduleDetails] (
    [DetailId] int NOT NULL IDENTITY,
    [ScheduleId] int NOT NULL,
    [ActivityName] nvarchar(100) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Duration] int NULL,
    CONSTRAINT [PK_ScheduleDetails] PRIMARY KEY ([DetailId]),
    CONSTRAINT [FK_ScheduleDetails_EventSchedules_ScheduleId] FOREIGN KEY ([ScheduleId]) REFERENCES [EventSchedules] ([ScheduleId]) ON DELETE CASCADE
);
GO


CREATE TABLE [ApplicationQuestions] (
    [QuestionId] int NOT NULL IDENTITY,
    [FormId] int NOT NULL,
    [QuestionText] nvarchar(max) NOT NULL,
    [QuestionType] nvarchar(50) NOT NULL,
    [IsRequired] bit NOT NULL,
    [DisplayOrder] int NULL,
    CONSTRAINT [PK_ApplicationQuestions] PRIMARY KEY ([QuestionId]),
    CONSTRAINT [FK_ApplicationQuestions_ApplicationForms_FormId] FOREIGN KEY ([FormId]) REFERENCES [ApplicationForms] ([FormId]) ON DELETE CASCADE
);
GO


CREATE TABLE [Applications] (
    [ApplicationId] int NOT NULL IDENTITY,
    [FormId] int NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [SubmissionDate] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    [ReviewedAt] datetime2 NULL,
    CONSTRAINT [PK_Applications] PRIMARY KEY ([ApplicationId]),
    CONSTRAINT [FK_Applications_ApplicationForms_FormId] FOREIGN KEY ([FormId]) REFERENCES [ApplicationForms] ([FormId]),
    CONSTRAINT [FK_Applications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
);
GO


CREATE TABLE [ApplicationAnswers] (
    [AnswerId] int NOT NULL IDENTITY,
    [ApplicationId] int NOT NULL,
    [QuestionId] int NOT NULL,
    [AnswerText] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_ApplicationAnswers] PRIMARY KEY ([AnswerId]),
    CONSTRAINT [FK_ApplicationAnswers_ApplicationQuestions_QuestionId] FOREIGN KEY ([QuestionId]) REFERENCES [ApplicationQuestions] ([QuestionId]),
    CONSTRAINT [FK_ApplicationAnswers_Applications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [Applications] ([ApplicationId]) ON DELETE CASCADE
);
GO


CREATE INDEX [IX_ApplicationAnswers_ApplicationId] ON [ApplicationAnswers] ([ApplicationId]);
GO


CREATE INDEX [IX_ApplicationAnswers_QuestionId] ON [ApplicationAnswers] ([QuestionId]);
GO


CREATE INDEX [IX_ApplicationForms_CampaignId] ON [ApplicationForms] ([CampaignId]);
GO


CREATE INDEX [IX_ApplicationQuestions_FormId] ON [ApplicationQuestions] ([FormId]);
GO


CREATE INDEX [IX_Applications_FormId] ON [Applications] ([FormId]);
GO


CREATE INDEX [IX_Applications_Status] ON [Applications] ([Status]);
GO


CREATE INDEX [IX_Applications_UserId] ON [Applications] ([UserId]);
GO


CREATE INDEX [IX_Attendances_EventId] ON [Attendances] ([EventId]);
GO


CREATE INDEX [IX_Attendances_UserId] ON [Attendances] ([UserId]);
GO


CREATE INDEX [IX_ClubFunds_ClubId] ON [ClubFunds] ([ClubId]);
GO


CREATE INDEX [IX_ClubPosts_ClubId] ON [ClubPosts] ([ClubId]);
GO


CREATE INDEX [IX_ClubPosts_PostDate] ON [ClubPosts] ([PostDate]);
GO


CREATE INDEX [IX_ClubPosts_UserId] ON [ClubPosts] ([UserId]);
GO


CREATE INDEX [IX_Clubs_ClubName] ON [Clubs] ([ClubName]);
GO


CREATE INDEX [IX_Departments_ClubId] ON [Departments] ([ClubId]);
GO


CREATE INDEX [IX_EmailVerificationTokens_TokenHash] ON [EmailVerificationTokens] ([TokenHash]);
GO


CREATE INDEX [IX_EmailVerificationTokens_UserId] ON [EmailVerificationTokens] ([UserId]);
GO


CREATE INDEX [IX_EventBudgets_EventId] ON [EventBudgets] ([EventId]);
GO


CREATE INDEX [IX_EventImages_EventId] ON [EventImages] ([EventId]);
GO


CREATE INDEX [IX_Events_ClubId] ON [Events] ([ClubId]);
GO


CREATE INDEX [IX_Events_StartDate] ON [Events] ([StartDate]);
GO


CREATE INDEX [IX_EventSchedules_EventId] ON [EventSchedules] ([EventId]);
GO


CREATE INDEX [IX_FundTransactions_CategoryId] ON [FundTransactions] ([CategoryId]);
GO


CREATE INDEX [IX_FundTransactions_FundId] ON [FundTransactions] ([FundId]);
GO


CREATE INDEX [IX_Notifications_UserId_IsRead] ON [Notifications] ([UserId], [IsRead]);
GO


CREATE INDEX [IX_PasswordResetTokens_TokenHash] ON [PasswordResetTokens] ([TokenHash]);
GO


CREATE INDEX [IX_PasswordResetTokens_UserId] ON [PasswordResetTokens] ([UserId]);
GO


CREATE INDEX [IX_RecruitmentCampaigns_ClubId] ON [RecruitmentCampaigns] ([ClubId]);
GO


CREATE INDEX [IX_RefreshTokens_UserID] ON [RefreshTokens] ([UserID]);
GO


CREATE INDEX [IX_ScheduleDetails_ScheduleId] ON [ScheduleDetails] ([ScheduleId]);
GO


CREATE INDEX [IX_UserClubRoles_AssignedBy] ON [UserClubRoles] ([AssignedBy]);
GO


CREATE INDEX [IX_UserClubRoles_ClubId] ON [UserClubRoles] ([ClubId]);
GO


CREATE INDEX [IX_UserClubRoles_ClubRoleId] ON [UserClubRoles] ([ClubRoleId]);
GO


CREATE INDEX [IX_UserClubRoles_UserId] ON [UserClubRoles] ([UserId]);
GO


CREATE INDEX [IX_UserDepartmentRoles_AssignedBy] ON [UserDepartmentRoles] ([AssignedBy]);
GO


CREATE INDEX [IX_UserDepartmentRoles_DepartmentId] ON [UserDepartmentRoles] ([DepartmentId]);
GO


CREATE INDEX [IX_UserDepartmentRoles_DeptRoleId] ON [UserDepartmentRoles] ([DeptRoleId]);
GO


CREATE INDEX [IX_UserDepartmentRoles_UserId] ON [UserDepartmentRoles] ([UserId]);
GO


CREATE INDEX [IX_UserRoles_UserId] ON [UserRoles] ([UserId]);
GO


CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO


CREATE UNIQUE INDEX [UQ__Users__A9D105345C1FCA58] ON [Users] ([Email]);
GO



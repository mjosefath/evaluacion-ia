-- Bitácora de accesos (inmutable). Sin StatusId / RowVersion / Updated*.
-- EventType: LoginSuccess, LoginFailure, Lockout, Logout, TokenRefresh, PasswordChanged.
-- Solo CREATE; recrear BD si cambia el esquema.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[AccessLogs] (
    [Id]                BIGINT         IDENTITY(1, 1) NOT NULL,
    [CreatedAtUtc]      DATETIME2(3)   NOT NULL CONSTRAINT [DF_AccessLogs_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]         INT            NULL,
    [UserNameAttempted] NVARCHAR(256)  NULL,
    [EventType]         NVARCHAR(50)   NOT NULL,
    [IpAddress]         NVARCHAR(64)   NULL,
    [UserAgent]         NVARCHAR(512)  NULL,
    [Detail]            NVARCHAR(500)  NULL,
    CONSTRAINT [PK_AccessLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AccessLogs_UserProfiles_CreatedBy]
        FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[UserProfiles] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_AccessLogs_CreatedAtUtc]
    ON [dbo].[AccessLogs] ([CreatedAtUtc] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_AccessLogs_CreatedBy]
    ON [dbo].[AccessLogs] ([CreatedBy] ASC)
    WHERE [CreatedBy] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_AccessLogs_EventType]
    ON [dbo].[AccessLogs] ([EventType] ASC);
GO

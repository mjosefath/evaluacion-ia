-- Bitácora de accesos (auth). Sin contraseñas ni tokens en Detail.
-- EventType esperados: LoginSuccess, LoginFailure, Lockout, Logout, TokenRefresh, PasswordChanged.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[AccessLogs] (
    [Id]                 BIGINT         IDENTITY(1, 1) NOT NULL,
    [OccurredAtUtc]      DATETIME2(7)   NOT NULL,
    [IdentityUserId]     NVARCHAR(450)  NULL,
    [UserNameAttempted]  NVARCHAR(256)  NULL,
    [EventType]          NVARCHAR(50)   NOT NULL,
    [IpAddress]          NVARCHAR(64)   NULL,
    [UserAgent]          NVARCHAR(512)  NULL,
    [Detail]             NVARCHAR(500)  NULL,
    CONSTRAINT [PK_AccessLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE NONCLUSTERED INDEX [IX_AccessLogs_OccurredAtUtc]
    ON [dbo].[AccessLogs] ([OccurredAtUtc] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_AccessLogs_IdentityUserId]
    ON [dbo].[AccessLogs] ([IdentityUserId] ASC)
    WHERE [IdentityUserId] IS NOT NULL;
GO

CREATE NONCLUSTERED INDEX [IX_AccessLogs_EventType]
    ON [dbo].[AccessLogs] ([EventType] ASC);
GO

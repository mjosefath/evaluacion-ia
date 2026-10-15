-- Perfil de usuario en BD Sistema. Vínculo lógico a Identity por IdentityUserId (sin FK cross-database).
-- Estándar E1–E9 (análisis 02). Solo CREATE; recrear BD si cambia el esquema.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[UserProfiles] (
    [Id]             INT            IDENTITY(1, 1) NOT NULL,
    [IdentityUserId] NVARCHAR(450)  NOT NULL,
    [DisplayName]    NVARCHAR(200)  NOT NULL,
    [StatusId]       TINYINT        NOT NULL CONSTRAINT [DF_UserProfiles_StatusId] DEFAULT (1),
    [CreatedAtUtc]   DATETIME2(3)   NOT NULL CONSTRAINT [DF_UserProfiles_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]      INT            NULL,
    [UpdatedAtUtc]   DATETIME2(3)   NULL,
    [UpdatedBy]      INT            NULL,
    [RowVersion]     ROWVERSION     NOT NULL,
    CONSTRAINT [PK_UserProfiles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserProfiles_RecordStatuses_StatusId]
        FOREIGN KEY ([StatusId]) REFERENCES [dbo].[RecordStatuses] ([Id]),
    CONSTRAINT [FK_UserProfiles_UserProfiles_CreatedBy]
        FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[UserProfiles] ([Id]),
    CONSTRAINT [FK_UserProfiles_UserProfiles_UpdatedBy]
        FOREIGN KEY ([UpdatedBy]) REFERENCES [dbo].[UserProfiles] ([Id])
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_UserProfiles_IdentityUserId]
    ON [dbo].[UserProfiles] ([IdentityUserId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_UserProfiles_StatusId]
    ON [dbo].[UserProfiles] ([StatusId] ASC);
GO

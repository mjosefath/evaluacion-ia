-- Perfil de usuario en BD Sistema. Vínculo lógico a Identity por IdentityUserId (sin FK cross-database).
-- Ejecutar sobre BD Sistema vacía. Solo CREATE; recrear BD si cambia el esquema.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[UserProfiles] (
    [Id]                          UNIQUEIDENTIFIER NOT NULL,
    [IdentityUserId]              NVARCHAR(450)    NOT NULL,
    [DisplayName]                 NVARCHAR(200)    NOT NULL,
    [IsActive]                    BIT              NOT NULL CONSTRAINT [DF_UserProfiles_IsActive] DEFAULT (1),
    [CreatedAtUtc]                DATETIME2(7)     NOT NULL,
    [CreatedByIdentityUserId]     NVARCHAR(450)    NULL,
    [ModifiedAtUtc]               DATETIME2(7)     NULL,
    [ModifiedByIdentityUserId]    NVARCHAR(450)    NULL,
    CONSTRAINT [PK_UserProfiles] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_UserProfiles_IdentityUserId]
    ON [dbo].[UserProfiles] ([IdentityUserId] ASC);
GO

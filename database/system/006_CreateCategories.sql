-- Catálogo Categories (mutable). Seed: Electrónica, Papelería, Limpieza.
-- CreatedBy = NULL (sistema). Solo CREATE/INSERT; recrear BD si cambia el esquema.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[Categories] (
    [Id]           INT            IDENTITY(1, 1) NOT NULL,
    [Name]         NVARCHAR(50)   NOT NULL,
    [StatusId]     TINYINT        NOT NULL CONSTRAINT [DF_Categories_StatusId] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(3)   NOT NULL CONSTRAINT [DF_Categories_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]    INT            NULL,
    [UpdatedAtUtc] DATETIME2(3)   NULL,
    [UpdatedBy]    INT            NULL,
    [RowVersion]   ROWVERSION     NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UX_Categories_Name] UNIQUE NONCLUSTERED ([Name] ASC),
    CONSTRAINT [FK_Categories_RecordStatuses_StatusId]
        FOREIGN KEY ([StatusId]) REFERENCES [dbo].[RecordStatuses] ([Id]),
    CONSTRAINT [FK_Categories_UserProfiles_CreatedBy]
        FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[UserProfiles] ([Id]),
    CONSTRAINT [FK_Categories_UserProfiles_UpdatedBy]
        FOREIGN KEY ([UpdatedBy]) REFERENCES [dbo].[UserProfiles] ([Id])
);
GO

INSERT INTO [dbo].[Categories] ([Name], [StatusId], [CreatedBy])
VALUES
    (N'Electrónica', 1, NULL),
    (N'Papelería', 1, NULL),
    (N'Limpieza', 1, NULL);
GO

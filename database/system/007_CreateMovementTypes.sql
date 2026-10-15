-- Catálogo MovementTypes (mutable). Seed: ENTRADA, SALIDA, AJUSTE.
-- CreatedBy = NULL (sistema). Solo CREATE/INSERT; recrear BD si cambia el esquema.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[MovementTypes] (
    [Id]           INT            IDENTITY(1, 1) NOT NULL,
    [Code]         VARCHAR(10)    NOT NULL,
    [Name]         NVARCHAR(30)   NOT NULL,
    [StatusId]     TINYINT        NOT NULL CONSTRAINT [DF_MovementTypes_StatusId] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(3)   NOT NULL CONSTRAINT [DF_MovementTypes_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]    INT            NULL,
    [UpdatedAtUtc] DATETIME2(3)   NULL,
    [UpdatedBy]    INT            NULL,
    [RowVersion]   ROWVERSION     NOT NULL,
    CONSTRAINT [PK_MovementTypes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UX_MovementTypes_Code] UNIQUE NONCLUSTERED ([Code] ASC),
    CONSTRAINT [FK_MovementTypes_RecordStatuses_StatusId]
        FOREIGN KEY ([StatusId]) REFERENCES [dbo].[RecordStatuses] ([Id]),
    CONSTRAINT [FK_MovementTypes_UserProfiles_CreatedBy]
        FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[UserProfiles] ([Id]),
    CONSTRAINT [FK_MovementTypes_UserProfiles_UpdatedBy]
        FOREIGN KEY ([UpdatedBy]) REFERENCES [dbo].[UserProfiles] ([Id])
);
GO

INSERT INTO [dbo].[MovementTypes] ([Code], [Name], [StatusId], [CreatedBy])
VALUES
    ('ENTRADA', N'Entrada', 1, NULL),
    ('SALIDA', N'Salida', 1, NULL),
    ('AJUSTE', N'Ajuste', 1, NULL);
GO

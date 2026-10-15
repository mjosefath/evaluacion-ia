-- Catálogo de permisos y asignación rol → permiso (RoleName = rol Identity, sin FK cross-database).
-- Estándar mutable; FK NO ACTION (E2). Solo CREATE; recrear BD si cambia el esquema.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[Permissions] (
    [Id]           INT            IDENTITY(1, 1) NOT NULL,
    [Code]         NVARCHAR(100)  NOT NULL,
    [Name]         NVARCHAR(200)  NOT NULL,
    [Module]       NVARCHAR(100)  NOT NULL,
    [StatusId]     TINYINT        NOT NULL CONSTRAINT [DF_Permissions_StatusId] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(3)   NOT NULL CONSTRAINT [DF_Permissions_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]    INT            NULL,
    [UpdatedAtUtc] DATETIME2(3)   NULL,
    [UpdatedBy]    INT            NULL,
    [RowVersion]   ROWVERSION     NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Permissions_RecordStatuses_StatusId]
        FOREIGN KEY ([StatusId]) REFERENCES [dbo].[RecordStatuses] ([Id]),
    CONSTRAINT [FK_Permissions_UserProfiles_CreatedBy]
        FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[UserProfiles] ([Id]),
    CONSTRAINT [FK_Permissions_UserProfiles_UpdatedBy]
        FOREIGN KEY ([UpdatedBy]) REFERENCES [dbo].[UserProfiles] ([Id])
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_Permissions_Code]
    ON [dbo].[Permissions] ([Code] ASC);
GO

CREATE TABLE [dbo].[RolePermissions] (
    [Id]           INT            IDENTITY(1, 1) NOT NULL,
    [RoleName]     NVARCHAR(256)  NOT NULL,
    [PermissionId] INT            NOT NULL,
    [StatusId]     TINYINT        NOT NULL CONSTRAINT [DF_RolePermissions_StatusId] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(3)   NOT NULL CONSTRAINT [DF_RolePermissions_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]    INT            NULL,
    [UpdatedAtUtc] DATETIME2(3)   NULL,
    [UpdatedBy]    INT            NULL,
    [RowVersion]   ROWVERSION     NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UX_RolePermissions_RoleName_PermissionId]
        UNIQUE NONCLUSTERED ([RoleName] ASC, [PermissionId] ASC),
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId]
        FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions] ([Id]),
    CONSTRAINT [FK_RolePermissions_RecordStatuses_StatusId]
        FOREIGN KEY ([StatusId]) REFERENCES [dbo].[RecordStatuses] ([Id]),
    CONSTRAINT [FK_RolePermissions_UserProfiles_CreatedBy]
        FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[UserProfiles] ([Id]),
    CONSTRAINT [FK_RolePermissions_UserProfiles_UpdatedBy]
        FOREIGN KEY ([UpdatedBy]) REFERENCES [dbo].[UserProfiles] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_RolePermissions_PermissionId]
    ON [dbo].[RolePermissions] ([PermissionId] ASC);
GO

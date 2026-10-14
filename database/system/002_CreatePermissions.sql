-- Catálogo de permisos y asignación rol → permiso.
-- RoleName es el nombre del rol Identity (sin FK cross-database).

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[Permissions] (
    [Id]     INT            IDENTITY(1, 1) NOT NULL,
    [Code]   NVARCHAR(100)  NOT NULL,
    [Name]   NVARCHAR(200)  NOT NULL,
    [Module] NVARCHAR(100)  NOT NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX [UX_Permissions_Code]
    ON [dbo].[Permissions] ([Code] ASC);
GO

CREATE TABLE [dbo].[RolePermissions] (
    [RoleName]     NVARCHAR(256) NOT NULL,
    [PermissionId] INT           NOT NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY CLUSTERED ([RoleName] ASC, [PermissionId] ASC),
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId]
        FOREIGN KEY ([PermissionId]) REFERENCES [dbo].[Permissions] ([Id]) ON DELETE CASCADE
);
GO

CREATE NONCLUSTERED INDEX [IX_RolePermissions_PermissionId]
    ON [dbo].[RolePermissions] ([PermissionId] ASC);
GO

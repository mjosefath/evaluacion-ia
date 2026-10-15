-- =============================================================================
-- Inventario (Sistema) — 001_Schema: tablas + vista (virgen).
-- RecordStatuses incluye seed Activo/Inactivo (requerido por FK StatusId).
-- Categorías/tipos: solo CREATE aquí; datos en 002_Seed.
-- Ejecutar sobre BD Sistema vacía.
-- =============================================================================

/* --- 001_CreateRecordStatuses.sql --- */

-- Catálogo de estatus (E1): 1 = Activo, 2 = Inactivo.
-- Sin StatusId propio (catálogo de sistema). Solo CREATE; recrear BD si cambia el esquema.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[RecordStatuses] (
    [Id]   TINYINT       NOT NULL,
    [Code] VARCHAR(10)   NOT NULL,
    [Name] NVARCHAR(20)  NOT NULL,
    CONSTRAINT [PK_RecordStatuses] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UX_RecordStatuses_Code] UNIQUE NONCLUSTERED ([Code] ASC),
    CONSTRAINT [UX_RecordStatuses_Name] UNIQUE NONCLUSTERED ([Name] ASC)
);
GO

INSERT INTO [dbo].[RecordStatuses] ([Id], [Code], [Name])
VALUES
    (1, 'ACTIVO', N'Activo'),
    (2, 'INACTIVO', N'Inactivo');
GO


/* --- 002_CreateUserProfiles.sql --- */

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


/* --- 003_CreatePermissions.sql --- */

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


/* --- 004_CreateAccessLogs.sql --- */

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


/* --- 006_CreateCategories.sql --- */

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


/* --- 007_CreateMovementTypes.sql --- */

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


/* --- 008_CreateProducts.sql --- */

-- Products (mutable). Sin columna de existencia (sale de InventoryMovements / vw_ProductStock).
-- Solo CREATE; recrear BD si cambia el esquema.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[Products] (
    [Id]           INT             IDENTITY(1, 1) NOT NULL,
    [Code]         NVARCHAR(20)    NOT NULL,
    [Name]         NVARCHAR(150)   NOT NULL,
    [CategoryId]   INT             NOT NULL,
    [UnitPrice]    DECIMAL(18, 2)  NOT NULL,
    [MinStock]     INT             NOT NULL,
    [StatusId]     TINYINT         NOT NULL CONSTRAINT [DF_Products_StatusId] DEFAULT (1),
    [CreatedAtUtc] DATETIME2(3)    NOT NULL CONSTRAINT [DF_Products_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]    INT             NULL,
    [UpdatedAtUtc] DATETIME2(3)    NULL,
    [UpdatedBy]    INT             NULL,
    [RowVersion]   ROWVERSION      NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UX_Products_Code] UNIQUE NONCLUSTERED ([Code] ASC),
    CONSTRAINT [CK_Products_UnitPrice] CHECK ([UnitPrice] >= 0),
    CONSTRAINT [CK_Products_MinStock] CHECK ([MinStock] >= 0),
    CONSTRAINT [FK_Products_Categories_CategoryId]
        FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[Categories] ([Id]),
    CONSTRAINT [FK_Products_RecordStatuses_StatusId]
        FOREIGN KEY ([StatusId]) REFERENCES [dbo].[RecordStatuses] ([Id]),
    CONSTRAINT [FK_Products_UserProfiles_CreatedBy]
        FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[UserProfiles] ([Id]),
    CONSTRAINT [FK_Products_UserProfiles_UpdatedBy]
        FOREIGN KEY ([UpdatedBy]) REFERENCES [dbo].[UserProfiles] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_Products_CategoryId]
    ON [dbo].[Products] ([CategoryId] ASC);
GO

CREATE NONCLUSTERED INDEX [IX_Products_StatusId]
    ON [dbo].[Products] ([StatusId] ASC);
GO


/* --- 009_CreateInventoryMovements.sql --- */

-- InventoryMovements (inmutable, bigint). Existencia = último StockAfter por producto.
-- Solo CREATE; recrear BD si cambia el esquema.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE [dbo].[InventoryMovements] (
    [Id]             BIGINT        IDENTITY(1, 1) NOT NULL,
    [ProductId]      INT           NOT NULL,
    [MovementTypeId] INT           NOT NULL,
    [Quantity]       INT           NOT NULL,
    [StockBefore]    INT           NOT NULL,
    [StockAfter]     INT           NOT NULL,
    [Reason]         NVARCHAR(250) NULL,
    [CreatedAtUtc]   DATETIME2(3)  NOT NULL CONSTRAINT [DF_InventoryMovements_CreatedAtUtc] DEFAULT (SYSUTCDATETIME()),
    [CreatedBy]      INT           NULL,
    CONSTRAINT [PK_InventoryMovements] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [CK_InventoryMovements_Quantity] CHECK ([Quantity] <> 0),
    CONSTRAINT [CK_InventoryMovements_StockBefore] CHECK ([StockBefore] >= 0),
    CONSTRAINT [CK_InventoryMovements_StockAfter] CHECK ([StockAfter] >= 0),
    CONSTRAINT [CK_InventoryMovements_StockBalance] CHECK ([StockAfter] = [StockBefore] + [Quantity]),
    CONSTRAINT [FK_InventoryMovements_Products_ProductId]
        FOREIGN KEY ([ProductId]) REFERENCES [dbo].[Products] ([Id]),
    CONSTRAINT [FK_InventoryMovements_MovementTypes_MovementTypeId]
        FOREIGN KEY ([MovementTypeId]) REFERENCES [dbo].[MovementTypes] ([Id]),
    CONSTRAINT [FK_InventoryMovements_UserProfiles_CreatedBy]
        FOREIGN KEY ([CreatedBy]) REFERENCES [dbo].[UserProfiles] ([Id])
);
GO

CREATE NONCLUSTERED INDEX [IX_InventoryMovements_ProductId_Id]
    ON [dbo].[InventoryMovements] ([ProductId] ASC, [Id] DESC)
    INCLUDE ([StockAfter]);
GO

CREATE NONCLUSTERED INDEX [IX_InventoryMovements_CreatedAtUtc]
    ON [dbo].[InventoryMovements] ([CreatedAtUtc] DESC);
GO


/* --- 010_CreateVwProductStock.sql --- */

-- Existencia actual = último StockAfter del producto; 0 si no hay movimientos (análisis 02 / SP3).

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE VIEW [dbo].[vw_ProductStock]
AS
SELECT
    p.[Id] AS [ProductId],
    ISNULL(lastMov.[StockAfter], 0) AS [Stock]
FROM [dbo].[Products] AS p
OUTER APPLY (
    SELECT TOP (1) m.[StockAfter]
    FROM [dbo].[InventoryMovements] AS m
    WHERE m.[ProductId] = p.[Id]
    ORDER BY m.[Id] DESC
) AS lastMov;
GO


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

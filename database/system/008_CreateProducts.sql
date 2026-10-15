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

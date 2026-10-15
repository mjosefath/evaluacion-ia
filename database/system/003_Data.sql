-- =============================================================================
-- Inventario (Sistema) — 003_Data: productos y movimientos de carga inicial.
-- Requiere 001_Schema + 002_Seed.
-- =============================================================================

-- Seed productos + ENTRADA inicial (análisis 02, pasos 2–4).
-- PDF de evaluación no disponible en el repo: valores elegidos para demo (documentados abajo).
-- CreatedBy = primer UserProfiles.Id si existe; si no, NULL (DevelopmentAdminSeeder puede completarlo).
--
-- Productos (Code, Name, Category, UnitPrice, MinStock) → Qty ENTRADA:
--   ELE-001  Mouse USB              Electrónica  150.00  5  → 20
--   ELE-002  Teclado USB            Electrónica  250.00  5  → 12
--   PAP-001  Resma papel A4         Papelería     80.00 10  → 30
--   PAP-002  Bolígrafo azul (caja)  Papelería     45.00 20  →  8
--   LIM-001  Detergente 1 L         Limpieza      35.00  5  → 15
-- Reason = N'Carga inicial'. Idempotente por Code.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @CreatedBy INT =
(
    SELECT TOP (1) [Id]
    FROM [dbo].[UserProfiles]
    ORDER BY [Id] ASC
);

DECLARE @CatEle INT = (SELECT [Id] FROM [dbo].[Categories] WHERE [Name] = N'Electrónica');
DECLARE @CatPap INT = (SELECT [Id] FROM [dbo].[Categories] WHERE [Name] = N'Papelería');
DECLARE @CatLim INT = (SELECT [Id] FROM [dbo].[Categories] WHERE [Name] = N'Limpieza');
DECLARE @TipoEntrada INT = (SELECT [Id] FROM [dbo].[MovementTypes] WHERE [Code] = 'ENTRADA');
DECLARE @StatusActivo TINYINT = 1;

IF @CatEle IS NULL OR @CatPap IS NULL OR @CatLim IS NULL OR @TipoEntrada IS NULL
BEGIN
    THROW 50001, 'Seed productos: faltan Categories o MovementTypes (ejecutar 006/007 antes).', 1;
END;

DECLARE @Products TABLE (
    [Code] NVARCHAR(20) NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [CategoryId] INT NOT NULL,
    [UnitPrice] DECIMAL(18, 2) NOT NULL,
    [MinStock] INT NOT NULL,
    [Qty] INT NOT NULL
);

INSERT INTO @Products ([Code], [Name], [CategoryId], [UnitPrice], [MinStock], [Qty])
VALUES
    (N'ELE-001', N'Mouse USB', @CatEle, 150.00, 5, 20),
    (N'ELE-002', N'Teclado USB', @CatEle, 250.00, 5, 12),
    (N'PAP-001', N'Resma papel A4', @CatPap, 80.00, 10, 30),
    (N'PAP-002', N'Bolígrafo azul (caja)', @CatPap, 45.00, 20, 8),
    (N'LIM-001', N'Detergente 1 L', @CatLim, 35.00, 5, 15);

DECLARE @Code NVARCHAR(20);
DECLARE @Name NVARCHAR(150);
DECLARE @CategoryId INT;
DECLARE @UnitPrice DECIMAL(18, 2);
DECLARE @MinStock INT;
DECLARE @Qty INT;
DECLARE @ProductId INT;

DECLARE product_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT [Code], [Name], [CategoryId], [UnitPrice], [MinStock], [Qty]
    FROM @Products;

OPEN product_cursor;
FETCH NEXT FROM product_cursor INTO @Code, @Name, @CategoryId, @UnitPrice, @MinStock, @Qty;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Code] = @Code)
    BEGIN
        INSERT INTO [dbo].[Products] (
            [Code], [Name], [CategoryId], [UnitPrice], [MinStock],
            [StatusId], [CreatedBy]
        )
        VALUES (
            @Code, @Name, @CategoryId, @UnitPrice, @MinStock,
            @StatusActivo, @CreatedBy
        );
    END;

    SELECT @ProductId = [Id]
    FROM [dbo].[Products]
    WHERE [Code] = @Code;

    IF @ProductId IS NOT NULL
       AND NOT EXISTS (
           SELECT 1
           FROM [dbo].[InventoryMovements]
           WHERE [ProductId] = @ProductId
             AND [Reason] = N'Carga inicial'
       )
    BEGIN
        INSERT INTO [dbo].[InventoryMovements] (
            [ProductId], [MovementTypeId], [Quantity],
            [StockBefore], [StockAfter], [Reason], [CreatedBy]
        )
        VALUES (
            @ProductId, @TipoEntrada, @Qty,
            0, @Qty, N'Carga inicial', @CreatedBy
        );
    END;

    FETCH NEXT FROM product_cursor INTO @Code, @Name, @CategoryId, @UnitPrice, @MinStock, @Qty;
END;

CLOSE product_cursor;
DEALLOCATE product_cursor;
GO


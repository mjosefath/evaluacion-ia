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

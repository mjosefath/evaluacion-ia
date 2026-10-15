-- Reporte de existencias. Valor = ROUND(UnitPrice * Stock, 2).
-- @GrandTotal = suma de Valor sobre todas las filas filtradas (PG5/SP4).
-- Orden default: Code ASC. Ordenables: code, product/name, category, stock, value/valor.
-- Salida: ProductId, Code, Name, CategoryId, CategoryName, UnitPrice, Stock, MinStock,
--         StockStatus, InventoryValue.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE [dbo].[usp_Report_Stock]
    @Search         NVARCHAR(200) = NULL,
    @Page           INT = 1,
    @PageSize       INT = 20,
    @SortColumn     NVARCHAR(50) = NULL,
    @SortDirection  NVARCHAR(4) = NULL,
    @CategoryId     INT = NULL,
    @TotalRows      INT OUTPUT,
    @GrandTotal     DECIMAL(18, 2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Page IS NULL OR @Page < 1 SET @Page = 1;
    IF @PageSize IS NULL OR @PageSize < 1 SET @PageSize = 20;
    IF @PageSize > 100 SET @PageSize = 100;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    DECLARE @Sort NVARCHAR(50) = LOWER(LTRIM(RTRIM(ISNULL(@SortColumn, N''))));
    DECLARE @Dir NVARCHAR(4) =
        CASE WHEN UPPER(LTRIM(RTRIM(ISNULL(@SortDirection, N'')))) = N'DESC' THEN N'DESC' ELSE N'ASC' END;
    DECLARE @Active TINYINT = 1;

    ;WITH Base AS (
        SELECT
            p.[Id] AS [ProductId],
            p.[Code],
            p.[Name],
            p.[CategoryId],
            c.[Name] AS [CategoryName],
            p.[UnitPrice],
            stock.[Stock],
            p.[MinStock],
            CASE WHEN stock.[Stock] <= p.[MinStock] THEN N'BAJO' ELSE N'NORMAL' END AS [StockStatus],
            CAST(ROUND(p.[UnitPrice] * stock.[Stock], 2) AS DECIMAL(18, 2)) AS [InventoryValue]
        FROM [dbo].[Products] AS p
        INNER JOIN [dbo].[Categories] AS c ON c.[Id] = p.[CategoryId]
        INNER JOIN [dbo].[vw_ProductStock] AS stock ON stock.[ProductId] = p.[Id]
        WHERE p.[StatusId] = @Active
          AND (@CategoryId IS NULL OR p.[CategoryId] = @CategoryId)
    ),
    Filtered AS (
        SELECT b.*
        FROM Base AS b
        WHERE (
              NULLIF(LTRIM(RTRIM(@Search)), N'') IS NULL
              OR NOT EXISTS (
                  SELECT 1
                  FROM STRING_SPLIT(@Search, N' ') AS s
                  CROSS APPLY (SELECT LTRIM(RTRIM(s.[value])) AS [Word]) AS w
                  WHERE w.[Word] <> N''
                    AND NOT (
                        (b.[Code] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (b.[Name] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (b.[CategoryName] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (b.[StockStatus] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (TRY_CONVERT(DECIMAL(18, 2), w.[Word]) IS NOT NULL AND b.[UnitPrice] = TRY_CONVERT(DECIMAL(18, 2), w.[Word]))
                        OR (TRY_CONVERT(INT, w.[Word]) IS NOT NULL AND b.[Stock] = TRY_CONVERT(INT, w.[Word]))
                        OR (TRY_CONVERT(INT, w.[Word]) IS NOT NULL AND b.[MinStock] = TRY_CONVERT(INT, w.[Word]))
                    )
              )
          )
    )
    SELECT
        @TotalRows = COUNT(1),
        @GrandTotal = ISNULL(SUM(f.[InventoryValue]), CAST(0 AS DECIMAL(18, 2)))
    FROM Filtered AS f;

    SELECT
        f.[ProductId],
        f.[Code],
        f.[Name],
        f.[CategoryId],
        f.[CategoryName],
        f.[UnitPrice],
        f.[Stock],
        f.[MinStock],
        f.[StockStatus],
        f.[InventoryValue]
    FROM Filtered AS f
    ORDER BY
        CASE WHEN @Dir = N'ASC'  AND (@Sort = N'' OR @Sort = N'code' OR @Sort = N'codigo') THEN f.[Code] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort = N'' OR @Sort = N'code' OR @Sort = N'codigo') THEN f.[Code] END DESC,
        CASE WHEN @Dir = N'ASC'  AND (@Sort IN (N'product', N'producto', N'name', N'nombre')) THEN f.[Name] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort IN (N'product', N'producto', N'name', N'nombre')) THEN f.[Name] END DESC,
        CASE WHEN @Dir = N'ASC'  AND (@Sort IN (N'category', N'categoria')) THEN f.[CategoryName] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort IN (N'category', N'categoria')) THEN f.[CategoryName] END DESC,
        CASE WHEN @Dir = N'ASC'  AND (@Sort IN (N'stock', N'existencia')) THEN f.[Stock] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort IN (N'stock', N'existencia')) THEN f.[Stock] END DESC,
        CASE WHEN @Dir = N'ASC'  AND (@Sort IN (N'value', N'valor', N'inventoryvalue')) THEN f.[InventoryValue] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort IN (N'value', N'valor', N'inventoryvalue')) THEN f.[InventoryValue] END DESC,
        f.[Code] ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

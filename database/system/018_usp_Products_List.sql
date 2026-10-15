-- Listado Productos. Existencia = vw_ProductStock; StockStatus = BAJO|NORMAL.
-- Orden default: Code ASC. Ordenables: code, name, category, price/unitprice, stock/existencia.
-- Salida: Id, Code, Name, CategoryId, CategoryName, UnitPrice, MinStock, Stock, StockStatus,
--         StatusId, StatusName, RowVersion.
-- @StatusId NULL = todos (la UI/API suele pasar 1 = Activo).

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE [dbo].[usp_Products_List]
    @Search         NVARCHAR(200) = NULL,
    @Page           INT = 1,
    @PageSize       INT = 20,
    @SortColumn     NVARCHAR(50) = NULL,
    @SortDirection  NVARCHAR(4) = NULL,
    @CategoryId     INT = NULL,
    @StatusId       TINYINT = NULL,
    @TotalRows      INT OUTPUT
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

    ;WITH Base AS (
        SELECT
            p.[Id],
            p.[Code],
            p.[Name],
            p.[CategoryId],
            c.[Name] AS [CategoryName],
            p.[UnitPrice],
            p.[MinStock],
            stock.[Stock],
            CASE WHEN stock.[Stock] <= p.[MinStock] THEN N'BAJO' ELSE N'NORMAL' END AS [StockStatus],
            p.[StatusId],
            rs.[Name] AS [StatusName],
            p.[RowVersion]
        FROM [dbo].[Products] AS p
        INNER JOIN [dbo].[Categories] AS c ON c.[Id] = p.[CategoryId]
        INNER JOIN [dbo].[RecordStatuses] AS rs ON rs.[Id] = p.[StatusId]
        INNER JOIN [dbo].[vw_ProductStock] AS stock ON stock.[ProductId] = p.[Id]
        WHERE (@CategoryId IS NULL OR p.[CategoryId] = @CategoryId)
          AND (@StatusId IS NULL OR p.[StatusId] = @StatusId)
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
                        OR (b.[StatusName] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (b.[StockStatus] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (TRY_CONVERT(DECIMAL(18, 2), w.[Word]) IS NOT NULL AND b.[UnitPrice] = TRY_CONVERT(DECIMAL(18, 2), w.[Word]))
                        OR (TRY_CONVERT(INT, w.[Word]) IS NOT NULL AND b.[MinStock] = TRY_CONVERT(INT, w.[Word]))
                        OR (TRY_CONVERT(INT, w.[Word]) IS NOT NULL AND b.[Stock] = TRY_CONVERT(INT, w.[Word]))
                    )
              )
          )
    )
    SELECT @TotalRows = COUNT(1) FROM Filtered;

    SELECT
        f.[Id],
        f.[Code],
        f.[Name],
        f.[CategoryId],
        f.[CategoryName],
        f.[UnitPrice],
        f.[MinStock],
        f.[Stock],
        f.[StockStatus],
        f.[StatusId],
        f.[StatusName],
        f.[RowVersion]
    FROM Filtered AS f
    ORDER BY
        CASE WHEN @Dir = N'ASC'  AND (@Sort = N'' OR @Sort = N'code' OR @Sort = N'codigo') THEN f.[Code] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort = N'' OR @Sort = N'code' OR @Sort = N'codigo') THEN f.[Code] END DESC,
        CASE WHEN @Dir = N'ASC'  AND (@Sort = N'name' OR @Sort = N'nombre') THEN f.[Name] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort = N'name' OR @Sort = N'nombre') THEN f.[Name] END DESC,
        CASE WHEN @Dir = N'ASC'  AND (@Sort = N'category' OR @Sort = N'categoria') THEN f.[CategoryName] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort = N'category' OR @Sort = N'categoria') THEN f.[CategoryName] END DESC,
        CASE WHEN @Dir = N'ASC'  AND (@Sort = N'price' OR @Sort = N'unitprice' OR @Sort = N'precio') THEN f.[UnitPrice] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort = N'price' OR @Sort = N'unitprice' OR @Sort = N'precio') THEN f.[UnitPrice] END DESC,
        CASE WHEN @Dir = N'ASC'  AND (@Sort = N'stock' OR @Sort = N'existencia') THEN f.[Stock] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort = N'stock' OR @Sort = N'existencia') THEN f.[Stock] END DESC,
        f.[Code] ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- =============================================================================
-- Inventario (Sistema) — 004_Procedures: función LIKE + usp_* de lectura.
-- Requiere 001_Schema (y datos opcionales). Solo CREATE.
-- =============================================================================

/* --- 013_CreateFnEscapeLikePattern.sql --- */

-- Escapa comodines LIKE del usuario (B6): %, _, [

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE FUNCTION [dbo].[fn_EscapeLikePattern] (@Text NVARCHAR(4000))
RETURNS NVARCHAR(4000)
WITH SCHEMABINDING
AS
BEGIN
    IF @Text IS NULL
        RETURN N'';

    SET @Text = REPLACE(@Text, N'[', N'[[]');
    SET @Text = REPLACE(@Text, N'%', N'[%]');
    SET @Text = REPLACE(@Text, N'_', N'[_]');
    RETURN @Text;
END;
GO


/* --- 014_usp_Categories_List.sql --- */

-- Selector de categoría (PG8/PG9). Salida: Id, Name, StatusId, StatusName.
-- @StatusId default 1 (activos); NULL = todos.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE [dbo].[usp_Categories_List]
    @Search         NVARCHAR(200) = NULL,
    @Page           INT = 1,
    @PageSize       INT = 20,
    @SortColumn     NVARCHAR(50) = NULL,
    @SortDirection  NVARCHAR(4) = NULL,
    @StatusId       TINYINT = 1,
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

    ;WITH Filtered AS (
        SELECT
            c.[Id],
            c.[Name],
            c.[StatusId],
            rs.[Name] AS [StatusName]
        FROM [dbo].[Categories] AS c
        INNER JOIN [dbo].[RecordStatuses] AS rs ON rs.[Id] = c.[StatusId]
        WHERE (@StatusId IS NULL OR c.[StatusId] = @StatusId)
          AND (
              NULLIF(LTRIM(RTRIM(@Search)), N'') IS NULL
              OR NOT EXISTS (
                  SELECT 1
                  FROM STRING_SPLIT(@Search, N' ') AS s
                  CROSS APPLY (SELECT LTRIM(RTRIM(s.[value])) AS [Word]) AS w
                  WHERE w.[Word] <> N''
                    AND NOT (
                        (c.[Name] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (rs.[Name] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                    )
              )
          )
    )
    SELECT @TotalRows = COUNT(1) FROM Filtered;

    SELECT
        f.[Id],
        f.[Name],
        f.[StatusId],
        f.[StatusName]
    FROM Filtered AS f
    ORDER BY
        CASE WHEN @Dir = N'ASC'  AND (@Sort = N'' OR @Sort = N'name') THEN f.[Name] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort = N'' OR @Sort = N'name') THEN f.[Name] END DESC,
        CASE WHEN @Dir = N'ASC'  AND @Sort = N'id' THEN f.[Id] END ASC,
        CASE WHEN @Dir = N'DESC' AND @Sort = N'id' THEN f.[Id] END DESC,
        f.[Name] ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO


/* --- 015_usp_MovementTypes_List.sql --- */

-- Selector de tipo de movimiento. Salida: Id, Code, Name, StatusId, StatusName.
-- @StatusId default 1 (activos); NULL = todos.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE [dbo].[usp_MovementTypes_List]
    @Search         NVARCHAR(200) = NULL,
    @Page           INT = 1,
    @PageSize       INT = 20,
    @SortColumn     NVARCHAR(50) = NULL,
    @SortDirection  NVARCHAR(4) = NULL,
    @StatusId       TINYINT = 1,
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

    ;WITH Filtered AS (
        SELECT
            mt.[Id],
            mt.[Code],
            mt.[Name],
            mt.[StatusId],
            rs.[Name] AS [StatusName]
        FROM [dbo].[MovementTypes] AS mt
        INNER JOIN [dbo].[RecordStatuses] AS rs ON rs.[Id] = mt.[StatusId]
        WHERE (@StatusId IS NULL OR mt.[StatusId] = @StatusId)
          AND (
              NULLIF(LTRIM(RTRIM(@Search)), N'') IS NULL
              OR NOT EXISTS (
                  SELECT 1
                  FROM STRING_SPLIT(@Search, N' ') AS s
                  CROSS APPLY (SELECT LTRIM(RTRIM(s.[value])) AS [Word]) AS w
                  WHERE w.[Word] <> N''
                    AND NOT (
                        (mt.[Code] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (mt.[Name] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (rs.[Name] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                    )
              )
          )
    )
    SELECT @TotalRows = COUNT(1) FROM Filtered;

    SELECT
        f.[Id],
        f.[Code],
        f.[Name],
        f.[StatusId],
        f.[StatusName]
    FROM Filtered AS f
    ORDER BY
        CASE WHEN @Dir = N'ASC'  AND (@Sort = N'' OR @Sort = N'name') THEN f.[Name] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort = N'' OR @Sort = N'name') THEN f.[Name] END DESC,
        CASE WHEN @Dir = N'ASC'  AND @Sort = N'code' THEN f.[Code] END ASC,
        CASE WHEN @Dir = N'DESC' AND @Sort = N'code' THEN f.[Code] END DESC,
        f.[Name] ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO


/* --- 016_usp_RecordStatuses_List.sql --- */

-- Selector de estatus (Productos). Salida: Id, Code, Name.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE [dbo].[usp_RecordStatuses_List]
    @Search         NVARCHAR(200) = NULL,
    @Page           INT = 1,
    @PageSize       INT = 20,
    @SortColumn     NVARCHAR(50) = NULL,
    @SortDirection  NVARCHAR(4) = NULL,
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

    ;WITH Filtered AS (
        SELECT
            rs.[Id],
            rs.[Code],
            rs.[Name]
        FROM [dbo].[RecordStatuses] AS rs
        WHERE (
              NULLIF(LTRIM(RTRIM(@Search)), N'') IS NULL
              OR NOT EXISTS (
                  SELECT 1
                  FROM STRING_SPLIT(@Search, N' ') AS s
                  CROSS APPLY (SELECT LTRIM(RTRIM(s.[value])) AS [Word]) AS w
                  WHERE w.[Word] <> N''
                    AND NOT (
                        (rs.[Code] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (rs.[Name] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                    )
              )
          )
    )
    SELECT @TotalRows = COUNT(1) FROM Filtered;

    SELECT
        f.[Id],
        f.[Code],
        f.[Name]
    FROM Filtered AS f
    ORDER BY
        CASE WHEN @Dir = N'ASC'  AND (@Sort = N'' OR @Sort = N'id' OR @Sort = N'name') THEN f.[Id] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort = N'' OR @Sort = N'id' OR @Sort = N'name') THEN f.[Id] END DESC,
        CASE WHEN @Dir = N'ASC'  AND @Sort = N'code' THEN f.[Code] END ASC,
        CASE WHEN @Dir = N'DESC' AND @Sort = N'code' THEN f.[Code] END DESC,
        f.[Id] ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO


/* --- 017_usp_Products_Lookup.sql --- */

-- Buscador de producto (Movimientos): solo activos + existencia (vw_ProductStock).
-- Salida: Id, Code, Name, Stock.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE [dbo].[usp_Products_Lookup]
    @Search         NVARCHAR(200) = NULL,
    @Page           INT = 1,
    @PageSize       INT = 20,
    @SortColumn     NVARCHAR(50) = NULL,
    @SortDirection  NVARCHAR(4) = NULL,
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
    DECLARE @Active TINYINT = 1;

    ;WITH Filtered AS (
        SELECT
            p.[Id],
            p.[Code],
            p.[Name],
            stock.[Stock]
        FROM [dbo].[Products] AS p
        INNER JOIN [dbo].[vw_ProductStock] AS stock ON stock.[ProductId] = p.[Id]
        WHERE p.[StatusId] = @Active
          AND (
              NULLIF(LTRIM(RTRIM(@Search)), N'') IS NULL
              OR NOT EXISTS (
                  SELECT 1
                  FROM STRING_SPLIT(@Search, N' ') AS s
                  CROSS APPLY (SELECT LTRIM(RTRIM(s.[value])) AS [Word]) AS w
                  WHERE w.[Word] <> N''
                    AND NOT (
                        (p.[Code] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (p.[Name] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (TRY_CONVERT(INT, w.[Word]) IS NOT NULL AND stock.[Stock] = TRY_CONVERT(INT, w.[Word]))
                    )
              )
          )
    )
    SELECT @TotalRows = COUNT(1) FROM Filtered;

    SELECT
        f.[Id],
        f.[Code],
        f.[Name],
        f.[Stock]
    FROM Filtered AS f
    ORDER BY
        CASE WHEN @Dir = N'ASC'  AND (@Sort = N'' OR @Sort = N'code') THEN f.[Code] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort = N'' OR @Sort = N'code') THEN f.[Code] END DESC,
        CASE WHEN @Dir = N'ASC'  AND @Sort = N'name' THEN f.[Name] END ASC,
        CASE WHEN @Dir = N'DESC' AND @Sort = N'name' THEN f.[Name] END DESC,
        CASE WHEN @Dir = N'ASC'  AND @Sort IN (N'stock', N'existencia') THEN f.[Stock] END ASC,
        CASE WHEN @Dir = N'DESC' AND @Sort IN (N'stock', N'existencia') THEN f.[Stock] END DESC,
        f.[Code] ASC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO


/* --- 018_usp_Products_List.sql --- */

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


/* --- 019_usp_InventoryMovements_List.sql --- */

-- Historial de movimientos. Orden default: CreatedAtUtc DESC.
-- Ordenables: date/fecha/createdatutc, product/producto, type/tipo.
-- Salida: Id, CreatedAtUtc, ProductId, ProductCode, ProductName, MovementTypeId, MovementTypeCode,
--         MovementTypeName, Quantity, StockBefore, StockAfter, Reason, CreatedBy, CreatedByName.
-- @DateFromUtc / @DateToUtc: filtro inclusivo en UTC sobre CreatedAtUtc.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE [dbo].[usp_InventoryMovements_List]
    @Search           NVARCHAR(200) = NULL,
    @Page             INT = 1,
    @PageSize         INT = 20,
    @SortColumn       NVARCHAR(50) = NULL,
    @SortDirection    NVARCHAR(4) = NULL,
    @MovementTypeId   INT = NULL,
    @DateFromUtc      DATETIME2(3) = NULL,
    @DateToUtc        DATETIME2(3) = NULL,
    @TotalRows        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Page IS NULL OR @Page < 1 SET @Page = 1;
    IF @PageSize IS NULL OR @PageSize < 1 SET @PageSize = 20;
    IF @PageSize > 100 SET @PageSize = 100;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    DECLARE @Sort NVARCHAR(50) = LOWER(LTRIM(RTRIM(ISNULL(@SortColumn, N''))));
    -- Default pantalla Historial: fecha ↓
    DECLARE @Dir NVARCHAR(4) =
        CASE
            WHEN NULLIF(LTRIM(RTRIM(@SortDirection)), N'') IS NULL
                 AND (@Sort = N'' OR @Sort IN (N'date', N'fecha', N'createdatutc'))
                THEN N'DESC'
            WHEN UPPER(LTRIM(RTRIM(ISNULL(@SortDirection, N'')))) = N'DESC' THEN N'DESC'
            ELSE N'ASC'
        END;

    ;WITH Base AS (
        SELECT
            m.[Id],
            m.[CreatedAtUtc],
            m.[ProductId],
            p.[Code] AS [ProductCode],
            p.[Name] AS [ProductName],
            m.[MovementTypeId],
            mt.[Code] AS [MovementTypeCode],
            mt.[Name] AS [MovementTypeName],
            m.[Quantity],
            m.[StockBefore],
            m.[StockAfter],
            m.[Reason],
            m.[CreatedBy],
            up.[DisplayName] AS [CreatedByName]
        FROM [dbo].[InventoryMovements] AS m
        INNER JOIN [dbo].[Products] AS p ON p.[Id] = m.[ProductId]
        INNER JOIN [dbo].[MovementTypes] AS mt ON mt.[Id] = m.[MovementTypeId]
        LEFT JOIN [dbo].[UserProfiles] AS up ON up.[Id] = m.[CreatedBy]
        WHERE (@MovementTypeId IS NULL OR m.[MovementTypeId] = @MovementTypeId)
          AND (@DateFromUtc IS NULL OR m.[CreatedAtUtc] >= @DateFromUtc)
          AND (@DateToUtc IS NULL OR m.[CreatedAtUtc] <= @DateToUtc)
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
                        (b.[ProductCode] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (b.[ProductName] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (b.[MovementTypeName] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (b.[MovementTypeCode] COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (ISNULL(b.[Reason], N'') COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (ISNULL(b.[CreatedByName], N'') COLLATE Latin1_General_CI_AI) LIKE N'%' + [dbo].[fn_EscapeLikePattern](w.[Word]) + N'%'
                        OR (TRY_CONVERT(INT, w.[Word]) IS NOT NULL AND b.[Quantity] = TRY_CONVERT(INT, w.[Word]))
                        OR (TRY_CONVERT(INT, w.[Word]) IS NOT NULL AND b.[StockBefore] = TRY_CONVERT(INT, w.[Word]))
                        OR (TRY_CONVERT(INT, w.[Word]) IS NOT NULL AND b.[StockAfter] = TRY_CONVERT(INT, w.[Word]))
                    )
              )
          )
    )
    SELECT @TotalRows = COUNT(1) FROM Filtered;

    SELECT
        f.[Id],
        f.[CreatedAtUtc],
        f.[ProductId],
        f.[ProductCode],
        f.[ProductName],
        f.[MovementTypeId],
        f.[MovementTypeCode],
        f.[MovementTypeName],
        f.[Quantity],
        f.[StockBefore],
        f.[StockAfter],
        f.[Reason],
        f.[CreatedBy],
        f.[CreatedByName]
    FROM Filtered AS f
    ORDER BY
        CASE WHEN @Dir = N'ASC'  AND (@Sort = N'' OR @Sort IN (N'date', N'fecha', N'createdatutc')) THEN f.[CreatedAtUtc] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort = N'' OR @Sort IN (N'date', N'fecha', N'createdatutc')) THEN f.[CreatedAtUtc] END DESC,
        CASE WHEN @Dir = N'ASC'  AND (@Sort IN (N'product', N'producto', N'productname')) THEN f.[ProductName] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort IN (N'product', N'producto', N'productname')) THEN f.[ProductName] END DESC,
        CASE WHEN @Dir = N'ASC'  AND (@Sort IN (N'type', N'tipo', N'movementtype')) THEN f.[MovementTypeName] END ASC,
        CASE WHEN @Dir = N'DESC' AND (@Sort IN (N'type', N'tipo', N'movementtype')) THEN f.[MovementTypeName] END DESC,
        f.[CreatedAtUtc] DESC,
        f.[Id] DESC
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END;
GO


/* --- 020_usp_Report_Stock.sql --- */

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


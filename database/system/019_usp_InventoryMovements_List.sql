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

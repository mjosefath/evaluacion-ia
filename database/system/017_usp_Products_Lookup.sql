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

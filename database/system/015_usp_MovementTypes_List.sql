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

-- =============================================================================
-- InventarioIdentity — borra objetos de Identity para re-ejecutar 001_Identity.sql.
-- No borra la base de datos. Ejecutar en InventarioIdentity.
-- =============================================================================

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @sql NVARCHAR(MAX) = N'';

-- 1) FK de tablas AspNet* y RefreshTokens
SELECT @sql += N'ALTER TABLE '
    + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.'
    + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
    + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = N'dbo'
  AND (
        OBJECT_NAME(fk.parent_object_id) LIKE N'AspNet%'
     OR OBJECT_NAME(fk.parent_object_id) = N'RefreshTokens'
  );

IF LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

-- 2) Tablas
SET @sql = N'';
SELECT @sql += N'DROP TABLE '
    + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';' + CHAR(10)
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name = N'dbo'
  AND (t.name LIKE N'AspNet%' OR t.name = N'RefreshTokens');

IF LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;
GO

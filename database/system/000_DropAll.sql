-- =============================================================================
-- Inventario (Sistema) — borra objetos para re-ejecutar Schema/Seed/Data/Procedures.
-- No borra la base de datos. Ejecutar en Inventario.
-- Orden: procedimientos → función → vistas → FK → tablas.
-- =============================================================================

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @sql NVARCHAR(MAX) = N'';

-- 1) Procedimientos usp_*
SELECT @sql += N'DROP PROCEDURE '
    + QUOTENAME(SCHEMA_NAME(p.schema_id)) + N'.' + QUOTENAME(p.name) + N';' + CHAR(10)
FROM sys.procedures AS p
WHERE SCHEMA_NAME(p.schema_id) = N'dbo'
  AND p.name LIKE N'usp[_]%';

IF LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

-- 2) Función de escape LIKE
IF OBJECT_ID(N'dbo.fn_EscapeLikePattern', N'FN') IS NOT NULL
    DROP FUNCTION [dbo].[fn_EscapeLikePattern];
GO

-- 3) Vistas (p. ej. vw_ProductStock)
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql += N'DROP VIEW '
    + QUOTENAME(SCHEMA_NAME(v.schema_id)) + N'.' + QUOTENAME(v.name) + N';' + CHAR(10)
FROM sys.views AS v
WHERE SCHEMA_NAME(v.schema_id) = N'dbo'
  AND v.name LIKE N'vw[_]%';

IF LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

-- 4) Todas las FK en dbo (cualquier tabla de aplicación)
SET @sql = N'';
SELECT @sql += N'ALTER TABLE '
    + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.'
    + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
    + N' DROP CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk
WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = N'dbo';

IF LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

-- 5) Todas las tablas de usuario en dbo
SET @sql = N'';
SELECT @sql += N'DROP TABLE '
    + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name) + N';' + CHAR(10)
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE s.name = N'dbo'
  AND t.is_ms_shipped = 0;

IF LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;
GO

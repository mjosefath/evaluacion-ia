-- Permisos de inventario (análisis 02) → Administrador.
-- StatusId = 1, CreatedBy = NULL. Idempotente (UX Code / UX RoleName+PermissionId).

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @RoleName NVARCHAR(256) = N'Administrador';
DECLARE @StatusId TINYINT = 1;

DECLARE @Perms TABLE (
    [Code] NVARCHAR(100) NOT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [Module] NVARCHAR(100) NOT NULL
);

INSERT INTO @Perms ([Code], [Name], [Module])
VALUES
    (N'productos.ver', N'Ver productos', N'productos'),
    (N'productos.crear', N'Crear productos', N'productos'),
    (N'productos.editar', N'Editar productos', N'productos'),
    (N'productos.activar', N'Activar o desactivar productos', N'productos'),
    (N'movimientos.entrada', N'Registrar entrada de inventario', N'movimientos'),
    (N'movimientos.salida', N'Registrar salida de inventario', N'movimientos'),
    (N'movimientos.ajuste', N'Registrar ajuste de inventario', N'movimientos'),
    (N'historial.ver', N'Ver historial de movimientos', N'historial'),
    (N'reporte.ver', N'Ver reporte de existencias', N'reporte');

DECLARE @Code NVARCHAR(100);
DECLARE @Name NVARCHAR(200);
DECLARE @Module NVARCHAR(100);
DECLARE @PermissionId INT;

DECLARE perm_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT [Code], [Name], [Module]
    FROM @Perms;

OPEN perm_cursor;
FETCH NEXT FROM perm_cursor INTO @Code, @Name, @Module;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[Permissions]
        WHERE [Code] = @Code
    )
    BEGIN
        INSERT INTO [dbo].[Permissions] ([Code], [Name], [Module], [StatusId], [CreatedBy])
        VALUES (@Code, @Name, @Module, @StatusId, NULL);
    END;

    SELECT @PermissionId = [Id]
    FROM [dbo].[Permissions]
    WHERE [Code] = @Code;

    IF @PermissionId IS NOT NULL
       AND NOT EXISTS (
           SELECT 1
           FROM [dbo].[RolePermissions]
           WHERE [RoleName] = @RoleName
             AND [PermissionId] = @PermissionId
       )
    BEGIN
        INSERT INTO [dbo].[RolePermissions] ([RoleName], [PermissionId], [StatusId], [CreatedBy])
        VALUES (@RoleName, @PermissionId, @StatusId, NULL);
    END;

    FETCH NEXT FROM perm_cursor INTO @Code, @Name, @Module;
END;

CLOSE perm_cursor;
DEALLOCATE perm_cursor;
GO

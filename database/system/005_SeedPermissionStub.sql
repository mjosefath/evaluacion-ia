-- Stub sistema.inicio → Administrador (valida el mecanismo de permisos).
-- StatusId = 1 (Activo), CreatedBy = NULL (sistema). Seguro ante re-ejecución.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @Code NVARCHAR(100) = N'sistema.inicio';
DECLARE @Name NVARCHAR(200) = N'Ver Inicio';
DECLARE @Module NVARCHAR(100) = N'sistema';
DECLARE @RoleName NVARCHAR(256) = N'Administrador';
DECLARE @StatusId TINYINT = 1;
DECLARE @PermissionId INT;

IF NOT EXISTS (
    SELECT 1
    FROM [dbo].[Permissions]
    WHERE [Code] = @Code
)
BEGIN
    INSERT INTO [dbo].[Permissions] ([Code], [Name], [Module], [StatusId], [CreatedBy])
    VALUES (@Code, @Name, @Module, @StatusId, NULL);
END

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
END
GO

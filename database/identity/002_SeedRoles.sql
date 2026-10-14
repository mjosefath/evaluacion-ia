-- Rol semilla Administrador. Seguro ante re-ejecución (IF NOT EXISTS).
-- No crea usuarios ni contraseñas.

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @RoleId NVARCHAR(450) = N'a0000000-0000-4000-8000-000000000001';
DECLARE @RoleName NVARCHAR(256) = N'Administrador';
DECLARE @NormalizedName NVARCHAR(256) = N'ADMINISTRADOR';

IF NOT EXISTS (
    SELECT 1
    FROM [dbo].[AspNetRoles]
    WHERE [NormalizedName] = @NormalizedName
)
BEGIN
    INSERT INTO [dbo].[AspNetRoles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp])
    VALUES (@RoleId, @RoleName, @NormalizedName, N'00000000-0000-4000-8000-000000000001');
END
GO

# Inventario

Sistema de inventario (.NET 10): API + Web MVC. Arranque local según el análisis aprobado.

## 1. Prerrequisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server local (Instancia por defecto, LocalDB o Express)

## 2. Crear las bases de datos

Nombres sugeridos:

| Uso | Nombre sugerido |
|-----|-----------------|
| Identity (usuarios, roles, refresh tokens) | `InventarioIdentity` |
| Sistema (perfiles, permisos, bitácora, inventario) | `Inventario` |

Ejemplo con `sqlcmd` (ajusta servidor e instancia):

```bash
sqlcmd -S localhost -Q "CREATE DATABASE InventarioIdentity"
sqlcmd -S localhost -Q "CREATE DATABASE Inventario"
```

## 3. Ejecutar scripts SQL (en orden)

Cinco scripts vírgenes (CREATE/INSERT). Si ya hay objetos, primero los `000_DropAll` (o borra/recrea la BD).

### Limpiar (opcional, desarrollo)

```bash
sqlcmd -S localhost -d InventarioIdentity -i database/identity/000_DropAll.sql
sqlcmd -S localhost -d Inventario -i database/system/000_DropAll.sql
```

### Crear (orden fijo)

Sobre **InventarioIdentity**:

1. `database/identity/001_Identity.sql` — esquema AspNet* + RefreshTokens + rol Administrador

Sobre **Inventario** (BD Sistema):

1. `database/system/001_Schema.sql` — tablas + `vw_ProductStock` (+ seed mínimo `RecordStatuses`)
2. `database/system/002_Seed.sql` — categorías, tipos de movimiento y permisos
3. `database/system/003_Data.sql` — productos y movimientos de carga inicial
4. `database/system/004_Procedures.sql` — función LIKE + procedimientos de listado/reporte

```bash
sqlcmd -S localhost -d InventarioIdentity -i database/identity/001_Identity.sql
sqlcmd -S localhost -d Inventario -i database/system/001_Schema.sql
sqlcmd -S localhost -d Inventario -i database/system/002_Seed.sql
sqlcmd -S localhost -d Inventario -i database/system/003_Data.sql
sqlcmd -S localhost -d Inventario -i database/system/004_Procedures.sql
```

Sin ALTER en desarrollo: drop + create (o recrear la BD) si cambia el esquema.

## 4. User-secrets (API)

En Development, los secretos van fuera del repo:

```bash
dotnet user-secrets set "ConnectionStrings:Identity" "Server=localhost;Database=InventarioIdentity;Trusted_Connection=True;TrustServerCertificate=True" --project src/Inventario.Api
dotnet user-secrets set "ConnectionStrings:System" "Server=localhost;Database=Inventario;Trusted_Connection=True;TrustServerCertificate=True" --project src/Inventario.Api
dotnet user-secrets set "Jwt:SigningKey" "REEMPLAZAR_CON_CLAVE_DE_AL_MENOS_32_CARACTERES" --project src/Inventario.Api
dotnet user-secrets set "Seed:Admin:Password" "REEMPLAZAR_PASSWORD_ADMIN_DEV" --project src/Inventario.Api
```

Claves obligatorias en **Inventario.Api**:

| Clave | Notas |
|-------|--------|
| `ConnectionStrings:Identity` | Cadena a `InventarioIdentity` |
| `ConnectionStrings:System` | Cadena a `Inventario` |
| `Jwt:SigningKey` | ≥ 32 caracteres |
| `Seed:Admin:Password` | Solo Development; crea el usuario `admin` al arrancar |

La Web **no** usa cadenas de BD ni `Jwt:SigningKey`. Configura `Api:BaseUrl` en `src/Inventario.Web/appsettings.json` (por defecto `https://localhost:7196/`).

Usuario seed (Development): `Seed:Admin:UserName` = `admin` en `appsettings.Development.json` de la API.

## 5. Compilar y ejecutar

```bash
dotnet build Inventario.sln
```

Dos terminales (perfil **https**):

```bash
dotnet run --project src/Inventario.Api --launch-profile https
dotnet run --project src/Inventario.Web --launch-profile https
```

Puertos (`launchSettings.json`):

| Proyecto | HTTPS | HTTP |
|----------|-------|------|
| Api | `https://localhost:7196` | `http://localhost:5278` |
| Web | `https://localhost:7043` | `http://localhost:5205` |

La Web debe apuntar a la API en `https://localhost:7196/` (`Api:BaseUrl`).

## 6. Abrir la aplicación

1. Abre la Web: `https://localhost:7043`
2. Inicia sesión con usuario `admin` y la contraseña definida en `Seed:Admin:Password`
3. Tras el login verás **Inicio** (layout autenticado)

## 7. Recordatorios

- **No commitear secretos**: user-secrets, `.env`, connection strings con credenciales, `Jwt:SigningKey`, contraseñas. Ver `.gitignore`.
- **Recrear la BD** si cambia un script en `database/` (editar el CREATE e ir de nuevo; sin ALTER en desarrollo).
- El seeder admin solo corre en `Development`.
- Solución: `Inventario.sln` — proyectos bajo `src/`.

# 01 — Estructura inicial del proyecto

## Alcance

Base sobre la que se construye el sistema de inventario: solución .NET 10 con API y Web separadas, dos bases SQL Server (Identity e inventario), sesión Web (cookie) + API (JWT), diseño adaptado desde `recursos/ui`, login con usuario de prueba y página de inicio vacía con el layout final.

Queda fuera: catálogo, almacenes, movimientos, existencias, reportes, publicación/producción y pruebas.

## Entidades y datos

### Bases

| Base | Cadena | Contenido |
|------|--------|-----------|
| Identity | `ConnectionStrings:Identity` | Esquema ASP.NET Identity (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, claims, logins, tokens, etc.). Contraseñas solo como hash de Identity. |
| Sistema | `ConnectionStrings:System` | Perfil de usuario, catálogo de permisos, asignación rol→permiso, bitácora de accesos, y a futuro el inventario. |

Sin llaves foráneas entre bases. En Sistema, el vínculo al usuario es el `IdentityUserId` (`nvarchar(450)`, mismo valor que `AspNetUsers.Id`).

### BD Identity — tablas (esquema estándar)

Scripts en `database/identity/`. Solo CREATE/INSERT. Sin migraciones al arrancar.

- Tablas Identity habituales (`AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetRoleClaims`, `AspNetUserLogins`, `AspNetUserTokens`).
- Rol semilla: `Administrador` (script INSERT).
- Refresh tokens: tabla `RefreshTokens` en Identity (Id, UserId, TokenHash, ExpiresAtUtc, CreatedAtUtc, RevokedAtUtc, ReplacedByTokenHash). El valor en claro solo viaja una vez; se guarda hash.

### BD Sistema — tablas iniciales

Scripts en `database/system/`.

**UserProfiles**

| Campo | Tipo / notas |
|-------|----------------|
| Id | `uniqueidentifier`, PK |
| IdentityUserId | `nvarchar(450)`, único, no FK |
| DisplayName | `nvarchar(200)` |
| IsActive | `bit`, default 1 |
| CreatedAtUtc | `datetime2` |
| CreatedByIdentityUserId | `nvarchar(450)`, null en seed |
| ModifiedAtUtc | `datetime2`, null |
| ModifiedByIdentityUserId | `nvarchar(450)`, null |

**Permissions**

| Campo | Tipo / notas |
|-------|----------------|
| Id | `int` identity, PK |
| Code | `nvarchar(100)`, único (ej. `usuarios.ver`) |
| Name | `nvarchar(200)` |
| Module | `nvarchar(100)` (agrupa menú; en esta etapa solo estructura) |

**RolePermissions**

| Campo | Tipo / notas |
|-------|----------------|
| RoleName | `nvarchar(256)` (nombre del rol Identity, sin FK) |
| PermissionId | FK a Permissions |
| PK | (RoleName, PermissionId) |

En este documento no se cargan permisos de módulos de inventario; solo la estructura y, si hace falta, un permiso stub `sistema.inicio` asignado a `Administrador` para validar el mecanismo.

**AccessLogs** (bitácora de accesos)

| Campo | Tipo / notas |
|-------|----------------|
| Id | `bigint` identity |
| OccurredAtUtc | `datetime2` |
| IdentityUserId | `nvarchar(450)`, null si fallo sin usuario |
| UserNameAttempted | `nvarchar(256)`, null |
| EventType | `nvarchar(50)`: `LoginSuccess`, `LoginFailure`, `Lockout`, `Logout`, `TokenRefresh`, `PasswordChanged` |
| IpAddress | `nvarchar(64)`, null |
| UserAgent | `nvarchar(512)`, null |
| Detail | `nvarchar(500)`, null (sin contraseñas ni tokens) |

### Orden de los primeros scripts

```
database/
  identity/
    001_CreateIdentitySchema.sql      -- tablas AspNet* + RefreshTokens
    002_SeedRoles.sql                 -- rol Administrador
  system/
    001_CreateUserProfiles.sql
    002_CreatePermissions.sql         -- Permissions + RolePermissions
    003_CreateAccessLogs.sql
    004_SeedPermissionStub.sql        -- opcional: sistema.inicio → Administrador
```

El usuario administrador **no** se inserta por SQL: en Development, al arrancar la API, un seeder crea el usuario Identity + perfil Sistema si no existen, con contraseña desde user-secrets.

### Ligadura Identity ↔ perfil

1. Login/refresh exitoso: la API resuelve `UserProfiles` por `IdentityUserId`.
2. Si no hay perfil: lo crea con `DisplayName` = UserName e `IsActive = 1` (idempotente).
3. Si `IsActive = 0`: login denegado aunque Identity acepte la contraseña.
4. Claims del JWT incluyen `sub` (IdentityUserId), `unique_name` / name, roles y códigos de permiso.

## Flujos

### Actor: usuario (navegador → Web → API)

**Login**

1. GET `/Account/Login` (anónimo): formulario usuario + contraseña + antiforgery.
2. POST: Web valida antiforgery y llama `POST /api/auth/login` del cliente tipado.
3. API: Identity `PasswordSignIn` (sin cookie en API); si lockout → 423 + AccessLog `Lockout`; si fallo → 401 + `LoginFailure`; si ok pero perfil inactivo → 403.
4. API emite access JWT + refresh token; persiste hash del refresh; AccessLog `LoginSuccess`.
5. Web guarda en cookie de autenticación (HttpOnly, Secure en no-Dev, SameSite=Lax) un ticket con access token, refresh token y expiración; redirige a Inicio.

**Petición autenticada**

1. Filtro/handler de la Web lee la cookie, pone el Bearer en el `HttpClient`.
2. Si la API responde 401 por token vencido: el handler llama `POST /api/auth/refresh` con el refresh, actualiza la cookie y reintenta **una** vez. El usuario no ve el paso.
3. Si el refresh falla: Web limpia cookie y redirige a Login con mensaje de sesión vencida.

**Sesión vencida / cookie ausente**

- Recurso protegido sin cookie → Login.
- Tras fallo de refresh → Login + aviso.

**Cuenta bloqueada**

- API 423; Web muestra mensaje fijo de bloqueo temporal (sin revelar si el usuario existe en otros casos de fallo: mensaje genérico en 401).

**Logout**

1. POST con antiforgery.
2. Web llama `POST /api/auth/logout` (revoca refresh actual).
3. Limpia cookie; AccessLog `Logout`; redirige a Login.

**Cambio de contraseña**

1. Usuario autenticado, formulario con antiforgery (contraseña actual, nueva, confirmación).
2. Web → `POST /api/auth/change-password`.
3. API: Identity ChangePassword; revoca todos los refresh del usuario; AccessLog `PasswordChanged`.
4. Web obliga a volver a iniciar sesión (limpia cookie).

### Actor: desarrollador (arranque)

1. Crea ambas BD y ejecuta scripts en orden.
2. Configura user-secrets (API y Web).
3. Arranca API (seeder admin en Development) y Web.
4. Entra con usuario de prueba → Inicio vacío con layout.

## Reglas de negocio

- R1. Solo la API accede a las bases de datos.
- R2. La Web nunca referencia proyectos de datos ni tipos de entidades EF; solo Contracts + ApiClient.
- R3. Contraseñas: únicamente hash Identity; nunca texto plano en BD, logs, DTOs de respuesta ni commits.
- R4. Secretos (cadenas, JWT signing key, contraseña admin seed) fuera del repo: user-secrets en local, variables de entorno al publicar.
- R5. Scripts SQL numerados, vírgenes (CREATE/INSERT). Cambio de esquema = editar CREATE y recrear BD; sin ALTER en desarrollo.
- R6. Lockout Identity: 5 intentos fallidos, bloqueo 15 minutos (configurable en código/appsettings no secretos).
- R7. Access token: 15 minutos. Refresh token: 7 días. Renovación silenciosa vía ApiClient.
- R8. Perfil inactivo no autentica.
- R9. Antiforgery en todo POST/PUT/DELETE de formularios Razor.
- R10. En Development, si no existe el usuario admin configurado, se crea al arrancar la API (Identity + perfil + rol Administrador).

## Pantallas

### Login (`/Account/Login`)

- Formulario centrado sobre canvas `#f5f5f5`; tarjeta paper 24px, borde hairline.
- Campos: usuario, contraseña; botón primary filled; mensajes de error con color ember solo en texto de error.
- Sin menú lateral. Foco inicial en usuario; Tab/Enter.

### Inicio (`/`, autenticado)

- Layout completo: sidebar + encabezado + área de contenido vacía (título “Inicio”, sin widgets).
- Sin pérdida de datos (N/A); sin redirecciones extra tras login.

### Cambio de contraseña

- Formulario bajo el layout autenticado (o ruta `/Account/ChangePassword`).
- Validación de coincidencia en cliente básica; autoridad en API.

### Parciales de UI (Razor)

Adaptación de `recursos/ui/DESIGN.md` + tokens de `recursos/ui/theme.css` (copiados a Web, no reinventar):

| Parcial / pieza | Función |
|-----------------|--------|
| `_Layout.cshtml` | Canvas, grid sidebar + main, max-width contenido 1280px |
| `_Sidebar.cshtml` | Superficie `#fafafa`; marca; ítems de menú (por ahora solo Inicio) |
| `_Header.cshtml` | Usuario DisplayName, menú cuenta (cambio contraseña, cerrar sesión) |
| `_Alerts.cshtml` | TempData / mensajes éxito, info, error (ember solo error) |
| `_Confirm.cshtml` | Diálogo confirmación destructiva/neutra |
| `_Loading.cshtml` | Estado de carga (overlay o barra discreta) |
| Login layout | Layout mínimo sin sidebar |

Librerías JS/CSS en `wwwroot/lib` (locales, sin CDN). Licencias solo MIT, Apache o BSD. Tipografía Geist (o Inter como sustituto del DESIGN) servida en local. Iconos geométricos tipo Lucide (MIT) en local si se usan.

Captura rápida: login usable solo con teclado; menú con foco visible; botones según tokens (primary/ghost/outline).

## Permisos

Estructura (sin permisos de inventario aún):

| Acción | Quién |
|--------|--------|
| Ver Login | Anónimo |
| Iniciar / cerrar sesión | Anónimo / autenticado |
| Ver Inicio | Autenticado (+ permiso stub `sistema.inicio` o solo autenticado en esta etapa) |
| Cambiar propia contraseña | Autenticado |
| Administración de usuarios/roles | Fuera de este punto |

### Cómo se revisan

- **API:** JWT Bearer. Políticas `Authorize` por rol y/o permiso (`IAuthorizationHandler` que lee claim `permission`). Toda acción sensible valida en servidor.
- **Web:** Cookie con claims espejo (roles, permisos). Menú y botones se ocultan sin claim; el servidor MVC también exige auth. La API sigue siendo la autoridad.

## Reglas que impone a otros módulos

- Todo módulo nuevo: tablas solo en BD Sistema vía scripts numerados; scaffolding al DbContext Sistema; DTOs en Contracts; llamadas desde Web solo por ApiClient.
- Cada tabla de negocio debe incluir columnas de auditoría (`Created*` / `Modified*`) como UserProfiles.
- Permisos nuevos: INSERT en `Permissions` + `RolePermissions`; códigos estables `modulo.accion`.
- Ningún proyecto Web referencia Data.*.

## Decisiones técnicas tomadas

| Decisión | Por qué |
|----------|---------|
| Solución `Inventario.sln` con 7 proyectos (ver abajo) | Separa API/Web, evita filtrar EF a la UI y concentra casos de uso. |
| `Inventario.Application` entre API y Data | Login, JWT, seed y perfiles no viven en controladores ni en Web. |
| Refresh token con hash en BD Identity | Renovación silenciosa sin alargar el access token ni guardar JWT eterno. |
| Permisos en BD Sistema por `RoleName` | Sin FK cross-database; roles Identity siguen siendo la fuente de membresía. |
| ProblemDetails (RFC 7807) + extensión de errores de validación por campo | Contrato uniforme para el ApiClient y la Web. |
| Serilog: consola + archivo rolling en Development | Diagnóstico local sin secretos en sinks remotos. |
| Cookie de autenticación de la Web cifra access+refresh (Data Protection) | Un solo mecanismo de sesión de navegador; tokens no legibles por JS. |
| Seeder admin solo `IsDevelopment()` | Evita crear admin automático fuera de desarrollo. |
| Nombre de producto en UI: `Inventario` | Alineado al contexto del repo; se puede renombrar después sin tocar auth. |
| Login por **nombre de usuario** (UserName) | Operación diaria más corta que correo; email Identity opcional/único interno. |
| Bitácora de accesos **sí** (auth); no bitácora genérica de cada UPDATE aún | Cumple auditoría de quién entró; el quién/cuándo de datos va en columnas por fila. |

### Solución y referencias

```
Inventario.sln
├── src/
│   ├── Inventario.Api                 # Host HTTP, auth Bearer, Serilog, seeder
│   ├── Inventario.Web                 # MVC Razor, cookie, antiforgery
│   ├── Inventario.Contracts           # DTOs / contratos de API (sin EF)
│   ├── Inventario.ApiClient           # Cliente HTTP tipado (Refit o HttpClient generado)
│   ├── Inventario.Application         # Casos de uso: auth, perfiles, permisos
│   ├── Inventario.Data.Identity       # DbContext Identity + RefreshTokens
│   └── Inventario.Data.System         # DbContext Sistema (scaffolding database-first)
└── database/
    ├── identity/
    └── system/
```

| Proyecto | Referencia a | No referencia |
|----------|--------------|---------------|
| Web | ApiClient, Contracts | Data.*, Application, Api |
| ApiClient | Contracts | todo lo demás |
| Api | Application, Contracts, (host) | Web |
| Application | Contracts, Data.Identity, Data.System | Web, Api, ApiClient |
| Data.Identity | — (paquetes EF/Identity) | Contracts, Web |
| Data.System | — (paquetes EF) | Contracts, Web |

Cliente HTTP: interfaz tipada sobre `HttpClient` (paquete MIT, p. ej. Refit MIT, o `IHttpClientFactory` + clases propias sin dependencia extra). Handler: Authorization + refresh-on-401.

API: únicos dueños de `IdentityDbContext` y `SystemDbContext`.

### Endpoints Auth (API)

| Método | Ruta | Auth |
|--------|------|------|
| POST | `/api/auth/login` | Anónimo |
| POST | `/api/auth/refresh` | Anónimo (body/cookie server-side refresh) |
| POST | `/api/auth/logout` | Bearer |
| POST | `/api/auth/change-password` | Bearer |
| GET | `/api/auth/me` | Bearer (DisplayName, roles, permisos; para hidratar UI si hace falta) |

### Configuración — claves

Valores no secretos en `appsettings.json` / `appsettings.Development.json`. Secretos solo user-secrets (local) o variables de entorno (publicación). **Nunca** en repo.

#### Inventario.Api

| Clave | Dónde | Notas |
|-------|--------|------|
| `ConnectionStrings:Identity` | secret / env | |
| `ConnectionStrings:System` | secret / env | |
| `Jwt:Issuer` | appsettings | p. ej. `Inventario.Api` |
| `Jwt:Audience` | appsettings | p. ej. `Inventario.Web` |
| `Jwt:SigningKey` | secret / env | ≥ 32 chars |
| `Jwt:AccessTokenMinutes` | appsettings | `15` |
| `Jwt:RefreshTokenDays` | appsettings | `7` |
| `Identity:Lockout:MaxFailedAccessAttempts` | appsettings | `5` |
| `Identity:Lockout:DefaultLockoutMinutes` | appsettings | `15` |
| `Seed:Admin:UserName` | appsettings.Development | p. ej. `admin` |
| `Seed:Admin:Password` | secret / env | solo Development |
| `Seed:Admin:DisplayName` | appsettings.Development | p. ej. `Administrador` |
| `Serilog` | appsettings | niveles y sinks |

Variables de entorno (ejemplo): `ConnectionStrings__Identity`, `ConnectionStrings__System`, `Jwt__SigningKey`, `Seed__Admin__Password`.

#### Inventario.Web

| Clave | Dónde | Notas |
|-------|--------|------|
| `Api:BaseUrl` | appsettings | p. ej. `https://localhost:7xxx/` |
| `Auth:CookieName` | appsettings | p. ej. `Inventario.Session` |
| `Auth:CookieExpireDays` | appsettings | alineado al refresh (7) |
| Secretos de Data Protection | automáticos en local; en publicación store compartido si hay varias instancias (fuera de alcance ahora) | |

La Web **no** tiene cadenas de BD ni JWT signing key.

### Errores

| Caso | API | Web | Log |
|------|-----|-----|-----|
| Validación | 400 ProblemDetails + `errors` por campo | Mensajes junto al campo / resumen | Warning sin PII sensible |
| Credenciales inválidas | 401 mensaje genérico | Aviso en Login | Information + AccessLog Failure |
| Lockout | 423 | Mensaje de bloqueo | Warning + AccessLog Lockout |
| Perfil inactivo / prohibido | 403 | Mensaje | Warning |
| No autenticado / token inválido | 401 | Refresh o redirect Login | Information |
| No autorizado (permiso) | 403 | Página/aviso no autorizado | Warning |
| No encontrado | 404 | Mensaje | Information |
| Error no controlado | 500 ProblemDetails genérico (sin stack al cliente) | Página error amable | Error con excepción completa en Serilog |

Nunca loguear: contraseñas, tokens, cookies, authorization headers.

### README inicial (contenido obligatorio del repo)

El `README.md` raíz debe indicar, en orden:

1. Prerrequisitos: .NET 10 SDK, SQL Server local.
2. Crear BD `InventarioIdentity` e `Inventario` (nombres sugeridos).
3. Ejecutar scripts `database/identity/*.sql` y `database/system/*.sql` en orden.
4. `dotnet user-secrets` en Api (connection strings, Jwt:SigningKey, Seed:Admin:Password) y, si aplica, nada sensible en Web salvo lo futuro.
5. `dotnet run` Api y Web (dos terminales) o perfil de compilación múltiple.
6. Abrir Web, login con `admin` y la contraseña del secret.
7. Recordatorio: no commitear secretos; recrear BD si cambia un script.

## Preguntas de negocio pendientes

Ninguna bloqueante. Login por nombre de usuario y nombre de producto `Inventario` quedan como decisiones técnicas de este documento; si el dueño prefiere correo o otro nombre comercial, se actualiza aquí con una decisión fechada.

## Decisiones del dueño

| Fecha | Decisión |
|-------|----------|
| 2026-03-25 | Dos proyectos host: API y Web, ambos .NET 10. |
| 2026-03-25 | Solo la API toca BD; Web MVC Razor consume API por HTTP. |
| 2026-03-25 | Cliente HTTP tipado + proyecto de contratos (DTOs); Web sin entidades EF. |
| 2026-03-25 | Dos BD SQL Server: Identity e inventario/sistema; sin FK entre ellas; perfil en sistema con id Identity. |
| 2026-03-25 | Sistema database-first: scripts numerados CREATE/INSERT en `database/`; scaffolding; recrear si cambia. Identity por script, sin migraciones al arrancar. |
| 2026-03-25 | Web cookie + API JWT; login vía API; renovación silenciosa; lockout; logout; antiforgery en formularios. |
| 2026-03-25 | Hash Identity; secretos en user-secrets / env; nunca en repo ni appsettings con secretos. |
| 2026-03-25 | UI desde `recursos/ui`; adaptar a Razor; libs locales en `wwwroot/lib`; licencias MIT/Apache/BSD. |
| 2026-03-25 | Serilog; en Development crear admin si no existe con contraseña desde user-secrets. |
| 2026-03-25 | Fuera de alcance: inventario funcional, reportes, producción, pruebas. |

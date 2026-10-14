# ADR-001 — Estructura de la solución

## Contexto

Se inicia el sistema de inventario desde cero. Hay que separar el host HTTP de la UI, impedir que la Web toque Entity Framework o las bases, y dejar un lugar claro para contratos, cliente tipado y casos de uso. El dueño aprobó dos hosts .NET 10 (API + Web MVC) y dos bases SQL Server (Identity y Sistema).

## Decisión

Solución `Inventario.sln` con siete proyectos bajo `src/` y scripts SQL en `database/`:

| Proyecto | Responsabilidad |
|----------|-----------------|
| `Inventario.Api` | Host HTTP; auth Bearer; Serilog; seeder de desarrollo. Único dueño de los DbContext. |
| `Inventario.Web` | MVC Razor; cookie de sesión; antiforgery. Solo consume la API por HTTP. |
| `Inventario.Contracts` | DTOs y contratos de API. Sin EF. |
| `Inventario.ApiClient` | Cliente HTTP tipado hacia la API. |
| `Inventario.Application` | Casos de uso (auth, perfiles, permisos, etc.). |
| `Inventario.Data.Identity` | DbContext Identity + refresh tokens. |
| `Inventario.Data.System` | DbContext Sistema (database-first). |

Referencias permitidas:

- Web → ApiClient, Contracts
- ApiClient → Contracts
- Api → Application, Contracts
- Application → Contracts, Data.Identity, Data.System
- Data.Identity y Data.System → ningún proyecto de la solución
- Web no referencia Data.*, Application ni Api

Scripts: `database/identity/` y `database/system/` (CREATE/INSERT numerados; sin migraciones al arrancar).

## Alternativas descartadas

| Alternativa | Por qué no |
|-------------|------------|
| Monolito MVC con acceso directo a BD | Filtra entidades EF a la UI y complica un futuro cliente no-Web. |
| Un solo proyecto Api+Web (Blazor/Razor en el mismo host) | Choca con la decisión del dueño de API y Web separadas. |
| Shared kernel con entidades compartidas a Web | Rompe R2: la Web no debe conocer tipos de datos. |
| Más de dos capas de datos o un solo DbContext | No refleja las dos bases ni el aislamiento Identity/Sistema. |

## Consecuencias

- La Web solo conoce Contracts y ApiClient; cualquier pantalla nueva pasa por DTOs + cliente HTTP.
- Los casos de uso viven en Application; los controladores de Api quedan delgados.
- Cambios de esquema se hacen en scripts vírgenes y se recrea la BD en desarrollo.
- El grafo de referencias queda fijado; añadir una referencia prohibida (p. ej. Web → Data.*) requiere un ADR nuevo o adenda.

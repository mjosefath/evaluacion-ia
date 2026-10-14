# 03 — Errores, avisos, alertas y validaciones (estándar único)

## Alcance

Palabras del dueño: "mi lógica de errores, avisos y alertas, validaciones, debe ser de todo lo que hemos
implementado, para mantener sólido y unificado la estructura y el desarrollo".

Aplica a todo lo construido (`01`: sesión, cambio de contraseña, layout), a todo lo analizado (`02`: productos,
movimientos, historial, reporte y catálogos) y a todo módulo futuro. Cubre API, ApiClient, Web, Application y logs.

Fuera: monitoreo y alertas de producción, envío de correos o notificaciones externas, y pruebas.

## Estado actual verificado en el repo

| # | Dónde | Qué hay hoy | Problema |
|---|-------|-------------|----------|
| A1 | `Application/Auth/AuthResult`, `AuthErrorCode` (ADR-002) | Patrón de resultado solo para autenticación | Productos y movimientos no tienen un patrón; cada módulo inventaría el suyo |
| A2 | `Api/Controllers/AuthResultHttpMapper` | Mapeo a HTTP exclusivo de auth; `title` en inglés ("Unauthorized"), `detail` en español; sin código de error | No es reutilizable y la Web no puede distinguir los errores por código |
| A3 | `Contracts/Errors/ValidationProblemDetailsDto` | Solo `errors` por campo | No incluye `code` ni `traceId` |
| A4 | `Web/Controllers/AccountController` | Reescribe los mensajes de la API ("Credenciales inválidas.", "Cuenta bloqueada…") y traduce errores con `ApplyChangePasswordApiErrors` / `MapChangePasswordField` | Mensajes duplicados en dos lugares; traducción hecha a mano por pantalla |
| A5 | `ApiClient/Auth/InventarioApiException` | `IsUnauthorized` trata el 403 como 401 | Mezcla "sin sesión" con "sin permiso" |
| A6 | `Web/Filters/InventarioApiExceptionFilter` y `Web/Program.cs` | Solo atiende el 401; `AccessDeniedPath = /Account/Login` | Un 403 termina en la página de error o manda al Login aunque haya sesión |
| A7 | `Web/Views/Shared/_Alerts.cshtml` | `TempData["Success" / "Info" / "Error"]` con textos literales | No hay tipo advertencia, no hay avisos para respuestas AJAX y las llaves están dispersas |
| A8 | `Web/Views/Shared/_Confirm.cshtml` | Texto fijo "¿Deseas continuar?"; Confirmar solo cierra el diálogo | No es reutilizable para desactivar productos, etc. |
| A9 | `Web/wwwroot/js/site.js` | Error genérico del modal con texto fijo en el script | No usa el mismo mecanismo de avisos |
| A10 | `Web/Models/ChangePasswordViewModel` | Validaciones solo en la Web; la API valida por separado | Reglas y mensajes duplicados |
| A11 | `Web/Program.cs` | Sin Serilog | Los errores de la Web no quedan en ningún log |
| A12 | `Web/Views/Shared/Error.cshtml` | Muestra el Request ID solo si `ShowRequestId` | El usuario no siempre tiene un código para reportar el error |

## Flujos

### Recorrido de un error (igual para todo el sistema)

1. **Web (cliente):** valida formato antes de enviar, con las mismas reglas y mensajes que el servidor (V2).
2. **Web (servidor):** vuelve a validar el formulario y llama a la API con el ApiClient.
3. **API:** valida el DTO → Application aplica las reglas de negocio → la BD aplica sus restricciones.
4. Cualquier fallo se convierte en un `Result` con un **código de error** (E1).
5. Un **único mapeador** de la API transforma ese código en HTTP + `ApiProblem` (E3).
6. El **ApiClient** convierte la respuesta en un `ApiError` tipado; la Web no lee JSON a mano.
7. Un **único traductor** de la Web decide dónde mostrarlo: en el campo, arriba del formulario, como aviso, en una
   página o redirigiendo (W1).
8. Los dos procesos registran el evento con el mismo `traceId` (L1).

## Reglas

### Errores: códigos y respuesta de la API

- E1. Application devuelve `Result` / `Result<T>` para **todos** los casos de uso (no solo auth). Un fallo lleva
  `Code`, un `Message` para el usuario y `FieldErrors` opcionales. Las reglas de negocio nunca se lanzan como
  excepción.
- E2. Catálogo único de códigos (texto estable en minúsculas, se amplía por módulo y nunca se renombra):

| Código | HTTP | Cuándo |
|--------|:----:|--------|
| `validation` | 400 | Formato o datos de entrada inválidos (con `errors` por campo) |
| `business.rule` | 400 | Regla de negocio violada; el subcódigo va en `rule` (ver tabla siguiente) |
| `auth.invalid_credentials` | 401 | Usuario o contraseña incorrectos (mensaje genérico) |
| `auth.session_expired` | 401 | Token o refresh inválido o vencido |
| `auth.locked` | 423 | Cuenta bloqueada por intentos fallidos |
| `auth.inactive_profile` | 403 | Perfil inactivo |
| `forbidden` | 403 | Sesión válida, pero sin el permiso |
| `not_found` | 404 | El registro no existe |
| `conflict.duplicate` | 409 | Violación de un valor único (con `errors` en el campo) |
| `conflict.concurrency` | 409 | El `RowVersion` no coincide |
| `unexpected` | 500 | Cualquier error no controlado |

| Subcódigo de `business.rule` | Regla de `02` |
|------------------------------|---------------|
| `stock.insufficient` | R5 (salida o ajuste negativo mayor que la existencia) |
| `product.inactive` | R6 |
| `product.has_stock` | R12 |
| `adjustment.reason_required` | R4 |

- E3. Contrato único de error en Contracts (`ApiProblem`, reemplaza a `ValidationProblemDetailsDto`): `type`,
  `title` (**en español**, corto), `status`, `detail` (mensaje para el usuario), `code`, `rule` (opcional),
  `errors` (campo → mensajes) y `traceId`. Toda respuesta de error de la API, incluidos 401, 403, 404 y 500, usa
  este contrato.
- E4. Las llaves de `errors` son el nombre de la propiedad del DTO de la petición, en camelCase (`quantity`,
  `code`). Un error general del formulario usa la llave vacía `""`.
- E5. Un solo mapeador `Result` → HTTP en la API, para todos los controladores (generaliza
  `AuthResultHttpMapper`).
- E6. Las restricciones de la BD nunca llegan como 500: índice único → `conflict.duplicate` en su campo; CHECK o FK
  → `validation`; `RowVersion` → `conflict.concurrency`. Solo lo no previsto es `unexpected`.
- E7. Un 500 nunca expone stack, SQL, nombres de tablas ni ids internos: solo el mensaje genérico y el `traceId`.

### Mensajes

- M1. Todo en español, en segunda persona y en dos partes: qué pasó + qué hacer. Ejemplo: "Existencia
  insuficiente: hay 8 unidades. Reduce la cantidad."
- M2. **Una sola fuente por mensaje:** los mensajes de negocio y de validación del servidor los redacta la API
  (catálogo de mensajes en Application). La Web muestra `detail` y `errors` tal como llegan y no los reescribe.
- M3. La Web solo redacta los mensajes que la API no puede dar: sin conexión, tiempo agotado y error inesperado.
  Están en una sola clase de textos de la Web, nunca sueltos en controladores, vistas ni scripts.
- M4. Login: el mensaje de credenciales inválidas es genérico (no dice si el usuario existe), como pide el contexto.

### Validaciones

- V1. **Capas y responsable:**

| Capa | Qué valida | Autoridad |
|------|------------|:---------:|
| Web cliente (unobtrusive) | Obligatorios, formato, longitud, rango | No (comodidad) |
| Web servidor (ModelState) | Lo mismo que el cliente | No |
| API: DTO | Lo mismo, con los mismos atributos | Sí |
| Application | Reglas de negocio (R1–R12 de `02`) | Sí |
| BD | CHECK, UNIQUE, FK (última barrera) | Sí |

- V2. **Las reglas de formato se declaran una sola vez**, con DataAnnotations y sus mensajes en los DTO de
  petición de Contracts. La Web usa esos DTO en sus formularios (o un ViewModel que los contiene), así la
  validación del cliente, la de la Web y la de la API salen de los mismos atributos.
- V3. Las reglas de negocio solo viven en Application; la Web no las replica (por ejemplo, no calcula si alcanza la
  existencia).
- V4. Orden en la API: formato → existencia del registro → estado (activo/inactivo) → regla de negocio →
  persistencia. Se corta en la primera capa que falla, pero dentro de una capa se devuelven **todos** los errores
  juntos.
- V5. Un error de negocio ligado a un campo va en ese campo (`stock.insufficient` → `quantity`;
  `conflict.duplicate` → `code`); si no tiene campo, va en la llave vacía.
- V6. Los textos se recortan (trim) antes de validar; un texto con solo espacios cuenta como vacío.

### Web: dónde y cómo se muestra (traductor único)

- W1. Un solo componente de la Web traduce un `ApiError`; ningún controlador decide por su cuenta:

| Código | Formulario o modal | Pantalla completa |
|--------|--------------------|-------------------|
| `validation`, `business.rule`, `conflict.duplicate` | Errores en sus campos + resumen arriba del formulario; el modal sigue abierto y conserva lo capturado | Aviso de error |
| `conflict.concurrency` | Aviso de advertencia con el botón "Recargar"; conserva lo capturado | Igual |
| `not_found` | Aviso de error; cierra el modal y refresca el listado | Página "No encontrado" |
| `forbidden` | Aviso "No tienes permiso para esta acción." | Página "Acceso denegado" (no el Login) |
| `auth.session_expired` | Refresh silencioso (de `01`); si falla, Login con "Tu sesión ha vencido" | Igual |
| `auth.invalid_credentials`, `auth.locked`, `auth.inactive_profile` | Solo en Login: mensaje arriba del formulario | — |
| `unexpected`, sin conexión | Aviso de error con "Código de referencia: {traceId}" | Página de error con el código |

- W2. **Errores de campo:** el texto va debajo del campo, el campo se marca con `aria-invalid="true"` y el foco
  pasa al primer campo con error. El resumen solo muestra los errores generales (llave `""`).
- W3. **Avisos (alertas), cuatro tipos con el mismo componente:** éxito, información, advertencia (nuevo) y
  error. Solo el de error usa el color ember (DESIGN.md).

| Tipo | Uso | Se cierra |
|------|-----|-----------|
| Éxito | Operación terminada ("Movimiento guardado: 13 → 10") | Solo, a los 5 s, o con su botón |
| Información | Estado neutro ("Sesión cerrada") | Solo, a los 5 s, o con su botón |
| Advertencia | Algo requiere atención, pero se pudo continuar (concurrencia; movimiento que deja el producto en BAJO, R13 de `02`) | Con su botón |
| Error | La operación no se hizo | Con su botón |

- W4. Los avisos llegan por dos vías y se ven igual: después de redirigir (TempData con llaves tipadas, nunca
  textos literales como llave) y en respuestas AJAX (`InventarioUi.notify(tipo, mensaje)`). Los dos pintan el
  mismo contenedor `_Alerts`, con `aria-live="polite"`, y `role="alert"` en el de error.
- W5. **Respuesta AJAX estándar** entre el controlador de la Web y `site.js` (amplía ADR-004): error de
  formulario → partial HTML con los errores (como hoy); éxito → JSON
  `{ redirectUrl?, notify?: { type, message }, refresh?: true }`. Todo formulario en modal usa este contrato.
- W6. **Confirmaciones:** un solo diálogo `InventarioUi.confirm({ title, message, acceptText, tone })` que
  devuelve sí/no. `tone = danger` (botón ember) para lo irreversible o de baja (desactivar); `neutral` para lo
  demás. Enter no confirma una acción `danger`: el foco inicial queda en Cancelar.
- W7. Se confirma: desactivar o reactivar, y salir de un formulario con cambios sin guardar. No se confirma:
  guardar un alta o una edición, ni registrar un movimiento (la captura es rápida; el aviso de éxito basta).
- W8. **Carga:** durante cualquier petición, el botón que la lanzó queda deshabilitado y con indicador, para evitar
  el doble envío; se muestra la barra `_Loading`. Si tarda más de 10 s, aviso "Sigue procesando…".
- W9. El usuario siempre conserva lo capturado ante cualquier error; nunca se limpia un formulario que falló.

### Logs

- L1. API y Web registran con Serilog, con la misma configuración base. El `traceId` (W3C, el que propaga
  `HttpClient`) es el mismo en la Web y en la API, y es el que ve el usuario como código de referencia.
- L2. Niveles:

| Evento | Nivel |
|--------|-------|
| `validation`, `business.rule`, `not_found`, `conflict.*` | Information |
| `auth.*`, `forbidden` | Warning (además de `AccessLogs` para los eventos de sesión de `01`) |
| `unexpected`, API no disponible desde la Web | Error, con la excepción completa |

- L3. Cada entrada lleva: `traceId`, ruta, código de error, `UserProfileId` (si hay sesión) y duración.
- L4. Nunca se registran contraseñas, tokens, cookies, el encabezado Authorization ni el cuerpo de las peticiones de
  autenticación.

## Pantallas

| Pieza (Razor / JS) | Responsabilidad |
|--------------------|-----------------|
| `_Alerts` | Contenedor único de avisos, con 4 tipos |
| `InventarioUi.notify` | Avisos desde AJAX en el mismo contenedor |
| `_Confirm` + `InventarioUi.confirm` | Confirmación configurable (título, mensaje, botón, tono) |
| `_ActionModal` | Errores dentro del modal, sin cerrarlo (ADR-004) |
| `_Loading` | Barra de carga y bloqueo de doble envío |
| `Error` (vista) | Error inesperado, siempre con el código de referencia |
| `AccessDenied` (vista, nueva) | 403 con sesión válida |
| `NotFound` (vista, nueva) | 404 en pantalla completa |

## Alineación de lo ya construido

| # | Cambio |
|---|--------|
| A1 | `AuthResult` / `AuthErrorCode` pasan a ser el `Result` general con el catálogo E2 (adenda a ADR-002). |
| A2 | `AuthResultHttpMapper` se convierte en el mapeador único (E5), con `title` en español y `code`. |
| A3 | `ValidationProblemDetailsDto` → `ApiProblem` (E3). |
| A4 | `AccountController` deja de reescribir mensajes y de traducir errores a mano; usa el traductor único (W1, M2). |
| A5 | `InventarioApiException` → `ApiError` con `Code`; se separan "sin sesión" (401) y "sin permiso" (403). |
| A6 | El filtro de la Web atiende todos los códigos según W1; `AccessDeniedPath` apunta a la vista AccessDenied. |
| A7 | `_Alerts` con 4 tipos, llaves tipadas y `notify` para AJAX (W3–W4). |
| A8 | `_Confirm` configurable (W6). |
| A9 | `site.js` toma el texto del error genérico de la clase de textos de la Web (M3) y lo muestra con `notify`. |
| A10 | Las validaciones de cambio de contraseña se declaran en `ChangePasswordRequest` (Contracts) (V2). |
| A11 | Serilog en la Web (L1). |
| A12 | `Error` siempre muestra el código de referencia. |

## Reglas que impone a otros módulos

- Todo caso de uso devuelve `Result`; todo endpoint responde errores con `ApiProblem`; todo código nuevo se agrega
  al catálogo E2 (y a la tabla de subcódigos si es de negocio).
- Todo formulario nuevo valida con los atributos de su DTO de Contracts, usa el traductor único y el contrato AJAX
  de W5.
- Ningún mensaje para el usuario se escribe fuera de los dos catálogos: el de Application (servidor) y el de textos
  de la Web.
- El documento `02` usa estos códigos: R5 → `stock.insufficient`, R6 → `product.inactive`, R12 →
  `product.has_stock`, código de producto duplicado → `conflict.duplicate`, edición concurrente →
  `conflict.concurrency`.

## Decisiones técnicas tomadas

| Decisión | Por qué |
|----------|---------|
| `Result` + catálogo de códigos en lugar de excepciones de negocio | Ya probado en auth (ADR-002); se extiende sin cambiar de idea. |
| Códigos de texto estables (no números ni el enum serializado) | La Web y los logs los leen sin depender del orden de un enum. |
| `business.rule` + subcódigo `rule` | Un solo código HTTP para las reglas, con detalle suficiente para la UI y los logs. |
| La API redacta los mensajes; la Web no los reescribe | Una sola fuente de verdad; hoy están duplicados (A4). |
| DataAnnotations en los DTO de Contracts | Contracts ya es compartido y no depende de EF; los atributos son de la BCL. |
| Traductor único en la Web | Elimina las traducciones por pantalla (A4) y garantiza el mismo comportamiento en todas. |
| 403 → página Acceso denegado, no el Login | El usuario sí tiene sesión; mandarlo al Login lo confunde. |
| `traceId` W3C como código de referencia | Ya lo propaga `HttpClient`; une el log de la Web con el de la API sin código extra. |
| No se confirma guardar ni registrar movimientos | Mantiene la captura rápida con teclado; el aviso de éxito ya da retroalimentación. |

- W10. Una respuesta exitosa puede traer un aviso de advertencia que no bloquea la operación (por ejemplo, R13 de
  `02`). La API lo devuelve en el resultado exitoso (`warnings`: código + mensaje) y la Web lo muestra con
  `notify` como advertencia, en lugar del aviso de éxito.

## Preguntas de negocio pendientes

Ninguna.

## Decisiones del dueño

| Fecha | Decisión |
|-------|----------|
| 2026-10-14 | La lógica de errores, avisos, alertas y validaciones es una sola para todo lo implementado y lo que venga, para mantener sólida y unificada la estructura. |
| 2026-10-14 | Si una salida o un ajuste deja el producto en BAJO, se guarda y se avisa con una advertencia (W10; R13 de `02`). |
| 2026-10-14 | Documento aprobado. |
| 2026-10-14 | Se autoriza modificar lo ya construido y aprobado (código, ADR-002 y ADR-004) para aplicar la tabla "Alineación de lo ya construido" (A1–A12). |

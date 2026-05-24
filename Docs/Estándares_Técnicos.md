# Estándares Técnicos: Errores, Logging y Observabilidad

Este documento define la convención común para los 5 microservicios del proyecto E-Commerce (G18), cumpliendo con los requerimientos de la Semana 3.

## 1. Estructura del Contrato de Error
Todas las APIs deben devolver un objeto basado en **RFC 7807 (Problem Details)** para errores 4xx y 5xx.

### Campos Obligatorios
| Campo | Tipo | Fuente |
| :--- | :--- | :--- |
| `type` | string | URI del tipo de error (ej: RFC 7231). |
| `title` | string | Nombre corto del error. |
| `status` | int | Código HTTP. |
| `detail` | string | Explicación amigable del error. |
| `instance` | string | Path del request (`httpContext.Request.Path`). |
| `errorCode` | string | Código del catálogo del servicio (ej: `PRD-001`). |
| `errorMessage` | string | Mensaje técnico del catálogo. |
| `correlationId` | string | ID de trazabilidad (Header `X-Correlation-Id`). |

---

## 2. Manejo de Excepciones (Handlers)
Se utilizará el patrón `IExceptionHandler` de .NET 8.

### Orden de Registro en `Program.cs`
El orden es jerárquico. Los handlers deben registrarse de la siguiente manera:
1. `ValidationExceptionHandler` (400)
2. `NotFoundExceptionHandler` (404)
3. `BusinessRuleExceptionHandler` (409/422)
4. `GlobalExceptionHandler` (500)

---

## 3. Convención de Logs (Serilog)
Para garantizar la observabilidad, los logs deben ser estructurados (JSON) y contener:

- **Propiedades fijas:** `Timestamp`, `Level`, `Service`, `CorrelationId`, `Endpoint`.
- **Eventos de Request:**
    - **Inicio:** Loguear inicio del request con el método y ruta.
    - **Fin:** Loguear finalización con el código de estado y `ElapsedMs`.
- **Niveles sugeridos:**
    - `Warning`: Para errores de negocio (400, 404, 409).
    - `Error`: Para excepciones no controladas capturadas por el GlobalHandler.

---

## 4. Endpoints de Health Checks
Se estandarizan las rutas para el monitoreo de los contenedores:

- `/health`: Estado base de la aplicación.
- `/health/live`: Verifica que el proceso de la API esté respondiendo.
- `/health/ready`: Verifica que las dependencias (Persistencia / Otras APIs) estén disponibles.

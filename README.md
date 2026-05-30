# E-Commerce Microservices - Grupo 18

## 📖 1. Descripción del Proyecto
Este proyecto es una solución basada en una **arquitectura de microservicios** para un sistema de **E-Commerce**, desarrollada en **C# con .NET Core 8** para la materia **"Construcción de Aplicaciones Informáticas"**. 

El sistema consta de 5 microservicios independientes que cooperan entre sí mediante llamadas HTTP, y cuenta con un robusto pipeline transversal para resolver logging estructurado (Serilog), auditoría transaccional, trazabilidad de solicitudes (Correlation ID), monitoreo de salud visual (Health Checks UI) y documentación interactiva (Swagger / OpenAPI con comentarios XML).

---

## 📐 2. Diagrama de Arquitectura
A continuación, se detalla la topología de los microservicios y sus interacciones. Las llamadas de escritura (POST/PUT/DELETE) se auditan automáticamente mediante un middleware dedicado.

```mermaid
graph TD
    Client[Cliente / Swagger UI] -->|HTTP| UsersAPI[Users.API :5002]
    Client -->|HTTP| ProductsAPI[Products.API :5001]
    Client -->|HTTP| CartAPI[Cart.API :5004]
    Client -->|HTTP| OrdersAPI[Orders.API :5003]
    Client -->|HTTP| NotificationsAPI[Notifications.API :5005]

    %% Interacciones
    CartAPI -->|Valida Stock & Info| ProductsAPI
    OrdersAPI -->|Valida Usuario| UsersAPI
    OrdersAPI -->|Valida Stock & Precios| ProductsAPI
    OrdersAPI -->|Notifica Alta| NotificationsAPI

    %% Bases de Datos
    subgraph Persistencia Local (SQLite + Dapper)
        DbUsers[(Users.db)] -.- UsersAPI
        DbProducts[(Products.db)] -.- ProductsAPI
        DbCart[(Cart.db)] -.- CartAPI
        DbOrders[(Orders.db)] -.- OrdersAPI
        DbNotifications[(Notifications.db)] -.- NotificationsAPI
    end

    %% Estilo
    style UsersAPI fill:#f9f,stroke:#333,stroke-width:2px
    style ProductsAPI fill:#bbf,stroke:#333,stroke-width:2px
    style CartAPI fill:#bfb,stroke:#333,stroke-width:2px
    style OrdersAPI fill:#fbb,stroke:#333,stroke-width:2px
    style NotificationsAPI fill:#ybf,stroke:#333,stroke-width:2px
```

---

## 🔌 3. Mapeo de Puertos y Endpoints del Sistema

Cada microservicio está configurado para ejecutarse localmente en puertos fijos e independientes, facilitando la comunicación inter-servicio:

| Microservicio | URL Base Local | Endpoint Swagger | Endpoint Salud (JSON) | Dashboard Monitoreo (UI) |
| :--- | :--- | :--- | :--- | :--- |
| **`Products.API`** | `http://localhost:5001` | `/swagger` | `/health` | `/health-ui` |
| **`Users.API`** | `http://localhost:5002` | `/swagger` | `/health` | `/health-ui` |
| **`Orders.API`** | `http://localhost:5003` | `/swagger` | `/health` | `/health-ui` |
| **`Cart.API`** | `http://localhost:5004` | `/swagger` | `/health` | `/health-ui` |
| **`Notifications.API`** | `http://localhost:5005` | `/swagger` | `/health` | `/health-ui` |

---

## ⚙️ 4. Infraestructura Transversal y Aspectos No Funcionales

### 4.1. Logging Estructurado con Serilog
El sistema inicializa Serilog en cada API leyendo desde `appsettings.json` y configurando dos destinos de log de forma limpia:
* **Consola:** Logs a partir de nivel `Information` formateados de forma simple para depuración rápida.
* **Archivo de Auditoría (`logs/audit.log`):** Filtra para registrar **exclusivamente** eventos de acceso a endpoints (`RequestLoggingMiddleware`), descartando ruidos de consultas a `/swagger` o `/health` automáticos.

### 4.2. Middleware de Auditoría (`AuditMiddleware`)
Cualquier solicitud de escritura (`POST`, `PUT`, `DELETE`) es interceptada por el middleware de auditoría personalizado. Este middleware lee el cuerpo de entrada de la solicitud y el de salida de la respuesta de forma segura (usando streams rebuferizados) y los registra en formato estructurado JSON:
`_logger.LogInformation("AUDIT {@Method} {@Path} {@StatusCode} {@RequestBody} {@ResponseBody}")`

### 4.3. Trazabilidad con Correlation ID
Cada request entrante es interceptada para verificar o autogenerar una cabecera `X-Correlation-Id`. 
* Este ID es devuelto en las cabeceras de respuesta HTTP al cliente.
* Es inyectado en el `LogContext` de Serilog para que **cada línea de log** vinculada a esa petición viaje enriquecida con dicho ID.
* Se propaga en las llamadas HTTP salientes entre microservicios (por ejemplo, cuando `Orders.API` llama a `Products.API`) mediante el inyector `CorrelationIdDelegatingHandler`.

### 4.4. Panel de Monitoreo (Health Checks)
Cada microservicio mapea el endpoint `/health` que expone información de liveness y readiness (uptime, runtime, conectividad de base de datos) en formato JSON estructurado. Adicionalmente, cuenta con el dashboard gráfico `/health-ui` que provee una consola visual interactiva del estado del ecosistema completo.

---

## 🚀 5. Instrucciones de Ejecución

Para iniciar el ecosistema completo localmente:

### Prerrequisitos
* Tener instalado [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0).

### Paso 1: Restaurar y Compilar la Solución
Desde la raíz de la solución, ejecuta en tu terminal:
```bash
dotnet restore ECommerce.Services.G18.sln
dotnet build ECommerce.Services.G18.sln
```

### Paso 2: Iniciar los Microservicios
Puedes abrir 5 terminales independientes en la raíz de cada proyecto y ejecutar:
```bash
# Terminal 1
cd Products.API
dotnet run

# Terminal 2
cd ../Users.API
dotnet run

# Terminal 3
cd ../Orders.API
dotnet run

# Terminal 4
cd ../Cart.API
dotnet run

# Terminal 5
cd ../Notifications.API
dotnet run
```
*Las bases de datos SQLite locales se inicializarán y sembrarán de forma automática la primera vez que se levanten los servicios.*

---

## 📋 6. Catálogo de Errores de Negocio (RFC 7807)
En caso de fallos controlados (4xx) o inesperados (5xx), las APIs retornan un objeto estándar con la siguiente nomenclatura de catálogo:

* **Products.API:**
  * `PRD-001` (404): Producto no encontrado.
  * `PRD-002` (400): Datos del producto inválidos.
  * `PRD-003` (409): Nombre de producto duplicado en categoría.
  * `PRD-004` (409): Producto con órdenes activas (no eliminable).
* **Users.API:**
  * `USR-001` (409): Email ya registrado.
  * `USR-002` (400): Datos de registro inválidos.
  * `USR-003` (401): Credenciales incorrectas.
  * `USR-004` (403): Usuario bloqueado por superar límite de intentos fallidos.
  * `USR-005` (403): Usuario bloqueado por seguridad/fraude.
* **Orders.API:**
  * `ORD-001` (404): Orden no encontrada.
  * `ORD-002` (400): Datos de orden inválidos.
  * `ORD-003` (404): Usuario no encontrado al crear la orden.
  * `ORD-004` (404): Producto no encontrado al crear la orden.
  * `ORD-005` (422): Stock insuficiente para procesar uno o más ítems.
  * `ORD-006` (409): Transición de estado de orden inválida.
* **Cart.API:**
  * `CRT-001` (404): Carrito no encontrado.
  * `CRT-002` (404): Producto no encontrado al agregar al carrito.
  * `CRT-003` (422): Stock insuficiente para agregar al carrito.
  * `CRT-004` (400): Cantidad inválida (menor o igual a cero).
* **Notifications.API:**
  * `NTF-001` (404): Usuario no encontrado al enviar notificación.
  * `NTF-002` (400): Datos de la notificación inválidos (formato o tipo incorrecto).
  * `NTF-003` (404): Notificaciones no encontradas para el usuario.
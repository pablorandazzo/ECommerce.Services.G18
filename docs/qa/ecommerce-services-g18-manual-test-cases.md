# Casos de prueba manuales - ECommerce.Services.G18

Este documento reúne casos de prueba manuales basados en los endpoints y reglas de negocio implementados en el repositorio.

## Supuestos generales

- Solución compilada con `.NET 8`.
- Microservicios levantados localmente según `README.md`.
- Puertos esperados:
  - `Products.API`: `http://localhost:5001`
  - `Users.API`: `http://localhost:5002`
  - `Orders.API`: `http://localhost:5003`
  - `Cart.API`: `http://localhost:5004`
  - `Notifications.API`: `http://localhost:5005`
- Las bases SQLite se crean automáticamente al iniciar cada servicio.
- `Resultado obtenido` queda inicializado como `Pendiente de ejecución` para completarlo durante la prueba.

---

## Usuarios

### TC-USR-001 - Registrar usuario correctamente
- **Identificador:** TC-USR-001
- **Requisito/Escenario asociado:** Alta de usuario válida en `POST /api/users/register`
- **Precondiciones:** `Users.API` en ejecución.
- **Datos de entrada:**
  ```json
  {
    "nombre": "Ana",
    "apellido": "Lopez",
    "email": "ana.lopez@test.com",
    "password": "Clave123"
  }
  ```
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5002/api/users/register`.
  2. Verificar el código HTTP.
  3. Guardar el `id` retornado para pruebas posteriores.
- **Resultado esperado:** Respuesta `201 Created` con los datos del usuario creado y sin exponer la contraseña.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-USR-002 - Rechazar registro inválido
- **Identificador:** TC-USR-002
- **Requisito/Escenario asociado:** Validación de registro de usuario
- **Precondiciones:** `Users.API` en ejecución.
- **Datos de entrada:**
  ```json
  {
    "nombre": "",
    "apellido": "Perez",
    "email": "correo-invalido",
    "password": "123"
  }
  ```
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5002/api/users/register` con el payload inválido.
  2. Revisar el cuerpo de error.
- **Resultado esperado:** Respuesta `400` con `errorCode` `USR-002` y mensaje indicando campos inválidos.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-USR-003 - Rechazar email duplicado
- **Identificador:** TC-USR-003
- **Requisito/Escenario asociado:** Regla de unicidad de email
- **Precondiciones:** Existencia previa de un usuario con email `ana.lopez@test.com`.
- **Datos de entrada:** Mismo payload usado en `TC-USR-001`.
- **Pasos:**
  1. Reenviar `POST` a `http://localhost:5002/api/users/register` con el mismo email.
  2. Revisar el código y cuerpo de respuesta.
- **Resultado esperado:** Respuesta `409` con `errorCode` `USR-001`.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-USR-004 - Bloquear cuenta tras tres intentos fallidos
- **Identificador:** TC-USR-004
- **Requisito/Escenario asociado:** Bloqueo por intentos fallidos en `POST /api/users/login`
- **Precondiciones:** Usuario activo existente con email válido.
- **Datos de entrada:**
  ```json
  {
    "email": "ana.lopez@test.com",
    "password": "PasswordIncorrecta"
  }
  ```
- **Pasos:**
  1. Enviar tres veces consecutivas `POST` a `http://localhost:5002/api/users/login` con contraseña inválida.
  2. Observar cada respuesta.
- **Resultado esperado:** Primer y segundo intento retornan `401` con `USR-003`; el tercer intento retorna `403` con `USR-004`.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-USR-005 - Login exitoso
- **Identificador:** TC-USR-005
- **Requisito/Escenario asociado:** Autenticación válida de usuario
- **Precondiciones:** Usuario activo existente con contraseña correcta.
- **Datos de entrada:**
  ```json
  {
    "email": "ana.lopez@test.com",
    "password": "Clave123"
  }
  ```
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5002/api/users/login`.
  2. Validar la respuesta.
- **Resultado esperado:** Respuesta `200 OK` con `id`, `nombre`, `apellido` y `email` del usuario.
- **Resultado obtenido:** Pendiente de ejecución.

---

## Productos

### TC-PRD-001 - Crear producto correctamente
- **Identificador:** TC-PRD-001
- **Requisito/Escenario asociado:** Alta válida de producto en `POST /api/products`
- **Precondiciones:** `Products.API` en ejecución.
- **Datos de entrada:**
  ```json
  {
    "nombre": "Mouse Gamer",
    "descripcion": "Mouse RGB",
    "precio": 15000,
    "stock": 10,
    "categoria": "Perifericos"
  }
  ```
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5001/api/products`.
  2. Guardar el `id` generado.
- **Resultado esperado:** Respuesta `201 Created` con el producto persistido.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-PRD-002 - Rechazar producto inválido
- **Identificador:** TC-PRD-002
- **Requisito/Escenario asociado:** Validaciones de nombre, precio, stock y categoría
- **Precondiciones:** `Products.API` en ejecución.
- **Datos de entrada:**
  ```json
  {
    "nombre": "",
    "descripcion": "x",
    "precio": 0,
    "stock": -1,
    "categoria": ""
  }
  ```
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5001/api/products` con datos inválidos.
  2. Revisar la respuesta.
- **Resultado esperado:** Respuesta `400` con `errorCode` `PRD-002`.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-PRD-003 - Rechazar producto duplicado
- **Identificador:** TC-PRD-003
- **Requisito/Escenario asociado:** Regla de unicidad nombre + categoría
- **Precondiciones:** Existencia previa del producto `Mouse Gamer` en categoría `Perifericos`.
- **Datos de entrada:** Mismo payload de `TC-PRD-001`.
- **Pasos:**
  1. Repetir el `POST` de creación.
  2. Revisar código y mensaje.
- **Resultado esperado:** Respuesta `409` con `errorCode` `PRD-003`.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-PRD-004 - Filtrar productos por categoría y nombre
- **Identificador:** TC-PRD-004
- **Requisito/Escenario asociado:** Consulta filtrada en `GET /api/products`
- **Precondiciones:** Existencia de productos en diferentes categorías y nombres.
- **Datos de entrada:** Query string `?categoria=Perifericos&nombre=Mouse`.
- **Pasos:**
  1. Enviar `GET` a `http://localhost:5001/api/products?categoria=Perifericos&nombre=Mouse`.
  2. Verificar el contenido de la lista.
- **Resultado esperado:** Respuesta `200` conteniendo solo productos que cumplan el filtro.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-PRD-005 - Impedir borrado de producto con órdenes activas
- **Identificador:** TC-PRD-005
- **Requisito/Escenario asociado:** Validación inter-servicio con `Orders.API`
- **Precondiciones:**
  - Producto existente.
  - Orden asociada al producto en estado `Pendiente` o `Confirmada`.
  - `Products.API` y `Orders.API` en ejecución.
- **Datos de entrada:** `id` de un producto con órdenes activas.
- **Pasos:**
  1. Enviar `DELETE` a `http://localhost:5001/api/products/{id}`.
  2. Revisar la respuesta.
- **Resultado esperado:** Respuesta `409` con `errorCode` `PRD-004`; el producto no debe eliminarse.
- **Resultado obtenido:** Pendiente de ejecución.

---

## Carrito

### TC-CRT-001 - Crear carrito automáticamente al agregar primer ítem
- **Identificador:** TC-CRT-001
- **Requisito/Escenario asociado:** Alta implícita de carrito en `POST /api/cart/{userId}/items`
- **Precondiciones:**
  - `Cart.API` y `Products.API` en ejecución.
  - Producto existente con stock suficiente.
  - `userId` sin carrito previo.
- **Datos de entrada:**
  ```json
  {
    "productoId": "{productId}",
    "cantidad": 2
  }
  ```
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5004/api/cart/{userId}/items`.
  2. Consultar luego `GET http://localhost:5004/api/cart/{userId}`.
- **Resultado esperado:** El `POST` responde exitosamente y el `GET` posterior retorna un carrito con el ítem agregado.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-CRT-002 - Rechazar cantidad inválida en carrito
- **Identificador:** TC-CRT-002
- **Requisito/Escenario asociado:** Validación de cantidad en carrito
- **Precondiciones:** `Cart.API` en ejecución.
- **Datos de entrada:**
  ```json
  {
    "productoId": "{productId}",
    "cantidad": 0
  }
  ```
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5004/api/cart/{userId}/items` con cantidad `0`.
  2. Revisar la respuesta.
- **Resultado esperado:** Respuesta `400` con `errorCode` `CRT-004`.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-CRT-003 - Rechazar agregado por stock insuficiente acumulado
- **Identificador:** TC-CRT-003
- **Requisito/Escenario asociado:** Validación de stock considerando cantidad ya agregada
- **Precondiciones:**
  - Producto existente con stock `3`.
  - Carrito del usuario ya contiene `2` unidades de ese producto.
- **Datos de entrada:**
  ```json
  {
    "productoId": "{productId}",
    "cantidad": 2
  }
  ```
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5004/api/cart/{userId}/items`.
  2. Revisar la respuesta.
- **Resultado esperado:** Respuesta `422` con `errorCode` `CRT-003` porque el acumulado supera el stock.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-CRT-004 - Eliminar ítem existente del carrito
- **Identificador:** TC-CRT-004
- **Requisito/Escenario asociado:** Baja de ítem en `DELETE /api/cart/{userId}/items/{productId}`
- **Precondiciones:** Carrito existente con el producto cargado.
- **Datos de entrada:** `userId` y `productId` válidos.
- **Pasos:**
  1. Enviar `DELETE` a `http://localhost:5004/api/cart/{userId}/items/{productId}`.
  2. Consultar luego el carrito.
- **Resultado esperado:** Respuesta exitosa y el producto ya no aparece en el carrito.
- **Resultado obtenido:** Pendiente de ejecución.

---

## Órdenes

### TC-ORD-001 - Crear orden correctamente
- **Identificador:** TC-ORD-001
- **Requisito/Escenario asociado:** Orquestación exitosa en `POST /api/orders`
- **Precondiciones:**
  - Usuario existente.
  - Producto(s) existente(s) con stock suficiente.
  - `Users.API`, `Products.API`, `Orders.API` y `Notifications.API` en ejecución.
- **Datos de entrada:**
  ```json
  {
    "usuarioId": "{userId}",
    "items": [
      {
        "productoId": "{productId}",
        "cantidad": 1
      }
    ]
  }
  ```
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5003/api/orders`.
  2. Guardar el `id` de la orden.
  3. Consultar luego la orden creada.
- **Resultado esperado:** Respuesta `201 Created`; la orden queda en estado `Confirmada` y el total se calcula con el precio actual del producto.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-ORD-002 - Rechazar orden con usuario inexistente
- **Identificador:** TC-ORD-002
- **Requisito/Escenario asociado:** Validación remota de usuario
- **Precondiciones:** `Orders.API` y `Users.API` en ejecución.
- **Datos de entrada:** `usuarioId` inexistente y producto válido.
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5003/api/orders` con un `usuarioId` inexistente.
  2. Revisar la respuesta.
- **Resultado esperado:** Respuesta `404` con `errorCode` `ORD-003`.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-ORD-003 - Rechazar orden por producto inexistente
- **Identificador:** TC-ORD-003
- **Requisito/Escenario asociado:** Validación remota de producto
- **Precondiciones:** `Orders.API` y `Products.API` en ejecución.
- **Datos de entrada:** `productoId` inexistente y usuario válido.
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5003/api/orders` con un producto inexistente.
  2. Revisar la respuesta.
- **Resultado esperado:** Respuesta `404` con `errorCode` `ORD-004`.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-ORD-004 - Rechazar orden por stock insuficiente
- **Identificador:** TC-ORD-004
- **Requisito/Escenario asociado:** Validación de stock al crear orden
- **Precondiciones:** Producto existente con stock menor a la cantidad solicitada.
- **Datos de entrada:** Payload de orden con cantidad superior al stock disponible.
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5003/api/orders`.
  2. Revisar el cuerpo de error.
- **Resultado esperado:** Respuesta `422` con `errorCode` `ORD-005`.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-ORD-005 - Verificar descuento de stock luego de crear orden
- **Identificador:** TC-ORD-005
- **Requisito/Escenario asociado:** Consistencia entre `Orders.API` y `Products.API`
- **Precondiciones:** Producto con stock conocido, por ejemplo `10` unidades.
- **Datos de entrada:** Orden por `2` unidades de ese producto.
- **Pasos:**
  1. Consultar el producto antes de crear la orden.
  2. Crear la orden desde `Orders.API`.
  3. Volver a consultar el producto en `Products.API`.
- **Resultado esperado:** El stock final del producto disminuye exactamente en la cantidad ordenada.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-ORD-006 - Validar transición de estado permitida
- **Identificador:** TC-ORD-006
- **Requisito/Escenario asociado:** Cambio válido de estado en `PUT /api/orders/{id}/status`
- **Precondiciones:** Orden existente en estado `Confirmada`.
- **Datos de entrada:**
  ```json
  {
    "nuevoEstado": "Enviada"
  }
  ```
- **Pasos:**
  1. Enviar `PUT` a `http://localhost:5003/api/orders/{id}/status`.
  2. Consultar la orden actualizada.
- **Resultado esperado:** Respuesta `200 OK` y orden con estado `Enviada`.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-ORD-007 - Rechazar transición de estado inválida
- **Identificador:** TC-ORD-007
- **Requisito/Escenario asociado:** Regla de negocio de transición de estados
- **Precondiciones:** Orden existente en estado `Pendiente`.
- **Datos de entrada:**
  ```json
  {
    "nuevoEstado": "Entregada"
  }
  ```
- **Pasos:**
  1. Enviar `PUT` a `http://localhost:5003/api/orders/{id}/status`.
  2. Revisar la respuesta.
- **Resultado esperado:** Respuesta `409` con `errorCode` `ORD-006`.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-ORD-008 - La falla de notificación no debe anular la orden
- **Identificador:** TC-ORD-008
- **Requisito/Escenario asociado:** Tolerancia a fallo de `Notifications.API`
- **Precondiciones:**
  - `Users.API`, `Products.API` y `Orders.API` en ejecución.
  - `Notifications.API` detenido o inaccesible.
  - Usuario y producto válidos.
- **Datos de entrada:** Payload válido de creación de orden.
- **Pasos:**
  1. Crear una orden desde `Orders.API` con `Notifications.API` fuera de servicio.
  2. Consultar la orden creada.
- **Resultado esperado:** La creación de la orden sigue siendo exitosa; la orden queda confirmada aunque la notificación falle.
- **Resultado obtenido:** Pendiente de ejecución.

---

## Notificaciones

### TC-NTF-001 - Enviar notificación correctamente
- **Identificador:** TC-NTF-001
- **Requisito/Escenario asociado:** Alta de notificación válida en `POST /api/notifications/send`
- **Precondiciones:**
  - Usuario existente.
  - `Notifications.API` y `Users.API` en ejecución.
- **Datos de entrada:**
  ```json
  {
    "usuarioId": "{userId}",
    "mensaje": "Su orden fue confirmada.",
    "tipo": "Email"
  }
  ```
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5005/api/notifications/send`.
  2. Consultar luego `GET /api/notifications/{userId}`.
- **Resultado esperado:** Respuesta exitosa y la notificación aparece listada para el usuario.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-NTF-002 - Rechazar notificación con tipo inválido
- **Identificador:** TC-NTF-002
- **Requisito/Escenario asociado:** Validación de tipo y mensaje en notificaciones
- **Precondiciones:** `Notifications.API` en ejecución.
- **Datos de entrada:**
  ```json
  {
    "usuarioId": "{userId}",
    "mensaje": "Mensaje de prueba",
    "tipo": "FAX"
  }
  ```
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5005/api/notifications/send`.
  2. Revisar la respuesta.
- **Resultado esperado:** Respuesta `400` con `errorCode` `NTF-002`.
- **Resultado obtenido:** Pendiente de ejecución.

---

## Observabilidad e integración

### TC-OBS-001 - Generar y devolver Correlation ID
- **Identificador:** TC-OBS-001
- **Requisito/Escenario asociado:** Trazabilidad mediante `X-Correlation-Id`
- **Precondiciones:** Cualquier API en ejecución.
- **Datos de entrada:** Request sin header `X-Correlation-Id`.
- **Pasos:**
  1. Enviar una solicitud `GET` a cualquier endpoint, por ejemplo `http://localhost:5001/api/products`.
  2. Revisar los headers de la respuesta.
- **Resultado esperado:** La respuesta devuelve un header `X-Correlation-Id` generado automáticamente.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-OBS-002 - Propagar Correlation ID enviado por el cliente
- **Identificador:** TC-OBS-002
- **Requisito/Escenario asociado:** Preservación de trazabilidad en llamadas inter-servicio
- **Precondiciones:** `Orders.API`, `Users.API` y `Products.API` en ejecución.
- **Datos de entrada:** Request a creación de orden con header `X-Correlation-Id: QA-TRACE-001`.
- **Pasos:**
  1. Enviar `POST` a `http://localhost:5003/api/orders` con el header indicado.
  2. Revisar la respuesta y logs relacionados.
- **Resultado esperado:** La respuesta conserva el mismo `X-Correlation-Id` y los logs asociados usan ese identificador.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-OBS-003 - Auditar operaciones de escritura
- **Identificador:** TC-OBS-003
- **Requisito/Escenario asociado:** Auditoría de `POST`, `PUT` y `DELETE`
- **Precondiciones:** Servicio con logging habilitado y acceso al archivo/log de auditoría.
- **Datos de entrada:** Ejecutar una operación `POST` o `PUT` válida.
- **Pasos:**
  1. Ejecutar una operación de escritura, por ejemplo crear un producto.
  2. Revisar los logs del servicio.
- **Resultado esperado:** Existe una entrada de auditoría con método, path, status code, request body y response body.
- **Resultado obtenido:** Pendiente de ejecución.

### TC-OBS-004 - Verificar estado saludable del servicio
- **Identificador:** TC-OBS-004
- **Requisito/Escenario asociado:** Health checks en `/health`
- **Precondiciones:** Servicio objetivo en ejecución con su base accesible.
- **Datos de entrada:** Request `GET /health`.
- **Pasos:**
  1. Enviar `GET` a `http://localhost:5001/health`.
  2. Revisar el JSON retornado.
- **Resultado esperado:** Respuesta `200` con información de runtime, uptime y estado de conectividad de persistencia.
- **Resultado obtenido:** Pendiente de ejecución.

---

## Cobertura resumida

| Área | Casos incluidos |
| --- | --- |
| Usuarios | 5 |
| Productos | 5 |
| Carrito | 4 |
| Órdenes | 8 |
| Notificaciones | 2 |
| Observabilidad / integración | 4 |
| **Total** | **28** |

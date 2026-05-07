namespace Orders.API.Constants;

public static class OrderErrors
{
    // ORD-001: GET/PUT cuando el ID de orden no existe.
    public const string NotFoundCode = "ORD-001";
    public const string NotFoundMessage = "Orden no encontrada.";

    // ORD-002: POST con campos faltantes o lista de items vacía.
    public const string InvalidDataCode = "ORD-002";
    public const string InvalidDataMessage = "Los datos de la orden son inválidos.";

    // ORD-003: POST cuando el UsuarioId no existe en Users API.
    public const string UserNotFoundCode = "ORD-003";
    public const string UserNotFoundMessage = "Usuario no encontrado al crear la orden.";

    // ORD-004: POST cuando algún ProductoId no existe en Products API.
    public const string ProductNotFoundCode = "ORD-004";
    public const string ProductNotFoundMessage = "Producto no encontrado al crear la orden.";

    // ORD-005: POST cuando la cantidad solicitada supera el stock disponible.
    public const string InsufficientStockCode = "ORD-005";
    public const string InsufficientStockMessage = "Stock insuficiente para uno o más productos.";

    // ORD-006: PUT /status cuando la transición de estado no es válida.
    public const string InvalidStatusTransitionCode = "ORD-006";
    public const string InvalidStatusTransitionMessage = "El estado de la orden no puede ser modificado.";

    // ORD-007: Error inesperado en servicio o persistencia.
    public const string InternalErrorCode = "ORD-007";
    public const string InternalErrorMessage = "Error interno al procesar la orden.";
}

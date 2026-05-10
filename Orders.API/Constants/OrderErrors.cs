namespace Orders.API.Constants;

public static class OrderErrors
{
    // ORD-001: 404 Not Found[cite: 1]
    public static readonly (string Code, string Message, int Status) NotFound =
        ("ORD-001", "Orden no encontrada.", 404);

    // ORD-002: 400 Bad Request[cite: 1]
    public static readonly (string Code, string Message, int Status) InvalidData =
        ("ORD-002", "Los datos de la orden son inválidos.", 400);

    // ORD-003: 404 Not Found (Cuando el usuario no existe en la otra API)[cite: 1]
    public static readonly (string Code, string Message, int Status) UserNotFound =
        ("ORD-003", "Usuario no encontrado al crear la orden.", 404);

    // ORD-004: 404 Not Found (Cuando el producto no existe en la otra API)[cite: 1]
    public static readonly (string Code, string Message, int Status) ProductNotFound =
        ("ORD-004", "Producto no encontrado al crear la orden.", 404);

    // ORD-005: 422 Unprocessable Entity (Stock insuficiente)[cite: 1]
    public static readonly (string Code, string Message, int Status) InsufficientStock =
        ("ORD-005", "Stock insuficiente para uno o más productos.", 422);

    // ORD-006: 409 Conflict (Transición de estado inválida)[cite: 1]
    public static readonly (string Code, string Message, int Status) InvalidStatusTransition =
        ("ORD-006", "El estado de la orden no puede ser modificado.", 409);

    // ORD-007: 500 Internal Server Error[cite: 1]
    public static readonly (string Code, string Message, int Status) InternalError =
        ("ORD-007", "Error interno al procesar la orden.", 500);
}
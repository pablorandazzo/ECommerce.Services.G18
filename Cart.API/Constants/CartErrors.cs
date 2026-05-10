namespace Cart.API.Constants;

public static class CartErrors
{
    // CRT-001: 404 Not Found[cite: 1]
    public static readonly (string Code, string Message, int Status) NotFound =
        ("CRT-001", "Carrito no encontrado.", 404);

    // CRT-002: 404 Not Found (Producto inexistente)[cite: 1]
    public static readonly (string Code, string Message, int Status) ProductNotFound =
        ("CRT-002", "Producto no encontrado.", 404);

    // CRT-003: 422 Unprocessable Entity (Stock)[cite: 1]
    public static readonly (string Code, string Message, int Status) InsufficientStock =
        ("CRT-003", "Stock insuficiente para agregar al carrito.", 422);

    // CRT-004: 400 Bad Request[cite: 1]
    public static readonly (string Code, string Message, int Status) InvalidQuantity =
        ("CRT-004", "Cantidad inválida.", 400);

    // CRT-005: 500 Internal Error[cite: 1]
    public static readonly (string Code, string Message, int Status) InternalError =
        ("CRT-005", "Error interno al procesar el carrito.", 500);
}
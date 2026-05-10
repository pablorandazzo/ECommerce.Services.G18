namespace Products.API.Constants;

public static class ProductErrors
{
    // PRD-001: GET/PUT/DELETE cuando el ID no existe.
    public const string NotFoundCode = "PRD-001";
    public const string NotFoundMessage = "Producto no encontrado.";

    // PRD-002: POST/PUT con campos faltantes o formato incorrecto.
    public const string InvalidDataCode = "PRD-002";
    public const string InvalidDataMessage = "Los datos del producto son inválidos.";

    // PRD-003: POST cuando se intenta crear un duplicado.
    public const string DuplicateNameCode = "PRD-003";
    public const string DuplicateNameMessage = "Ya existe un producto con ese nombre en la categoría.";

    // PRD-004: DELETE cuando hay órdenes Pendiente o Confirmada que lo referencian.
    public const string ActiveOrdersCode = "PRD-004";
    public const string ActiveOrdersMessage = "El producto tiene órdenes activas y no puede eliminarse.";

    // PRD-005: Error inesperado en servicio o persistencia.
    public const string InternalErrorCode = "PRD-005";
    public const string InternalErrorMessage = "Error interno al procesar el producto.";
}

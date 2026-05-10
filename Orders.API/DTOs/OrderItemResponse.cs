namespace Orders.API.DTOs;

public record OrderItemResponse(
    Guid ProductoId,
    int Cantidad,
    decimal PrecioUnitario
);

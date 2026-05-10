namespace Orders.API.DTOs;

public record OrderResponse(
    Guid Id,
    Guid UsuarioId,
    List<OrderItemResponse> Items,
    decimal Total,
    string Estado,
    DateTime FechaCreacion
);

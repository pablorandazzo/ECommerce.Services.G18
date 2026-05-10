namespace Orders.API.DTOs;

public record CreateOrderRequest(
    Guid UsuarioId,
    List<CreateOrderItemRequest> Items
);

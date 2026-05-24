namespace Orders.API.DTOs;
public record CreateOrderItemRequest(Guid ProductoId, int Cantidad);
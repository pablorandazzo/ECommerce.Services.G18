namespace Cart.API.DTOs
{
    public record CartResponse(
        Guid UsuarioId,
        List<CartItemResponse> Items,
        DateTime FechaActualizacion
    ); //[cite: 1]

    /// <summary>
    /// DTO anidado para representar cada producto dentro de la respuesta del carrito.
    /// </summary>
    public record CartItemResponse(
        Guid ProductoId,
        int Cantidad
    ); //[cite: 1]
}
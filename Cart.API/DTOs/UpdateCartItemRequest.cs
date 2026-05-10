namespace Cart.API.DTOs
{
    /// <summary>
    /// Contrato para actualizar la cantidad de un ítem existente.
    /// </summary>
    public record UpdateCartItemRequest(
        int Cantidad
    ); //
}
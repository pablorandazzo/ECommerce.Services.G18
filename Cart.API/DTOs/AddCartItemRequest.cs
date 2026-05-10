namespace Cart.API.DTOs
{
	/// <summary>
	/// Contrato para agregar un producto al carrito.
	/// </summary>
	public record AddCartItemRequest(
		Guid ProductoId,
		int Cantidad
	); //
}
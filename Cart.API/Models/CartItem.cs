namespace Cart.API.Models
{
    public class CartItem
    {
        public Guid ProductoId { get; set; } // Referencia al producto
        public int Cantidad { get; set; } // Requerido, mayor a 0
    }
}
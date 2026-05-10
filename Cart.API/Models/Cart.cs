using System.Collections.Generic;

namespace Cart.API.Models
{
    public class Cart
    {
        public Guid UsuarioId { get; set; } // Identificador del usuario dueño

        /// <summary>
        /// Lista de productos en el carrito.
        /// La inicializamos como una lista vacía para que nunca sea null.
        /// </summary>
        public List<CartItem> Items { get; set; } = new List<CartItem>(); //

        public DateTime FechaActualizacion { get; set; } // Se actualiza en cada operación
    }
}
namespace Orders.API.Models;

public class Order
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }

    // Inicializamos la lista para que nunca sea nula
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();

    public decimal Total { get; set; }
    public string Estado { get; set; } = "Pendiente"; // Valor inicial por defecto
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

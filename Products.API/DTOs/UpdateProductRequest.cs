namespace Products.API.DTOs
{
    public record UpdateProductRequest(
        string Nombre,
        string? Descripcion,
        decimal Precio,
        int Stock,
        string Categoria
    );
}

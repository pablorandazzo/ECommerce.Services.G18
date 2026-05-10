namespace Products.API.DTOs;
public record CreateProductRequest(
    string Nombre,
    string? Descripcion,
    decimal Precio,
    int Stock,
    string Categoria
);
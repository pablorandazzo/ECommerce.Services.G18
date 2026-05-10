namespace Products.API.DTOs;
public record ProductResponse(
    Guid Id,
    string Nombre,
    string? Descripcion,
    decimal Precio,
    int Stock,
    string Categoria,
    DateTime FechaCreacion
);
namespace Users.API.DTOs
{
    public record UserResponse(
        Guid Id,
        string Nombre,
        string Apellido,
        string Email,
        DateTime FechaRegistro,
        bool Activo
    ); //
}
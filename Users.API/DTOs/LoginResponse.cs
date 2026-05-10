namespace Users.API.DTOs
{
    public record LoginResponse(
        Guid Id,
        string Nombre,
        string Apellido,
        string Email
    ); //
}
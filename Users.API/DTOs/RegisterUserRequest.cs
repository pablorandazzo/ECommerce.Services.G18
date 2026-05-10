namespace Users.API.DTOs
{
    public record RegisterUserRequest(
        string Nombre,
        string Apellido,
        string Email,
        string Password
    ); //
}
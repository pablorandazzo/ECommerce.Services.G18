namespace Notifications.API.DTOs
{
    /// <summary>
    /// Contrato para solicitar el envío de una notificación.
    /// </summary>
    public record SendNotificationRequest(
        Guid UsuarioId,
        string Mensaje,
        string Tipo // Email | Push | SMS
    ); //
}
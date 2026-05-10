namespace Notifications.API.DTOs
{
    /// <summary>
    /// Contrato de respuesta con el detalle de la notificación.
    /// </summary>
    public record NotificationResponse(
        Guid Id,
        Guid UsuarioId,
        string Mensaje,
        string Tipo,
        string Estado, // Pendiente | Enviada | Fallida
        DateTime FechaEnvio
    ); //
}
namespace Notifications.API.Constants;

public static class NotificationErrors
{
    // NTF-001: 404 Not Found[cite: 1]
    public static readonly (string Code, string Message, int Status) UserNotFound =
        ("NTF-001", "Usuario no encontrado.", 404);

    // NTF-002: 400 Bad Request[cite: 1]
    public static readonly (string Code, string Message, int Status) InvalidData =
        ("NTF-002", "Los datos de la notificación son inválidos.", 400);

    // NTF-003: 404 Not Found[cite: 1]
    public static readonly (string Code, string Message, int Status) NoNotificationsFound =
        ("NTF-003", "No se encontraron notificaciones para el usuario.", 404);

    // NTF-004: 500 Internal Error[cite: 1]
    public static readonly (string Code, string Message, int Status) InternalError =
        ("NTF-004", "Error interno al procesar la notificación.", 500);
}
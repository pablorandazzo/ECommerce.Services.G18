namespace Users.API.Constants;

public static class UserErrors
{
    // USR-001: 409 Conflict
    public static readonly (string Code, string Message, int Status) DuplicateEmail =
        ("USR-001", "El email ya está registrado.", 409);

    // USR-002: 400 Bad Request
    public static readonly (string Code, string Message, int Status) InvalidData =
        ("USR-002", "Los datos del usuario son inválidos.", 400);

    // USR-003: 401 Unauthorized
    public static readonly (string Code, string Message, int Status) InvalidCredentials =
        ("USR-003", "Credenciales incorrectas.", 401);

    // USR-004: 403 Forbidden (Bloqueo por intentos)
    public static readonly (string Code, string Message, int Status) AccountBlockedAttempts =
        ("USR-004", "Su cuenta fue bloqueada por superar el máximo de intentos fallidos.", 403);

    // USR-005: 403 Forbidden (Fraude)
    public static readonly (string Code, string Message, int Status) AccountBlockedFraud =
        ("USR-005", "Su cuenta fue suspendida por razones de seguridad.", 403);

    // USR-006: 500 Internal Error[cite: 1]
    public static readonly (string Code, string Message, int Status) InternalError =
        ("USR-006", "Error interno al procesar el usuario.", 500);
}
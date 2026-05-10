namespace Notifications.API.Models
{
    public class Notification
    {
        public Guid Id { get; set; } // Identificador único

        public Guid UsuarioId { get; set; } // Usuario destinatario

        public string Mensaje { get; set; } = string.Empty; // Requerido, máx. 500 caracteres

        /// <summary>
        /// Valores permitidos: Email | Push | SMS
        /// </summary>
        public string Tipo { get; set; } = string.Empty;

        /// <summary>
        /// Valores permitidos: Pendiente | Enviada | Fallida
        /// </summary>
        public string Estado { get; set; } = string.Empty;

        public DateTime FechaEnvio { get; set; } // Asignado automáticamente al registrar
    }
}
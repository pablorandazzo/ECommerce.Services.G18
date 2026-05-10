using System.Text.Json.Serialization;

namespace Users.API.Models
{
    public class User
    {
        public Guid Id { get; set; } // Identificador único

        public string Nombre { get; set; } = string.Empty; // Requerido

        public string Apellido { get; set; } = string.Empty; // Requerido

        public string Email { get; set; } = string.Empty; // Único y formato válido

        /// <summary>
        /// Hash de la contraseña. 
        /// Usamos [JsonIgnore] para cumplir el criterio de que no se exponga en respuestas.
        /// </summary>
        
        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty; //

        public DateTime FechaRegistro { get; set; } // Asignado automáticamente al registrar

        public bool Activo { get; set; } = true; // false cuando el usuario está bloqueado[cite: 1, 2]

        public int IntentosFallidos { get; set; } = 0; // Se incrementa en cada login fallido[cite: 1, 2]
    }
}
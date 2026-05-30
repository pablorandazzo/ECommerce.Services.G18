using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Notifications.API.Data
{
    public class DatabaseInitializer
    {
        private readonly IConfiguration _config;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(IConfiguration config, ILogger<DatabaseInitializer> logger)
        {
            _config = config;
            _logger = logger;
        }

        public void Initialize()
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Data Source=notifications.db";
            }

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // Creamos la tabla de notificaciones si no existe
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS notifications (
                        Id             TEXT PRIMARY KEY,
                        UsuarioId      TEXT NOT NULL,
                        Mensaje        TEXT NOT NULL,
                        Tipo           TEXT NOT NULL,
                        Estado         TEXT NOT NULL,
                        FechaEnvio     TEXT NOT NULL
                    );
                ");
            }

            _logger.LogInformation("Base de datos de Notificaciones inicializada correctamente.");
        }
    }
}

using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Users.API.Data
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
            string? connectionString = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Data Source=users.db";
            }

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // Creamos la tabla de usuarios si no existe
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS users (
                        Id               TEXT    PRIMARY KEY,
                        Nombre           TEXT    NOT NULL,
                        Apellido         TEXT    NOT NULL,
                        Email            TEXT    NOT NULL UNIQUE,
                        PasswordHash     TEXT    NOT NULL,
                        FechaRegistro    TEXT    NOT NULL,
                        Activo           INTEGER NOT NULL DEFAULT 1,
                        IntentosFallidos INTEGER NOT NULL DEFAULT 0
                    );
                ");
            }

            _logger.LogInformation("Base de datos de Usuarios inicializada correctamente.");
        }
    }
}

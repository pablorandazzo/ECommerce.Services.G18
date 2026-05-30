using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Products.API.Data
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
                connectionString = "Data Source=products.db";
            }

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // Creamos la tabla de productos si no existe
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS products (
                        Id             TEXT    PRIMARY KEY,
                        Nombre         TEXT    NOT NULL,
                        Descripcion    TEXT,
                        Precio         REAL    NOT NULL,
                        Stock          INTEGER NOT NULL,
                        Categoria      TEXT    NOT NULL,
                        FechaCreacion  TEXT    NOT NULL
                    );
                ");
            }

            _logger.LogInformation("Base de datos de Productos inicializada correctamente.");
        }
    }
}

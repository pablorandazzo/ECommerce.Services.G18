using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cart.API.Data
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
                connectionString = "Data Source=carts.db";
            }

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // Creamos la tabla de carritos si no existe
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS carts (
                        UsuarioId          TEXT PRIMARY KEY,
                        FechaActualizacion TEXT NOT NULL
                    );
                ");

                // Creamos la tabla de ítems del carrito si no existe
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS cart_items (
                        UsuarioId          TEXT NOT NULL,
                        ProductoId         TEXT NOT NULL,
                        Cantidad           INTEGER NOT NULL,
                        PRIMARY KEY (UsuarioId, ProductoId)
                    );
                ");
            }

            _logger.LogInformation("Base de datos de Carrito inicializada correctamente.");
        }
    }
}

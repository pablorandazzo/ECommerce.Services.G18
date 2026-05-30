using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Orders.API.Data
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
                connectionString = "Data Source=orders.db";
            }

            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();

                // Creamos la tabla de órdenes si no existe
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS orders (
                        Id             TEXT PRIMARY KEY,
                        UsuarioId      TEXT NOT NULL,
                        Total          REAL NOT NULL,
                        Estado         TEXT NOT NULL,
                        FechaCreacion  TEXT NOT NULL
                    );
                ");

                // Creamos la tabla de ítems de órdenes si no existe
                connection.Execute(@"
                    CREATE TABLE IF NOT EXISTS order_items (
                        OrderId        TEXT NOT NULL,
                        ProductoId     TEXT NOT NULL,
                        Cantidad       INTEGER NOT NULL,
                        PrecioUnitario REAL NOT NULL,
                        PRIMARY KEY (OrderId, ProductoId)
                    );
                ");
            }

            _logger.LogInformation("Base de datos de Órdenes inicializada correctamente.");
        }
    }
}

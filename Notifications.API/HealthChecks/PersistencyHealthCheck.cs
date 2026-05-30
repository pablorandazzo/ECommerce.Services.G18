using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Notifications.API.HealthChecks
{
    public class PersistencyHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _config;

        // Constructor para recibir la configuraciÃ³n de la aplicaciÃ³n (inyectada)
        public PersistencyHealthCheck(IConfiguration config)
        {
            _config = config;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Obtenemos la cadena de conexiÃ³n del archivo appsettings.json o usamos una por defecto
                string? connectionString = _config.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    connectionString = "Data Source=notifications.db";
                }

                // Abrimos una conexiÃ³n a la base de datos SQLite para probar la disponibilidad
                using (SqliteConnection connection = new SqliteConnection(connectionString))
                {
                    await connection.OpenAsync(cancellationToken);

                    // Ejecutamos una consulta ultra rÃ¡pida para verificar que responde
                    await connection.ExecuteScalarAsync<int>("SELECT 1");
                }

                // Si todo sale bien, devolvemos un estado Saludable
                return HealthCheckResult.Healthy("Base de datos SQLite operativa y respondiendo correctamente.");
            }
            catch (Exception ex)
            {
                // Si ocurre algÃºn error (no se puede abrir el archivo, etc.), devolvemos un estado No Saludable
                return HealthCheckResult.Unhealthy("No se pudo conectar a la base de datos SQLite.", ex);
            }
        }
    }
}


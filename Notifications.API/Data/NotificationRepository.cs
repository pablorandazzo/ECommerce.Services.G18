using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Notifications.API.Models;

namespace Notifications.API.Data
{
    public class NotificationRepository
    {
        private readonly IConfiguration _config;

        public NotificationRepository(IConfiguration config)
        {
            _config = config;
        }

        private SqliteConnection CreateConnection()
        {
            string? connectionString = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Data Source=notifications.db";
            }
            return new SqliteConnection(connectionString);
        }

        // Listar las notificaciones de un usuario específico
        public async Task<IEnumerable<Notification>> ListByUserIdAsync(Guid userId)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = "SELECT Id, UsuarioId, Mensaje, Tipo, Estado, FechaEnvio FROM notifications WHERE UsuarioId = @UsuarioId ORDER BY FechaEnvio DESC";
                var rawList = await conn.QueryAsync(query, new { UsuarioId = userId.ToString() });
                
                var list = new List<Notification>();
                foreach (var raw in rawList)
                {
                    list.Add(new Notification
                    {
                        Id = Guid.Parse((string)raw.Id),
                        UsuarioId = Guid.Parse((string)raw.UsuarioId),
                        Mensaje = (string)raw.Mensaje,
                        Tipo = (string)raw.Tipo,
                        Estado = (string)raw.Estado,
                        FechaEnvio = DateTime.Parse((string)raw.FechaEnvio)
                    });
                }

                return list;
            }
        }

        // Crear una nueva notificación
        public async Task CreateAsync(Notification notification)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = @"
                    INSERT INTO notifications (Id, UsuarioId, Mensaje, Tipo, Estado, FechaEnvio)
                    VALUES (@Id, @UsuarioId, @Mensaje, @Tipo, @Estado, @FechaEnvio)
                ";

                await conn.ExecuteAsync(query, new {
                    Id = notification.Id.ToString(),
                    UsuarioId = notification.UsuarioId.ToString(),
                    notification.Mensaje,
                    notification.Tipo,
                    notification.Estado,
                    FechaEnvio = notification.FechaEnvio.ToString("o")
                });
            }
        }
    }
}

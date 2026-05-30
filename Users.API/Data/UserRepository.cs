using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Users.API.Models;

namespace Users.API.Data
{
    public class UserRepository
    {
        private readonly IConfiguration _config;

        public UserRepository(IConfiguration config)
        {
            _config = config;
        }

        private SqliteConnection CreateConnection()
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Data Source=users.db";
            }
            return new SqliteConnection(connectionString);
        }

        // Obtener usuario por ID
        public async Task<User?> GetByIdAsync(Guid id)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = @"
                    SELECT Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro, 
                           Activo, IntentosFallidos 
                    FROM users 
                    WHERE Id = @Id
                ";

                var rawUser = await conn.QueryFirstOrDefaultAsync(query, new { Id = id.ToString() });
                if (rawUser == null) return null;

                return new User
                {
                    Id = Guid.Parse((string)rawUser.Id),
                    Nombre = (string)rawUser.Nombre,
                    Apellido = (string)rawUser.Apellido,
                    Email = (string)rawUser.Email,
                    PasswordHash = (string)rawUser.PasswordHash,
                    FechaRegistro = DateTime.Parse((string)rawUser.FechaRegistro),
                    Activo = Convert.ToBoolean(rawUser.Activo),
                    IntentosFallidos = Convert.ToInt32(rawUser.IntentosFallidos)
                };
            }
        }

        // Obtener usuario por Email
        public async Task<User?> GetByEmailAsync(string email)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = @"
                    SELECT Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro, 
                           Activo, IntentosFallidos 
                    FROM users 
                    WHERE Email = @Email
                ";

                var rawUser = await conn.QueryFirstOrDefaultAsync(query, new { Email = email });
                if (rawUser == null) return null;

                return new User
                {
                    Id = Guid.Parse((string)rawUser.Id),
                    Nombre = (string)rawUser.Nombre,
                    Apellido = (string)rawUser.Apellido,
                    Email = (string)rawUser.Email,
                    PasswordHash = (string)rawUser.PasswordHash,
                    FechaRegistro = DateTime.Parse((string)rawUser.FechaRegistro),
                    Activo = Convert.ToBoolean(rawUser.Activo),
                    IntentosFallidos = Convert.ToInt32(rawUser.IntentosFallidos)
                };
            }
        }

        // Crear nuevo usuario
        public async Task CreateAsync(User user)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = @"
                    INSERT INTO users (Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro, Activo, IntentosFallidos)
                    VALUES (@Id, @Nombre, @Apellido, @Email, @PasswordHash, @FechaRegistro, @Activo, @IntentosFallidos)
                ";

                await conn.ExecuteAsync(query, new {
                    Id = user.Id.ToString(),
                    user.Nombre,
                    user.Apellido,
                    user.Email,
                    user.PasswordHash,
                    FechaRegistro = user.FechaRegistro.ToString("o"),
                    Activo = user.Activo ? 1 : 0,
                    user.IntentosFallidos
                });
            }
        }

        // Actualizar datos de usuario (Intentos fallidos, estado activo, etc.)
        public async Task UpdateAsync(User user)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = @"
                    UPDATE users 
                    SET Nombre = @Nombre,
                        Apellido = @Apellido,
                        Email = @Email,
                        PasswordHash = @PasswordHash,
                        Activo = @Activo,
                        IntentosFallidos = @IntentosFallidos
                    WHERE Id = @Id
                ";

                await conn.ExecuteAsync(query, new {
                    Id = user.Id.ToString(),
                    user.Nombre,
                    user.Apellido,
                    user.Email,
                    user.PasswordHash,
                    Activo = user.Activo ? 1 : 0,
                    user.IntentosFallidos
                });
            }
        }
    }
}

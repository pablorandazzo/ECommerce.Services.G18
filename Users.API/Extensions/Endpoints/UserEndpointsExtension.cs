using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Users.API.Data;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Models;

namespace Users.API.Extensions.Endpoints
{
    public static class UserEndpointsExtension
    {
        public static void MapUserEndpoints(this IEndpointRouteBuilder app)
        {
            // POST /api/users/register - Registrar nuevo usuario
            app.MapPost("/api/users/register", async (RegisterUserRequest req, UserRepository repo) =>
            {
                ValidarRegistro(req);

                // Validamos si el email ya existe
                var existing = await repo.GetByEmailAsync(req.Email);
                if (existing != null)
                {
                    throw new BusinessRuleException("USR-001", $"El email '{req.Email}' ya está registrado.");
                }

                // Creamos el nuevo usuario y generamos el hash de la contraseña
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Nombre = req.Nombre,
                    Apellido = req.Apellido,
                    Email = req.Email,
                    PasswordHash = GenerarHashPassword(req.Password),
                    FechaRegistro = DateTime.UtcNow,
                    Activo = true,
                    IntentosFallidos = 0
                };

                await repo.CreateAsync(user);

                return Results.Created($"/api/users/{user.Id}", new UserResponse(
                    user.Id,
                    user.Nombre,
                    user.Apellido,
                    user.Email,
                    user.FechaRegistro,
                    user.Activo
                ));
            })
            .WithTags("Users");

            // POST /api/users/login - Autenticar usuario
            app.MapPost("/api/users/login", async (LoginRequest req, UserRepository repo) =>
            {
                ValidarLogin(req);

                var user = await repo.GetByEmailAsync(req.Email);
                if (user == null)
                {
                    // Si el usuario no existe, devolvemos credenciales incorrectas
                    throw new NotFoundException("USR-003", "Credenciales incorrectas.");
                }

                // Si la cuenta está inactiva (bloqueada)
                if (!user.Activo)
                {
                    if (user.IntentosFallidos >= 3)
                    {
                        throw new BusinessRuleException("USR-004", "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.");
                    }
                    else
                    {
                        throw new BusinessRuleException("USR-005", "Su cuenta fue suspendida por razones de seguridad. Contacte a soporte.");
                    }
                }

                // Generamos el hash de la contraseña ingresada
                string inputHash = GenerarHashPassword(req.Password);

                if (user.PasswordHash == inputHash)
                {
                    // Login exitoso -> reseteamos intentos fallidos
                    user.IntentosFallidos = 0;
                    await repo.UpdateAsync(user);

                    return Results.Ok(new LoginResponse(
                        user.Id,
                        user.Nombre,
                        user.Apellido,
                        user.Email
                    ));
                }
                else
                {
                    // Login fallido -> incrementamos intentos fallidos
                    user.IntentosFallidos += 1;

                    if (user.IntentosFallidos >= 3)
                    {
                        user.Activo = false;
                        await repo.UpdateAsync(user);
                        throw new BusinessRuleException("USR-004", "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.");
                    }

                    await repo.UpdateAsync(user);
                    throw new NotFoundException("USR-003", "Credenciales incorrectas.");
                }
            })
            .WithTags("Users");

            // GET /api/users/{id} - Obtener información del usuario por ID (Útil para verificar la existencia del usuario desde Orders/Notifications)
            app.MapGet("/api/users/{id}", async (Guid id, UserRepository repo) =>
            {
                var user = await repo.GetByIdAsync(id);
                if (user == null)
                {
                    throw new NotFoundException("USR-006", "Usuario no encontrado.");
                }

                return Results.Ok(new UserResponse(
                    user.Id,
                    user.Nombre,
                    user.Apellido,
                    user.Email,
                    user.FechaRegistro,
                    user.Activo
                ));
            })
            .WithTags("Users");
        }

        // Generar hash SHA256 para contraseñas de forma simple y estándar
        private static string GenerarHashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        private static void ValidarRegistro(RegisterUserRequest req)
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(req.Nombre))
                errores.Add("El nombre es requerido.");

            if (string.IsNullOrWhiteSpace(req.Apellido))
                errores.Add("El apellido es requerido.");

            if (string.IsNullOrWhiteSpace(req.Email))
            {
                errores.Add("El email es requerido.");
            }
            else if (!req.Email.Contains("@"))
            {
                errores.Add("El email no tiene un formato válido.");
            }

            if (string.IsNullOrWhiteSpace(req.Password))
            {
                errores.Add("La contraseña es requerida.");
            }
            else if (req.Password.Length < 6)
            {
                errores.Add("La contraseña debe tener al menos 6 caracteres.");
            }

            if (errores.Count > 0)
            {
                string mensaje = string.Join("; ", errores);
                throw new ValidationException("USR-002", mensaje);
            }
        }

        private static void ValidarLogin(LoginRequest req)
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(req.Email))
                errores.Add("El email es requerido.");

            if (string.IsNullOrWhiteSpace(req.Password))
                errores.Add("La contraseña es requerida.");

            if (errores.Count > 0)
            {
                string mensaje = string.Join("; ", errores);
                throw new ValidationException("USR-002", mensaje);
            }
        }
    }
}

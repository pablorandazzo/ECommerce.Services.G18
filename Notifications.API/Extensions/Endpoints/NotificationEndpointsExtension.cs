using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Notifications.API.Data;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Models;

namespace Notifications.API.Extensions.Endpoints
{
    public static class NotificationEndpointsExtension
    {
        public static void MapNotificationEndpoints(this IEndpointRouteBuilder app)
        {
            // POST /api/notifications/send - Registrar y simular envío de notificación
            app.MapPost("/api/notifications/send", async (SendNotificationRequest req, NotificationRepository repo, IHttpClientFactory httpClientFactory, IConfiguration config) =>
            {
                ValidarEnvio(req);

                // 1. Comunicar con Users.API para verificar la existencia del usuario
                var client = httpClientFactory.CreateClient();
                string usersApiUrl = config.GetValue<string>("ServiceUrls:UsersApi") ?? "http://localhost:5002";

                try
                {
                    var response = await client.GetAsync($"{usersApiUrl}/api/users/{req.UsuarioId}");
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new NotFoundException("NTF-001", "El usuario destinatario no fue encontrado.");
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new BusinessRuleException("NTF-004", "Error al validar la existencia del usuario en Users.API.");
                    }
                }
                catch (Exception ex) when (ex is not NotFoundException && ex is not BusinessRuleException)
                {
                    throw new BusinessRuleException("NTF-004", "No se pudo establecer comunicación con Users.API. " + ex.Message);
                }

                // 2. Simular el envío exitoso
                var notification = new Notification
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = req.UsuarioId,
                    Mensaje = req.Mensaje,
                    Tipo = req.Tipo,
                    Estado = "Enviada", // Simulación exitosa
                    FechaEnvio = DateTime.UtcNow
                };

                await repo.CreateAsync(notification);

                return Results.Created($"/api/notifications/{notification.Id}", new NotificationResponse(
                    notification.Id,
                    notification.UsuarioId,
                    notification.Mensaje,
                    notification.Tipo,
                    notification.Estado,
                    notification.FechaEnvio
                ));
            })
            .WithTags("Notifications");

            // GET /api/notifications/{userId} - Listar notificaciones de un usuario
            app.MapGet("/api/notifications/{userId}", async (Guid userId, NotificationRepository repo) =>
            {
                var list = await repo.ListByUserIdAsync(userId);
                
                // Si la lista está vacía, lanzamos excepción NTF-003
                if (list == null || !list.Any())
                {
                    throw new NotFoundException("NTF-003", "No se encontraron notificaciones para el usuario.");
                }

                var responses = new List<NotificationResponse>();
                foreach (var item in list)
                {
                    responses.Add(new NotificationResponse(
                        item.Id,
                        item.UsuarioId,
                        item.Mensaje,
                        item.Tipo,
                        item.Estado,
                        item.FechaEnvio
                    ));
                }

                return Results.Ok(responses);
            })
            .WithTags("Notifications");
        }

        private static void ValidarEnvio(SendNotificationRequest req)
        {
            var errores = new List<string>();

            if (req.UsuarioId == Guid.Empty)
            {
                errores.Add("El UsuarioId no puede ser vacío.");
            }

            if (string.IsNullOrWhiteSpace(req.Mensaje))
            {
                errores.Add("El mensaje es requerido.");
            }
            else if (req.Mensaje.Length > 500)
            {
                errores.Add("El mensaje no puede superar los 500 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(req.Tipo))
            {
                errores.Add("El tipo de notificación es requerido.");
            }
            else
            {
                string tipoNormalizado = req.Tipo.Trim();
                if (tipoNormalizado != "Email" && tipoNormalizado != "Push" && tipoNormalizado != "SMS")
                {
                    errores.Add("Tipo de notificación no reconocido. Debe ser Email, Push o SMS.");
                }
            }

            if (errores.Count > 0)
            {
                string mensaje = string.Join("; ", errores);
                throw new ValidationException("NTF-002", mensaje);
            }
        }
    }
}

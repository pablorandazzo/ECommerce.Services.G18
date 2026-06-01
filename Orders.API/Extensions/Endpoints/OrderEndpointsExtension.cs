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
using Orders.API.Data;
using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Models;

namespace Orders.API.Extensions.Endpoints
{
    public static class OrderEndpointsExtension
    {
        // DTOs temporales para comunicación inter-servicios
        private record UserDto(Guid Id, string Nombre, string Apellido, string Email, DateTime FechaRegistro, bool Activo);
        
        private record ProductDto(
            Guid Id,
            string Nombre,
            string? Descripcion,
            decimal Precio,
            int Stock,
            string Categoria
        );

        private record SendNotificationRequest(Guid UsuarioId, string Mensaje, string Tipo);

        public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
        {
            // GET /api/orders - Listar órdenes (filtro opcional por usuarioId)
            app.MapGet("/api/orders", async (Guid? usuarioId, OrderRepository repo) =>
            {
                var orders = await repo.ListAsync(usuarioId);
                var responses = new List<OrderResponse>();

                foreach (var order in orders)
                {
                    var itemResponses = new List<OrderItemResponse>();
                    foreach (var item in order.Items)
                    {
                        itemResponses.Add(new OrderItemResponse(item.ProductoId, item.Cantidad, item.PrecioUnitario));
                    }

                    responses.Add(new OrderResponse(
                        order.Id,
                        order.UsuarioId,
                        itemResponses,
                        order.Total,
                        order.Estado,
                        order.FechaCreacion
                    ));
                }

                return Results.Ok(responses);
            })
            .WithTags("Orders");

            // GET /api/orders/{id} - Obtener detalle de una orden
            app.MapGet("/api/orders/{id}", async (Guid id, OrderRepository repo) =>
            {
                var order = await repo.GetByIdAsync(id);
                if (order == null)
                {
                    throw new NotFoundException("ORD-001", "Orden no encontrada.");
                }

                var itemResponses = new List<OrderItemResponse>();
                foreach (var item in order.Items)
                {
                    itemResponses.Add(new OrderItemResponse(item.ProductoId, item.Cantidad, item.PrecioUnitario));
                }

                return Results.Ok(new OrderResponse(
                    order.Id,
                    order.UsuarioId,
                    itemResponses,
                    order.Total,
                    order.Estado,
                    order.FechaCreacion
                ));
            })
            .WithTags("Orders");

            // GET /api/orders/check-product/{productId} - Verificar si un producto tiene órdenes activas (Llamado interno desde Products.API)
            app.MapGet("/api/orders/check-product/{productId}", async (Guid productId, OrderRepository repo) =>
            {
                bool hasActive = await repo.HasActiveOrdersForProductAsync(productId);
                return Results.Ok(hasActive);
            })
            .WithTags("Orders");

            // PUT /api/orders/{id}/status - Actualizar estado de una orden
            app.MapPut("/api/orders/{id}/status", async (Guid id, UpdateOrderStatusRequest req, OrderRepository repo) =>
            {
                if (string.IsNullOrWhiteSpace(req.NuevoEstado))
                {
                    throw new ValidationException("ORD-002", "El nuevo estado no puede estar vacío.");
                }

                var order = await repo.GetByIdAsync(id);
                if (order == null)
                {
                    throw new NotFoundException("ORD-001", "Orden no encontrada.");
                }

                // Validación de transiciones válidas de estado
                string estadoActual = order.Estado;
                string nuevoEstado = req.NuevoEstado.Trim();

                bool esTransicionValida = false;

                if (estadoActual == "Pendiente")
                {
                    if (nuevoEstado == "Confirmada" || nuevoEstado == "Cancelada")
                    {
                        esTransicionValida = true;
                    }
                }
                else if (estadoActual == "Confirmada")
                {
                    if (nuevoEstado == "Enviada" || nuevoEstado == "Cancelada")
                    {
                        esTransicionValida = true;
                    }
                }
                else if (estadoActual == "Enviada")
                {
                    if (nuevoEstado == "Entregada")
                    {
                        esTransicionValida = true;
                    }
                }

                if (!esTransicionValida)
                {
                    throw new BusinessRuleException("ORD-006", $"Una orden en estado '{estadoActual}' no puede pasar a '{nuevoEstado}'.");
                }

                await repo.UpdateStatusAsync(id, nuevoEstado);
                return Results.Ok(new { Id = id, Estado = nuevoEstado, FechaActualizacion = DateTime.UtcNow });
            })
            .WithTags("Orders");

            // POST /api/orders - Crear nueva orden (Orquestación e inter-servicio)
            app.MapPost("/api/orders", async (CreateOrderRequest req, OrderRepository repo, IHttpClientFactory httpClientFactory, IConfiguration config) =>
            {
                ValidarCrearOrden(req);

                var client = httpClientFactory.CreateClient("ECommerceClients");
                string usersApiUrl = config.GetValue<string>("ServiceUrls:UsersApi") ?? "http://localhost:5002";
                string productsApiUrl = config.GetValue<string>("ServiceUrls:ProductsApi") ?? "http://localhost:5001";
                string notificationsApiUrl = config.GetValue<string>("ServiceUrls:NotificationsApi") ?? "http://localhost:5005";

                // 1. Validar que el UsuarioId exista en Users.API
                try
                {
                    var userResponse = await client.GetAsync($"{usersApiUrl}/api/users/{req.UsuarioId}");
                    if (userResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new NotFoundException("ORD-003", "Usuario no encontrado al crear la orden.");
                    }

                    if (!userResponse.IsSuccessStatusCode)
                    {
                        throw new BusinessRuleException("ORD-007", "Error al validar existencia de usuario en Users.API.");
                    }
                }
                catch (Exception ex) when (ex is not NotFoundException && ex is not BusinessRuleException)
                {
                    throw new BusinessRuleException("ORD-007", "No se pudo establecer comunicación con Users.API. " + ex.Message);
                }

                var itemsToCreate = new List<OrderItem>();
                decimal total = 0;

                // 2. Validar cada producto y verificar stock
                var productsToUpdate = new List<(ProductDto Product, int CantidadVendida)>();

                foreach (var itemReq in req.Items)
                {
                    ProductDto? product = null;

                    try
                    {
                        var productResponse = await client.GetAsync($"{productsApiUrl}/api/products/{itemReq.ProductoId}");
                        if (productResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            throw new NotFoundException("ORD-004", "Producto no encontrado al crear la orden.");
                        }

                        if (!productResponse.IsSuccessStatusCode)
                        {
                            throw new BusinessRuleException("ORD-007", "Error al verificar producto en Products.API.");
                        }

                        product = await productResponse.Content.ReadFromJsonAsync<ProductDto>();
                    }
                    catch (Exception ex) when (ex is not NotFoundException && ex is not BusinessRuleException)
                    {
                        throw new BusinessRuleException("ORD-007", "No se pudo establecer comunicación con Products.API. " + ex.Message);
                    }

                    if (product == null)
                    {
                        throw new BusinessRuleException("ORD-007", "Error al leer datos del producto.");
                    }

                    if (product.Stock < itemReq.Cantidad)
                    {
                        throw new BusinessRuleException("ORD-005", $"Stock insuficiente para '{product.Nombre}'. Disponible: {product.Stock}, solicitado: {itemReq.Cantidad}.", 422);
                    }

                    total += itemReq.Cantidad * product.Precio;

                    itemsToCreate.Add(new OrderItem
                    {
                        ProductoId = itemReq.ProductoId,
                        Cantidad = itemReq.Cantidad,
                        PrecioUnitario = product.Precio
                    });

                    productsToUpdate.Add((product, itemReq.Cantidad));
                }

                // 3. Crear la orden localmente en estado inicial "Pendiente"
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    UsuarioId = req.UsuarioId,
                    Items = itemsToCreate,
                    Total = total,
                    Estado = "Pendiente",
                    FechaCreacion = DateTime.UtcNow
                };

                await repo.CreateAsync(order);

                // 4. Deducir stock remoto en Products.API con compensación automática
                var successfulUpdates = new List<(ProductDto Product, int OriginalStock)>();
                bool stockUpdateFailed = false;
                string failReason = "";

                foreach (var update in productsToUpdate)
                {
                    int nuevoStock = update.Product.Stock - update.CantidadVendida;
                    
                    try
                    {
                        var updateRequest = new {
                            Nombre = update.Product.Nombre,
                            Descripcion = update.Product.Descripcion,
                            Precio = update.Product.Precio,
                            Stock = nuevoStock,
                            Categoria = update.Product.Categoria
                        };

                        var response = await client.PutAsJsonAsync($"{productsApiUrl}/api/products/{update.Product.Id}", updateRequest);
                        if (!response.IsSuccessStatusCode)
                        {
                            stockUpdateFailed = true;
                            failReason = $"No se pudo actualizar el stock del producto '{update.Product.Nombre}'.";
                            break;
                        }
                        successfulUpdates.Add((update.Product, update.Product.Stock));
                    }
                    catch (Exception ex)
                    {
                        stockUpdateFailed = true;
                        failReason = "Error de red al actualizar stock en Products.API. " + ex.Message;
                        break;
                    }
                }

                if (stockUpdateFailed)
                {
                    // COMPENSACIÓN: Revertir stock descontado en Products.API
                    foreach (var rollBack in successfulUpdates)
                    {
                        try
                        {
                            var revertRequest = new {
                                Nombre = rollBack.Product.Nombre,
                                Descripcion = rollBack.Product.Descripcion,
                                Precio = rollBack.Product.Precio,
                                Stock = rollBack.OriginalStock,
                                Categoria = rollBack.Product.Categoria
                            };
                            await client.PutAsJsonAsync($"{productsApiUrl}/api/products/{rollBack.Product.Id}", revertRequest);
                        }
                        catch
                        {
                            // Ignoramos errores de compensación para continuar deshaciendo el resto si es posible
                        }
                    }

                    // COMPENSACIÓN: Cancelar orden local en base de datos
                    await repo.UpdateStatusAsync(order.Id, "Cancelada");

                    throw new BusinessRuleException("ORD-007", "Error al procesar el stock de la orden: " + failReason, 500);
                }

                // 5. Transición final: Confirmar orden localmente tras deducir stock con éxito
                order.Estado = "Confirmada";
                await repo.UpdateStatusAsync(order.Id, "Confirmada");

                // 6. Simular envío asíncrono de notificación (Sincronizada con el estado "Confirmada")
                try
                {
                    var notificationReq = new SendNotificationRequest(
                        req.UsuarioId,
                        $"Su orden #{order.Id} fue confirmada.",
                        "Email"
                    );

                    // No esperamos a que falle la creación si la notificación cae
                    _ = client.PostAsJsonAsync($"{notificationsApiUrl}/api/notifications/send", notificationReq);
                }
                catch
                {
                    // Ignoramos errores de notificación para no tumbar la creación de la orden
                }

                // Construimos DTO de respuesta
                var itemResponses = new List<OrderItemResponse>();
                foreach (var item in order.Items)
                {
                    itemResponses.Add(new OrderItemResponse(item.ProductoId, item.Cantidad, item.PrecioUnitario));
                }

                return Results.Created($"/api/orders/{order.Id}", new OrderResponse(
                    order.Id,
                    order.UsuarioId,
                    itemResponses,
                    order.Total,
                    order.Estado,
                    order.FechaCreacion
                ));
            })
            .WithTags("Orders");
        }

        private static void ValidarCrearOrden(CreateOrderRequest req)
        {
            var errores = new List<string>();

            if (req.UsuarioId == Guid.Empty)
            {
                errores.Add("El UsuarioId no puede estar vacío.");
            }

            if (req.Items == null || !req.Items.Any())
            {
                errores.Add("La lista de ítems de la orden no puede estar vacía.");
            }
            else
            {
                for (int i = 0; i < req.Items.Count; i++)
                {
                    var item = req.Items[i];
                    if (item.ProductoId == Guid.Empty)
                    {
                        errores.Add($"Ítem {i + 1}: El ProductoId es requerido.");
                    }
                    if (item.Cantidad <= 0)
                    {
                        errores.Add($"Ítem {i + 1}: La cantidad debe ser mayor a 0.");
                    }
                }
            }

            if (errores.Count > 0)
            {
                string mensaje = string.Join("; ", errores);
                throw new ValidationException("ORD-002", mensaje);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Products.API.Data;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;

namespace Products.API.Extensions.Endpoints
{
    public static class ProductEndpointsExtension
    {
        public static void MapProductEndpoints(this IEndpointRouteBuilder app)
        {
            // GET /api/products - Listar productos
            app.MapGet("/api/products", async (string? categoria, string? nombre, ProductRepository repo) =>
            {
                var products = await repo.GetAllAsync(categoria, nombre);
                var responses = new List<ProductResponse>();

                foreach (var product in products)
                {
                    responses.Add(new ProductResponse(
                        product.Id,
                        product.Nombre,
                        product.Descripcion,
                        product.Precio,
                        product.Stock,
                        product.Categoria,
                        product.FechaCreacion
                    ));
                }

                return Results.Ok(responses);
            })
            .WithTags("Products");

            // GET /api/products/{id} - Obtener producto por ID
            app.MapGet("/api/products/{id}", async (Guid id, ProductRepository repo) =>
            {
                var product = await repo.GetByIdAsync(id);
                if (product == null)
                {
                    throw new NotFoundException("PRD-001", "Producto no encontrado.");
                }

                return Results.Ok(new ProductResponse(
                    product.Id,
                    product.Nombre,
                    product.Descripcion,
                    product.Precio,
                    product.Stock,
                    product.Categoria,
                    product.FechaCreacion
                ));
            })
            .WithTags("Products");

            // POST /api/products - Crear nuevo producto
            app.MapPost("/api/products", async (CreateProductRequest req, ProductRepository repo) =>
            {
                ValidarCrearProducto(req);

                // Validamos si ya existe con el mismo nombre en la categoría
                var existing = await repo.GetByNameAndCategoryAsync(req.Nombre, req.Categoria);
                if (existing != null)
                {
                    throw new BusinessRuleException("PRD-003", $"Ya existe un producto con el nombre '{req.Nombre}' en la categoría '{req.Categoria}'.");
                }

                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    Nombre = req.Nombre,
                    Descripcion = req.Descripcion,
                    Precio = req.Precio,
                    Stock = req.Stock,
                    Categoria = req.Categoria,
                    FechaCreacion = DateTime.UtcNow
                };

                await repo.CreateAsync(product);

                return Results.Created($"/api/products/{product.Id}", new ProductResponse(
                    product.Id,
                    product.Nombre,
                    product.Descripcion,
                    product.Precio,
                    product.Stock,
                    product.Categoria,
                    product.FechaCreacion
                ));
            })
            .WithTags("Products");

            // PUT /api/products/{id} - Actualizar producto
            app.MapPut("/api/products/{id}", async (Guid id, UpdateProductRequest req, ProductRepository repo) =>
            {
                ValidarActualizarProducto(req);

                var product = await repo.GetByIdAsync(id);
                if (product == null)
                {
                    throw new NotFoundException("PRD-001", "Producto no encontrado.");
                }

                product.Nombre = req.Nombre;
                product.Descripcion = req.Descripcion;
                product.Precio = req.Precio;
                product.Stock = req.Stock;
                product.Categoria = req.Categoria;

                await repo.UpdateAsync(product);

                return Results.Ok(new ProductResponse(
                    product.Id,
                    product.Nombre,
                    product.Descripcion,
                    product.Precio,
                    product.Stock,
                    product.Categoria,
                    product.FechaCreacion
                ));
            })
            .WithTags("Products");

            // DELETE /api/products/{id} - Eliminar producto
            app.MapDelete("/api/products/{id}", async (Guid id, ProductRepository repo, IHttpClientFactory httpClientFactory, IConfiguration config, HttpContext httpContext) =>
            {
                var product = await repo.GetByIdAsync(id);
                if (product == null)
                {
                    throw new NotFoundException("PRD-001", "Producto no encontrado.");
                }

                // Obtener correlation ID actual
                string correlationId = "";
                if (httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var val))
                {
                    correlationId = val.ToString();
                }

                // Verificar si tiene órdenes activas llamando a Orders.API
                var client = httpClientFactory.CreateClient("OrdersApi");
                string ordersApiUrl = config.GetValue<string>("ServiceUrls:OrdersApi") ?? "http://localhost:5003";

                try
                {
                    var response = await client.GetAsync($"{ordersApiUrl}/api/orders/check-product/{id}");
                    if (response.IsSuccessStatusCode)
                    {
                        bool hasActiveOrders = await response.Content.ReadFromJsonAsync<bool>();
                        if (hasActiveOrders)
                        {
                            throw new BusinessRuleException("PRD-004", "El producto tiene órdenes activas y no puede eliminarse.");
                        }
                    }
                    else
                    {
                        // Si responde un código de error de Orders.API, lo tratamos como error de comunicación (502 Bad Gateway)
                        return Results.Problem(
                            detail: "No se pudo validar el estado de las órdenes en Orders.API.",
                            instance: $"/api/products/{id}",
                            statusCode: StatusCodes.Status502BadGateway,
                            title: "Error de comunicación inter-servicio",
                            extensions: new Dictionary<string, object?>
                            {
                                { "errorCode", "PRD-005" },
                                { "errorMessage", "No se pudo validar el estado de las órdenes en la API remota." },
                                { "correlationId", correlationId }
                            }
                        );
                    }
                }
                catch (Exception ex) when (ex is not BusinessRuleException && ex is not NotFoundException)
                {
                    // Ante falla de red o comunicación, rechazamos preventivamente (503 Service Unavailable)
                    return Results.Problem(
                        detail: "Falla de red o comunicación con Orders.API: " + ex.Message,
                        instance: $"/api/products/{id}",
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "Servicio no disponible",
                        extensions: new Dictionary<string, object?>
                        {
                            { "errorCode", "PRD-005" },
                            { "errorMessage", "No se pudo establecer comunicación con la API remota debido a una falla de red." },
                            { "correlationId", correlationId }
                        }
                    );
                }

                await repo.DeleteAsync(id);
                return Results.NoContent();
            })
            .WithTags("Products");
        }

        private static void ValidarCrearProducto(CreateProductRequest req)
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(req.Nombre))
                errores.Add("El nombre es requerido.");
            else if (req.Nombre.Length > 100)
                errores.Add("El nombre no puede superar los 100 caracteres.");

            if (req.Descripcion != null && req.Descripcion.Length > 500)
                errores.Add("La descripción no puede superar los 500 caracteres.");

            if (req.Precio <= 0)
                errores.Add("El precio debe ser mayor a 0.");

            if (req.Stock < 0)
                errores.Add("El stock debe ser mayor o igual a 0.");

            if (string.IsNullOrWhiteSpace(req.Categoria))
                errores.Add("La categoría es requerida.");

            if (errores.Count > 0)
            {
                string mensaje = string.Join("; ", errores);
                throw new ValidationException("PRD-002", mensaje);
            }
        }

        private static void ValidarActualizarProducto(UpdateProductRequest req)
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(req.Nombre))
                errores.Add("El nombre es requerido.");
            else if (req.Nombre.Length > 100)
                errores.Add("El nombre no puede superar los 100 caracteres.");

            if (req.Descripcion != null && req.Descripcion.Length > 500)
                errores.Add("La descripción no puede superar los 500 caracteres.");

            if (req.Precio <= 0)
                errores.Add("El precio debe ser mayor a 0.");

            if (req.Stock < 0)
                errores.Add("El stock debe ser mayor o igual a 0.");

            if (string.IsNullOrWhiteSpace(req.Categoria))
                errores.Add("La categoría es requerida.");

            if (errores.Count > 0)
            {
                string mensaje = string.Join("; ", errores);
                throw new ValidationException("PRD-002", mensaje);
            }
        }
    }
}

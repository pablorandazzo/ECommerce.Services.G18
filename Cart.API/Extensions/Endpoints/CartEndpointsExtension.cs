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
using Cart.API.Data;
using Cart.API.DTOs;
using Cart.API.Exceptions;
using Cart.API.Models;

namespace Cart.API.Extensions.Endpoints
{
    public static class CartEndpointsExtension
    {
        // DTO temporal para recibir datos de Products.API
        private record ProductDto(
            Guid Id,
            string Nombre,
            string? Descripcion,
            decimal Precio,
            int Stock,
            string Categoria
        );

        public static void MapCartEndpoints(this IEndpointRouteBuilder app)
        {
            // GET /api/cart/{userId} - Obtener carrito del usuario
            app.MapGet("/api/cart/{userId}", async (Guid userId, CartRepository repo) =>
            {
                var cart = await repo.GetByUserIdAsync(userId);
                if (cart == null)
                {
                    throw new NotFoundException("CRT-001", "Carrito no encontrado.");
                }

                var itemResponses = new List<CartItemResponse>();
                foreach (var item in cart.Items)
                {
                    itemResponses.Add(new CartItemResponse(item.ProductoId, item.Cantidad));
                }

                return Results.Ok(new CartResponse(cart.UsuarioId, itemResponses, cart.FechaActualizacion));
            })
            .WithTags("Cart");

            // POST /api/cart/{userId}/items - Agregar producto al carrito
            app.MapPost("/api/cart/{userId}/items", async (Guid userId, AddCartItemRequest req, CartRepository repo, IHttpClientFactory httpClientFactory, IConfiguration config) =>
            {
                if (req.Cantidad <= 0)
                {
                    throw new BusinessRuleException("CRT-004", "Cantidad inválida.");
                }

                // 1. Obtener cantidad existente del producto en el carrito del usuario
                int existingQty = 0;
                var cart = await repo.GetByUserIdAsync(userId);
                if (cart != null)
                {
                    var existingItem = cart.Items.FirstOrDefault(i => i.ProductoId == req.ProductoId);
                    if (existingItem != null)
                    {
                        existingQty = existingItem.Cantidad;
                    }
                }
                int cantidadRequerida = existingQty + req.Cantidad;

                // 2. Comunicar con Products.API para validar existencia y stock
                var client = httpClientFactory.CreateClient("ProductsApi");
                string productsApiUrl = config.GetValue<string>("ServiceUrls:ProductsApi") ?? "http://localhost:5001";

                ProductDto? product = null;
                try
                {
                    var response = await client.GetAsync($"{productsApiUrl}/api/products/{req.ProductoId}");
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new NotFoundException("CRT-002", "Producto no encontrado.");
                    }
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new BusinessRuleException("CRT-005", "Error al validar el producto en Products.API.");
                    }

                    product = await response.Content.ReadFromJsonAsync<ProductDto>();
                }
                catch (Exception ex) when (ex is not NotFoundException && ex is not BusinessRuleException)
                {
                    throw new BusinessRuleException("CRT-005", "No se pudo establecer comunicación con Products.API. " + ex.Message);
                }

                if (product == null)
                {
                    throw new BusinessRuleException("CRT-005", "No se pudo leer la información del producto.");
                }

                if (product.Stock < cantidadRequerida)
                {
                    throw new BusinessRuleException("CRT-003", $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {cantidadRequerida}.");
                }

                // 3. Agregar el producto al carrito
                var cartItem = new CartItem
                {
                    ProductoId = req.ProductoId,
                    Cantidad = cantidadRequerida
                };

                await repo.AddOrUpdateItemAsync(userId, cartItem);

                // Recuperamos el carrito actualizado
                var updatedCart = await repo.GetByUserIdAsync(userId);
                var itemResponses = new List<CartItemResponse>();
                foreach (var item in updatedCart!.Items)
                {
                    itemResponses.Add(new CartItemResponse(item.ProductoId, item.Cantidad));
                }

                return Results.Ok(new CartResponse(updatedCart.UsuarioId, itemResponses, updatedCart.FechaActualizacion));
            })
            .WithTags("Cart");

            // PUT /api/cart/{userId}/items/{productId} - Actualizar cantidad de un item
            app.MapPut("/api/cart/{userId}/items/{productId}", async (Guid userId, Guid productId, UpdateCartItemRequest req, CartRepository repo, IHttpClientFactory httpClientFactory, IConfiguration config) =>
            {
                if (req.Cantidad <= 0)
                {
                    throw new BusinessRuleException("CRT-004", "Cantidad inválida.");
                }

                // Verificamos si existe el carrito
                var cart = await repo.GetByUserIdAsync(userId);
                if (cart == null)
                {
                    throw new NotFoundException("CRT-001", "Carrito no encontrado.");
                }

                // 1. Comunicar con Products.API para validar existencia y stock
                var client = httpClientFactory.CreateClient("ProductsApi");
                string productsApiUrl = config.GetValue<string>("ServiceUrls:ProductsApi") ?? "http://localhost:5001";

                ProductDto? product = null;
                try
                {
                    var response = await client.GetAsync($"{productsApiUrl}/api/products/{productId}");
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new NotFoundException("CRT-002", "Producto no encontrado.");
                    }
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new BusinessRuleException("CRT-005", "Error al validar el producto en Products.API.");
                    }

                    product = await response.Content.ReadFromJsonAsync<ProductDto>();
                }
                catch (Exception ex) when (ex is not NotFoundException && ex is not BusinessRuleException)
                {
                    throw new BusinessRuleException("CRT-005", "No se pudo establecer comunicación con Products.API.");
                }

                if (product == null)
                {
                    throw new BusinessRuleException("CRT-005", "No se pudo leer la información del producto.");
                }

                if (product.Stock < req.Cantidad)
                {
                    throw new BusinessRuleException("CRT-003", $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {req.Cantidad}.");
                }

                // 2. Actualizar el ítem
                var cartItem = new CartItem
                {
                    ProductoId = productId,
                    Cantidad = req.Cantidad
                };

                await repo.AddOrUpdateItemAsync(userId, cartItem);

                // Recuperamos el carrito actualizado
                var updatedCart = await repo.GetByUserIdAsync(userId);
                var itemResponses = new List<CartItemResponse>();
                foreach (var item in updatedCart!.Items)
                {
                    itemResponses.Add(new CartItemResponse(item.ProductoId, item.Cantidad));
                }

                return Results.Ok(new CartResponse(updatedCart.UsuarioId, itemResponses, updatedCart.FechaActualizacion));
            })
            .WithTags("Cart");

            // DELETE /api/cart/{userId}/items/{productId} - Quitar un producto del carrito
            app.MapDelete("/api/cart/{userId}/items/{productId}", async (Guid userId, Guid productId, CartRepository repo) =>
            {
                var cart = await repo.GetByUserIdAsync(userId);
                if (cart == null)
                {
                    throw new NotFoundException("CRT-001", "Carrito no encontrado.");
                }

                bool deleted = await repo.DeleteItemAsync(userId, productId);
                if (!deleted)
                {
                    // Si el producto no estaba en el carrito
                    throw new NotFoundException("CRT-002", "Producto no encontrado.");
                }

                return Results.NoContent();
            })
            .WithTags("Cart");

            // DELETE /api/cart/{userId} - Vaciar carrito completo
            app.MapDelete("/api/cart/{userId}", async (Guid userId, CartRepository repo) =>
            {
                bool cleared = await repo.ClearCartAsync(userId);
                if (!cleared)
                {
                    throw new NotFoundException("CRT-001", "Carrito no encontrado.");
                }

                return Results.NoContent();
            })
            .WithTags("Cart");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Cart.API.Models;

namespace Cart.API.Data
{
    public class CartRepository
    {
        private readonly IConfiguration _config;

        public CartRepository(IConfiguration config)
        {
            _config = config;
        }

        private SqliteConnection CreateConnection()
        {
            string connectionString = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Data Source=carts.db";
            }
            return new SqliteConnection(connectionString);
        }

        // Obtener el carrito completo de un usuario
        public async Task<Models.Cart?> GetByUserIdAsync(Guid userId)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                // Buscamos si existe la entrada en la tabla carts
                string cartQuery = "SELECT UsuarioId, FechaActualizacion FROM carts WHERE UsuarioId = @UsuarioId";
                var rawCart = await conn.QueryFirstOrDefaultAsync(cartQuery, new { UsuarioId = userId.ToString() });
                
                if (rawCart == null) return null;

                // Buscamos los ítems de ese carrito
                string itemsQuery = "SELECT ProductoId, Cantidad FROM cart_items WHERE UsuarioId = @UsuarioId";
                var rawItems = await conn.QueryAsync(itemsQuery, new { UsuarioId = userId.ToString() });

                Models.Cart cart = new Models.Cart
                {
                    UsuarioId = Guid.Parse((string)rawCart.UsuarioId),
                    FechaActualizacion = DateTime.Parse((string)rawCart.FechaActualizacion),
                    Items = new List<CartItem>()
                };

                foreach (var rawItem in rawItems)
                {
                    cart.Items.Add(new CartItem
                    {
                        ProductoId = Guid.Parse((string)rawItem.ProductoId),
                        Cantidad = Convert.ToInt32(rawItem.Cantidad)
                    });
                }

                return cart;
            }
        }

        // Crear un carrito vacío para un usuario
        public async Task CreateCartAsync(Guid userId)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = "INSERT INTO carts (UsuarioId, FechaActualizacion) VALUES (@UsuarioId, @FechaActualizacion)";
                await conn.ExecuteAsync(query, new {
                    UsuarioId = userId.ToString(),
                    FechaActualizacion = DateTime.UtcNow.ToString("o")
                });
            }
        }

        // Agregar o actualizar un producto en el carrito
        public async Task AddOrUpdateItemAsync(Guid userId, CartItem item)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                // Si el carrito no existe en la tabla carts, lo creamos automáticamente
                string checkQuery = "SELECT 1 FROM carts WHERE UsuarioId = @UsuarioId";
                var exists = await conn.QueryFirstOrDefaultAsync<int?>(checkQuery, new { UsuarioId = userId.ToString() });
                if (exists == null)
                {
                    await CreateCartAsync(userId);
                }

                // Insertar o reemplazar (upsert) el ítem
                string query = @"
                    INSERT INTO cart_items (UsuarioId, ProductoId, Cantidad)
                    VALUES (@UsuarioId, @ProductoId, @Cantidad)
                    ON CONFLICT(UsuarioId, ProductoId) DO UPDATE SET Cantidad = @Cantidad
                ";

                await conn.ExecuteAsync(query, new {
                    UsuarioId = userId.ToString(),
                    ProductoId = item.ProductoId.ToString(),
                    item.Cantidad
                });

                // Actualizar la fecha de modificación del carrito
                string updateTimeQuery = "UPDATE carts SET FechaActualizacion = @Fecha WHERE UsuarioId = @UsuarioId";
                await conn.ExecuteAsync(updateTimeQuery, new {
                    Fecha = DateTime.UtcNow.ToString("o"),
                    UsuarioId = userId.ToString()
                });
            }
        }

        // Quitar un producto específico del carrito
        public async Task<bool> DeleteItemAsync(Guid userId, Guid productId)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = "DELETE FROM cart_items WHERE UsuarioId = @UsuarioId AND ProductoId = @ProductoId";
                int rows = await conn.ExecuteAsync(query, new {
                    UsuarioId = userId.ToString(),
                    ProductoId = productId.ToString()
                });

                if (rows > 0)
                {
                    // Actualizar la fecha de modificación del carrito
                    string updateTimeQuery = "UPDATE carts SET FechaActualizacion = @Fecha WHERE UsuarioId = @UsuarioId";
                    await conn.ExecuteAsync(updateTimeQuery, new {
                        Fecha = DateTime.UtcNow.ToString("o"),
                        UsuarioId = userId.ToString()
                    });
                    return true;
                }

                return false;
            }
        }

        // Vaciar completamente el carrito de un usuario
        public async Task<bool> ClearCartAsync(Guid userId)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                // Primero vemos si tiene un carrito
                string checkQuery = "SELECT 1 FROM carts WHERE UsuarioId = @UsuarioId";
                var exists = await conn.QueryFirstOrDefaultAsync<int?>(checkQuery, new { UsuarioId = userId.ToString() });
                if (exists == null) return false;

                // Eliminamos todos sus ítems
                string deleteItemsQuery = "DELETE FROM cart_items WHERE UsuarioId = @UsuarioId";
                await conn.ExecuteAsync(deleteItemsQuery, new { UsuarioId = userId.ToString() });

                // Actualizar la fecha del carrito
                string updateTimeQuery = "UPDATE carts SET FechaActualizacion = @Fecha WHERE UsuarioId = @UsuarioId";
                await conn.ExecuteAsync(updateTimeQuery, new {
                    Fecha = DateTime.UtcNow.ToString("o"),
                    UsuarioId = userId.ToString()
                });

                return true;
            }
        }
    }
}

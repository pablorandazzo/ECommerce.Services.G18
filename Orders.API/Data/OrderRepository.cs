using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Orders.API.Models;

namespace Orders.API.Data
{
    public class OrderRepository
    {
        private readonly IConfiguration _config;

        public OrderRepository(IConfiguration config)
        {
            _config = config;
        }

        private SqliteConnection CreateConnection()
        {
            string? connectionString = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Data Source=orders.db";
            }
            return new SqliteConnection(connectionString);
        }

        // Listar órdenes (con filtro opcional por UsuarioId)
        public async Task<IEnumerable<Order>> ListAsync(Guid? userId)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = "SELECT Id, UsuarioId, Total, Estado, FechaCreacion FROM orders WHERE 1=1";
                var parameters = new DynamicParameters();

                if (userId.HasValue && userId.Value != Guid.Empty)
                {
                    query += " AND UsuarioId = @UsuarioId";
                    parameters.Add("UsuarioId", userId.Value.ToString());
                }

                query += " ORDER BY FechaCreacion DESC";
                var rawOrders = await conn.QueryAsync(query, parameters);

                var list = new List<Order>();
                foreach (var raw in rawOrders)
                {
                    Guid orderId = Guid.Parse((string)raw.Id);
                    
                    // Traemos los items para cada orden
                    string itemsQuery = "SELECT ProductoId, Cantidad, PrecioUnitario FROM order_items WHERE OrderId = @OrderId";
                    var rawItems = await conn.QueryAsync(itemsQuery, new { OrderId = orderId.ToString() });
                    
                    var order = new Order
                    {
                        Id = orderId,
                        UsuarioId = Guid.Parse((string)raw.UsuarioId),
                        Total = Convert.ToDecimal(raw.Total),
                        Estado = (string)raw.Estado,
                        FechaCreacion = DateTime.Parse((string)raw.FechaCreacion),
                        Items = new List<OrderItem>()
                    };

                    foreach (var rawItem in rawItems)
                    {
                        order.Items.Add(new OrderItem
                        {
                            ProductoId = Guid.Parse((string)rawItem.ProductoId),
                            Cantidad = Convert.ToInt32(rawItem.Cantidad),
                            PrecioUnitario = Convert.ToDecimal(rawItem.PrecioUnitario)
                        });
                    }

                    list.Add(order);
                }

                return list;
            }
        }

        // Obtener orden por ID
        public async Task<Order?> GetByIdAsync(Guid id)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = "SELECT Id, UsuarioId, Total, Estado, FechaCreacion FROM orders WHERE Id = @Id";
                var raw = await conn.QueryFirstOrDefaultAsync(query, new { Id = id.ToString() });
                
                if (raw == null) return null;

                // Traemos los items
                string itemsQuery = "SELECT ProductoId, Cantidad, PrecioUnitario FROM order_items WHERE OrderId = @OrderId";
                var rawItems = await conn.QueryAsync(itemsQuery, new { OrderId = id.ToString() });

                var order = new Order
                {
                    Id = Guid.Parse((string)raw.Id),
                    UsuarioId = Guid.Parse((string)raw.UsuarioId),
                    Total = Convert.ToDecimal(raw.Total),
                    Estado = (string)raw.Estado,
                    FechaCreacion = DateTime.Parse((string)raw.FechaCreacion),
                    Items = new List<OrderItem>()
                };

                foreach (var rawItem in rawItems)
                {
                    order.Items.Add(new OrderItem
                    {
                        ProductoId = Guid.Parse((string)rawItem.ProductoId),
                        Cantidad = Convert.ToInt32(rawItem.Cantidad),
                        PrecioUnitario = Convert.ToDecimal(rawItem.PrecioUnitario)
                    });
                }

                return order;
            }
        }

        // Crear una nueva orden con sus ítems en una única transacción
        public async Task CreateAsync(Order order)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Insertamos la orden principal
                        string orderQuery = @"
                            INSERT INTO orders (Id, UsuarioId, Total, Estado, FechaCreacion)
                            VALUES (@Id, @UsuarioId, @Total, @Estado, @FechaCreacion)
                        ";
                        await conn.ExecuteAsync(orderQuery, new {
                            Id = order.Id.ToString(),
                            UsuarioId = order.UsuarioId.ToString(),
                            Total = (double)order.Total,
                            order.Estado,
                            FechaCreacion = order.FechaCreacion.ToString("o")
                        }, transaction);

                        // 2. Insertamos los ítems de la orden
                        string itemQuery = @"
                            INSERT INTO order_items (OrderId, ProductoId, Cantidad, PrecioUnitario)
                            VALUES (@OrderId, @ProductoId, @Cantidad, @PrecioUnitario)
                        ";
                        foreach (var item in order.Items)
                        {
                            await conn.ExecuteAsync(itemQuery, new {
                                OrderId = order.Id.ToString(),
                                ProductoId = item.ProductoId.ToString(),
                                item.Cantidad,
                                PrecioUnitario = (double)item.PrecioUnitario
                            }, transaction);
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // Actualizar el estado de la orden
        public async Task UpdateStatusAsync(Guid id, string status)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = "UPDATE orders SET Estado = @Estado WHERE Id = @Id";
                await conn.ExecuteAsync(query, new { Estado = status, Id = id.ToString() });
            }
        }

        // Verificar si existen órdenes activas que referencien un producto
        public async Task<bool> HasActiveOrdersForProductAsync(Guid productId)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                // Un producto tiene órdenes activas si está en una orden cuyo estado es "Pendiente" o "Confirmada"
                string query = @"
                    SELECT COUNT(1) 
                    FROM order_items oi
                    INNER JOIN orders o ON oi.OrderId = o.Id
                    WHERE oi.ProductoId = @ProductoId AND o.Estado IN ('Pendiente', 'Confirmada')
                ";
                int count = await conn.ExecuteScalarAsync<int>(query, new { ProductoId = productId.ToString() });
                return count > 0;
            }
        }
    }
}

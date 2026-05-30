using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Products.API.Models;

namespace Products.API.Data
{
    public class ProductRepository
    {
        private readonly IConfiguration _config;

        public ProductRepository(IConfiguration config)
        {
            _config = config;
        }

        private SqliteConnection CreateConnection()
        {
            string? connectionString = _config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "Data Source=products.db";
            }
            return new SqliteConnection(connectionString);
        }

        // Obtener todos los productos con filtros opcionales de categoría y nombre
        public async Task<IEnumerable<Product>> GetAllAsync(string? categoria, string? nombre)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = "SELECT Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion FROM products WHERE 1=1";
                var parameters = new DynamicParameters();

                if (!string.IsNullOrEmpty(categoria))
                {
                    query += " AND Categoria = @Categoria";
                    parameters.Add("Categoria", categoria);
                }

                if (!string.IsNullOrEmpty(nombre))
                {
                    query += " AND Nombre LIKE @Nombre";
                    parameters.Add("Nombre", "%" + nombre + "%");
                }

                return await conn.QueryAsync<Product>(query, parameters);
            }
        }

        // Obtener un producto por ID
        public async Task<Product?> GetByIdAsync(Guid id)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = "SELECT Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion FROM products WHERE Id = @Id";
                return await conn.QueryFirstOrDefaultAsync<Product>(query, new { Id = id.ToString() });
            }
        }

        // Buscar producto por nombre y categoría exactos (para validación de duplicados)
        public async Task<Product?> GetByNameAndCategoryAsync(string nombre, string categoria)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = "SELECT Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion FROM products WHERE Nombre = @Nombre AND Categoria = @Categoria";
                return await conn.QueryFirstOrDefaultAsync<Product>(query, new { Nombre = nombre, Categoria = categoria });
            }
        }

        // Crear producto
        public async Task CreateAsync(Product product)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = @"
                    INSERT INTO products (Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion)
                    VALUES (@Id, @Nombre, @Descripcion, @Precio, @Stock, @Categoria, @FechaCreacion)
                ";
                
                await conn.ExecuteAsync(query, new {
                    Id = product.Id.ToString(),
                    product.Nombre,
                    product.Descripcion,
                    product.Precio,
                    product.Stock,
                    product.Categoria,
                    FechaCreacion = product.FechaCreacion.ToString("o")
                });
            }
        }

        // Actualizar producto
        public async Task UpdateAsync(Product product)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = @"
                    UPDATE products 
                    SET Nombre = @Nombre, 
                        Descripcion = @Descripcion, 
                        Precio = @Precio, 
                        Stock = @Stock, 
                        Categoria = @Categoria 
                    WHERE Id = @Id
                ";

                await conn.ExecuteAsync(query, new {
                    Id = product.Id.ToString(),
                    product.Nombre,
                    product.Descripcion,
                    product.Precio,
                    product.Stock,
                    product.Categoria
                });
            }
        }

        // Eliminar producto
        public async Task<bool> DeleteAsync(Guid id)
        {
            using (SqliteConnection conn = CreateConnection())
            {
                string query = "DELETE FROM products WHERE Id = @Id";
                int rowsAffected = await conn.ExecuteAsync(query, new { Id = id.ToString() });
                return rowsAffected > 0;
            }
        }
    }
}

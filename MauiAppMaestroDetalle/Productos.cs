using Dapper; 
using MauiAppMaestroDetalle;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MauiAppMaestroDetalle
{
    public class Productos
    {
        private string connectionString;
        private SqliteConnection connection;

        public Productos()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "sistema_pedidos.db");
            connectionString = $"Data Source={dbPath};";
            connection = new SqliteConnection(connectionString);
            connection.Open();

            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Productos (
                    Id INTEGER PRIMARY KEY,
                    Nombre TEXT NOT NULL,
                    Precio REAL NOT NULL,
                    Stock INTEGER NOT NULL,
                    ImgUrl TEXT )"
                );
        }

        public Producto Create(int id, string nombre, double precio, int stock, string imgUrl)
        {
            var nuevoProducto = new Producto
            {
                Id = id,
                Nombre = nombre,
                Precio = precio,
                Stock = stock,
                ImgUrl = imgUrl
            };

            var recordsAffected = connection.Execute(
                "INSERT INTO Productos (Id, Nombre, Precio, Stock, ImgUrl) " +
                "VALUES (@Id, @Nombre, @Precio, @Stock, @ImgUrl)",
                nuevoProducto);

            if (recordsAffected == 0)
                throw new Exception("No se pudo insertar el producto en la base de datos.");
            else
                return nuevoProducto;
        }

        public Producto ReadById(int id)
        {
            var data = connection.Query<Producto>(
                "SELECT * FROM Productos WHERE Id = @IdProducto",
                new { IdProducto = id })
                .ToList();

            if (data.Count == 0)
                return null;
            else
                return data[0];
        }

        public List<Producto> ReadAll()
        {
            var data = connection.Query<Producto>("SELECT * FROM Productos").ToList();
            return data;
        }

        public void Update(int id, string nombre, double precio, int stock, string imgUrl)
        {
            var recordsAffected = connection.Execute(
                "UPDATE Productos SET Nombre = @Nombre, Precio = @Precio, Stock = @Stock, ImgUrl = @ImgUrl " +
                "WHERE Id = @Id",
                new { Id = id, Nombre = nombre, Precio = precio, Stock = stock, ImgUrl = imgUrl });

            if (recordsAffected == 0)
                throw new Exception("No se pudo actualizar el producto en la base de datos.");
        }

        public void Delete(int id)
        {
            var recordsAffected = connection.Execute(
                "DELETE FROM Productos WHERE Id = @IdProducto",
                new { IdProducto = id });

            if (recordsAffected == 0)
                throw new Exception($"No se pudo eliminar el producto con Id {id} de la base de datos.");
        }
    }
}
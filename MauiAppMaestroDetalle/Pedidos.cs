using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MauiAppMaestroDetalle
{
    /// <summary>
    /// Gestor Maestro-Detalle para Pedidos.
    /// Inicializa las tablas PedidosCab y PedidosDet en SQLite
    /// y utiliza transacciones de Dapper para garantizar la integridad
    /// al guardar un pedido completo (cabecera + detalles).
    /// </summary>
    public class Pedidos
    {
        private string connectionString;
        private SqliteConnection connection;

        public Pedidos()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "sistema_pedidos.db");
            connectionString = $"Data Source={dbPath};";
            connection = new SqliteConnection(connectionString);
            connection.Open();

            // ═══════════════════════════════════════════
            //  Inicializar tabla CABECERA de Pedidos
            // ═══════════════════════════════════════════
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS PedidosCab (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Fecha TEXT NOT NULL,
                    Cliente TEXT NOT NULL,
                    Total REAL NOT NULL
                )");

            // ═══════════════════════════════════════════
            //  Inicializar tabla DETALLE de Pedidos
            // ═══════════════════════════════════════════
            connection.Execute(@"
                CREATE TABLE IF NOT EXISTS PedidosDet (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    PedidoCabId INTEGER NOT NULL,
                    ProductoId INTEGER NOT NULL,
                    Cantidad INTEGER NOT NULL,
                    PrecioUnitario REAL NOT NULL,
                    Subtotal REAL NOT NULL,
                    FOREIGN KEY (PedidoCabId) REFERENCES PedidosCab(Id),
                    FOREIGN KEY (ProductoId) REFERENCES Productos(Id)
                )");
        }

        // ═══════════════════════════════════════════════════════════
        //  CREATE: Guardar pedido completo dentro de una TRANSACCIÓN
        //  Si la cabecera o cualquier detalle falla, se hace ROLLBACK
        //  y no se guardan datos incompletos en la base de datos.
        // ═══════════════════════════════════════════════════════════
        public PedidoCab CrearPedidoCompleto(string cliente, List<PedidoDet> detalles)
        {
            if (detalles == null || detalles.Count == 0)
                throw new Exception("El pedido debe tener al menos un detalle.");

            if (string.IsNullOrWhiteSpace(cliente))
                throw new Exception("Debe indicar el nombre del cliente.");

            // Calcular el total sumando los subtotales de cada detalle
            double totalPedido = detalles.Sum(d => d.Subtotal);

            // Crear el objeto cabecera
            var cabecera = new PedidoCab
            {
                Fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Cliente = cliente,
                Total = totalPedido
            };

            // ─── Iniciar Transacción ───
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1) Insertar la CABECERA del pedido
                var sqlCab = @"INSERT INTO PedidosCab (Fecha, Cliente, Total) 
                               VALUES (@Fecha, @Cliente, @Total);
                               SELECT last_insert_rowid();";

                var idGenerado = connection.ExecuteScalar<int>(sqlCab, cabecera, transaction);
                cabecera.Id = idGenerado;

                // 2) Insertar cada DETALLE vinculado a la cabecera
                var sqlDet = @"INSERT INTO PedidosDet (PedidoCabId, ProductoId, Cantidad, PrecioUnitario, Subtotal) 
                               VALUES (@PedidoCabId, @ProductoId, @Cantidad, @PrecioUnitario, @Subtotal)";

                foreach (var detalle in detalles)
                {
                    detalle.PedidoCabId = cabecera.Id;
                    var affected = connection.Execute(sqlDet, detalle, transaction);

                    if (affected == 0)
                        throw new Exception($"No se pudo insertar el detalle del producto ID {detalle.ProductoId}.");
                }

                // 3) Si todo salió bien → COMMIT
                transaction.Commit();

                return cabecera;
            }
            catch
            {
                // Si algo falla → ROLLBACK (no se guardan datos incompletos)
                transaction.Rollback();
                throw;
            }
        }

        // ═══════════════════════════════════════════
        //  READ: Obtener todos los pedidos (cabecera)
        // ═══════════════════════════════════════════
        public List<PedidoCab> ReadAllPedidos()
        {
            return connection.Query<PedidoCab>(
                "SELECT * FROM PedidosCab ORDER BY Id DESC").ToList();
        }

        // ═══════════════════════════════════════════
        //  READ: Obtener un pedido por ID
        // ═══════════════════════════════════════════
        public PedidoCab ReadPedidoById(int id)
        {
            return connection.Query<PedidoCab>(
                "SELECT * FROM PedidosCab WHERE Id = @Id",
                new { Id = id }).FirstOrDefault();
        }

        // ═══════════════════════════════════════════
        //  READ: Obtener los detalles de un pedido
        // ═══════════════════════════════════════════
        public List<PedidoDet> ReadDetallesByPedidoId(int pedidoCabId)
        {
            return connection.Query<PedidoDet>(
                "SELECT * FROM PedidosDet WHERE PedidoCabId = @PedidoCabId",
                new { PedidoCabId = pedidoCabId }).ToList();
        }

        // ═══════════════════════════════════════════════════════════════
        //  READ: Obtener detalles con nombre del producto (JOIN)
        //  Retorna una proyección enriquecida para mostrar en la UI
        // ═══════════════════════════════════════════════════════════════
        public List<PedidoDetVista> ReadDetallesConProducto(int pedidoCabId)
        {
            var sql = @"SELECT d.Id, d.PedidoCabId, d.ProductoId, d.Cantidad, 
                               d.PrecioUnitario, d.Subtotal, p.Nombre AS NombreProducto, p.ImgUrl
                        FROM PedidosDet d
                        INNER JOIN Productos p ON d.ProductoId = p.Id
                        WHERE d.PedidoCabId = @PedidoCabId";

            return connection.Query<PedidoDetVista>(sql,
                new { PedidoCabId = pedidoCabId }).ToList();
        }

        // ═══════════════════════════════════════════
        //  DELETE: Eliminar un pedido completo
        //  (cabecera + detalles) con transacción
        // ═══════════════════════════════════════════
        public void DeletePedido(int pedidoId)
        {
            using var transaction = connection.BeginTransaction();

            try
            {
                // Primero eliminar detalles (dependencia FK)
                connection.Execute(
                    "DELETE FROM PedidosDet WHERE PedidoCabId = @Id",
                    new { Id = pedidoId }, transaction);

                // Luego eliminar cabecera
                var affected = connection.Execute(
                    "DELETE FROM PedidosCab WHERE Id = @Id",
                    new { Id = pedidoId }, transaction);

                if (affected == 0)
                    throw new Exception($"No se encontró el pedido con ID {pedidoId}.");

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    /// <summary>
    /// Clase de proyección para mostrar detalles de pedido
    /// enriquecidos con datos del producto (nombre e imagen).
    /// </summary>
    public class PedidoDetVista
    {
        public int Id { get; set; }
        public int PedidoCabId { get; set; }
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public double PrecioUnitario { get; set; }
        public double Subtotal { get; set; }
        public string NombreProducto { get; set; }
        public string ImgUrl { get; set; }
    }
}

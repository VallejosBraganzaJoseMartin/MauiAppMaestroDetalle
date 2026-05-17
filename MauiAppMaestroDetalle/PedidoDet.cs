using System;
using System.Collections.Generic;
using System.Text;

namespace MauiAppMaestroDetalle
{
    public class PedidoDet
    {
        public int Id { get; set; }
        public int PedidoCabId { get; set; } 
        public int ProductoId { get; set; }  
        public int Cantidad { get; set; }
        public double PrecioUnitario { get; set; }
        public double Subtotal { get; set; }
    }
}

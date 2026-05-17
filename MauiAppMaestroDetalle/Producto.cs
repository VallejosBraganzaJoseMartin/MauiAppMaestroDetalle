using System;
using System.Collections.Generic;
using System.Text;

namespace MauiAppMaestroDetalle
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public double Precio { get; set; }
        public int Stock { get; set; }
        public string ImgUrl { get; set; }
    }
}

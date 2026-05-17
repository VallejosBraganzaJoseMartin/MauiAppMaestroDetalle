using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MauiAppMaestroDetalle
{
    public partial class NuevoPedidoPage : ContentPage
    {
        private Productos dbProductos;
        private Pedidos dbPedidos;

        // Lista maestra del catálogo (para búsqueda)
        private List<Producto> todosLosProductos;

        // Detalles del pedido en construcción (observable para actualizar la UI)
        private ObservableCollection<DetalleEnConstruccion> detallesActuales;

        public NuevoPedidoPage()
        {
            InitializeComponent();
            dbProductos = new Productos();
            dbPedidos = new Pedidos();
            detallesActuales = new ObservableCollection<DetalleEnConstruccion>();
            cvDetalles.ItemsSource = detallesActuales;
            CargarCatalogo();
        }

        // ═══════════════════════════════════════════
        //  Cargar catálogo de productos
        // ═══════════════════════════════════════════
        private void CargarCatalogo()
        {
            try
            {
                todosLosProductos = dbProductos.ReadAll();
                cvCatalogo.ItemsSource = todosLosProductos;
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"No se pudo cargar el catálogo: {ex.Message}", "OK");
            }
        }

        // ═══════════════════════════════════════════
        //  Buscar en el catálogo
        // ═══════════════════════════════════════════
        private void OnBuscarProductoCatalogo(object sender, TextChangedEventArgs e)
        {
            var filtro = e.NewTextValue?.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(filtro))
                cvCatalogo.ItemsSource = todosLosProductos;
            else
                cvCatalogo.ItemsSource = todosLosProductos?
                    .Where(p => p.Nombre != null && p.Nombre.ToLowerInvariant().Contains(filtro))
                    .ToList();
        }

        // ═══════════════════════════════════════════
        //  Agregar producto al detalle del pedido
        // ═══════════════════════════════════════════
        private async void OnAgregarProductoAlPedido(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Producto producto)
            {
                // Verificar si el producto ya está en el detalle
                var existente = detallesActuales.FirstOrDefault(d => d.ProductoId == producto.Id);

                if (existente != null)
                {
                    // Incrementar cantidad
                    existente.Cantidad++;
                    existente.Subtotal = existente.Cantidad * existente.PrecioUnitario;

                    // Refrescar la lista para actualizar la UI
                    RefrescarDetalles();
                }
                else
                {
                    // Solicitar cantidad al usuario
                    string cantidadStr = await DisplayPromptAsync(
                        "Cantidad",
                        $"¿Cuántas unidades de \"{producto.Nombre}\"?",
                        initialValue: "1",
                        keyboard: Keyboard.Numeric);

                    if (string.IsNullOrEmpty(cantidadStr)) return;

                    if (!int.TryParse(cantidadStr, out int cantidad) || cantidad <= 0)
                    {
                        await DisplayAlert("Error", "Ingrese una cantidad válida mayor a 0.", "OK");
                        return;
                    }

                    if (cantidad > producto.Stock)
                    {
                        await DisplayAlert("Stock insuficiente",
                            $"Solo hay {producto.Stock} unidades disponibles de \"{producto.Nombre}\".", "OK");
                        return;
                    }

                    var nuevoDetalle = new DetalleEnConstruccion
                    {
                        ProductoId = producto.Id,
                        NombreProducto = producto.Nombre,
                        ImgUrl = producto.ImgUrl,
                        PrecioUnitario = producto.Precio,
                        Cantidad = cantidad,
                        Subtotal = producto.Precio * cantidad
                    };

                    detallesActuales.Add(nuevoDetalle);
                }

                ActualizarTotales();
            }
        }

        // ═══════════════════════════════════════════
        //  Quitar detalle (botón ✕)
        // ═══════════════════════════════════════════
        private void OnQuitarDetalleClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is DetalleEnConstruccion detalle)
            {
                detallesActuales.Remove(detalle);
                ActualizarTotales();
            }
        }

        // ═══════════════════════════════════════════
        //  Quitar detalle (swipe)
        // ═══════════════════════════════════════════
        private void OnQuitarDetalleSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is DetalleEnConstruccion detalle)
            {
                detallesActuales.Remove(detalle);
                ActualizarTotales();
            }
        }

        // ═══════════════════════════════════════════
        //  Confirmar y guardar el pedido (TRANSACCIÓN)
        // ═══════════════════════════════════════════
        private async void OnConfirmarPedido(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCliente.Text))
            {
                await DisplayAlert("Campo requerido", "Ingrese el nombre del cliente.", "OK");
                return;
            }

            if (detallesActuales.Count == 0)
            {
                await DisplayAlert("Pedido vacío", "Agregue al menos un producto al pedido.", "OK");
                return;
            }

            bool confirmar = await DisplayAlert(
                "Confirmar Pedido",
                $"Cliente: {txtCliente.Text}\n" +
                $"Productos: {detallesActuales.Count}\n" +
                $"Total: ${detallesActuales.Sum(d => d.Subtotal):F2}\n\n" +
                "¿Desea confirmar el pedido?",
                "Confirmar", "Cancelar");

            if (!confirmar) return;

            try
            {
                // Convertir DetalleEnConstruccion → PedidoDet para persistir
                var detallesParaGuardar = detallesActuales.Select(d => new PedidoDet
                {
                    ProductoId = d.ProductoId,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    Subtotal = d.Subtotal
                }).ToList();

                // Guardar con TRANSACCIÓN (todo o nada)
                var pedidoCreado = dbPedidos.CrearPedidoCompleto(txtCliente.Text.Trim(), detallesParaGuardar);

                await DisplayAlert("✓ Pedido Registrado",
                    $"Pedido #{pedidoCreado.Id} guardado exitosamente.\n" +
                    $"Total: ${pedidoCreado.Total:F2}", "OK");

                // Volver a la página principal
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error al guardar",
                    $"La transacción fue revertida. No se guardaron datos incompletos.\n\n{ex.Message}", "OK");
            }
        }

        // ═══════════════════════════════════════════
        //  Volver a la página anterior
        // ═══════════════════════════════════════════
        private async void OnVolverClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // ═══════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════
        private void ActualizarTotales()
        {
            double total = detallesActuales.Sum(d => d.Subtotal);
            lblTotal.Text = $"${total:F2}";
            lblItemCount.Text = $"{detallesActuales.Count} items";
        }

        private void RefrescarDetalles()
        {
            // Forzar re-render del CollectionView
            var temp = detallesActuales.ToList();
            detallesActuales.Clear();
            foreach (var item in temp)
                detallesActuales.Add(item);
        }
    }

    /// <summary>
    /// Clase auxiliar para construir el detalle del pedido en la UI
    /// antes de persistirlo en la base de datos.
    /// Incluye datos del producto para mostrar en la interfaz.
    /// </summary>
    public class DetalleEnConstruccion
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; }
        public string ImgUrl { get; set; }
        public double PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public double Subtotal { get; set; }
    }
}

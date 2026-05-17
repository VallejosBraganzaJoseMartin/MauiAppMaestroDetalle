using System;
using System.Collections.Generic;
using System.Linq;

namespace MauiAppMaestroDetalle
{
    public partial class MainPage : ContentPage
    {
        // Instancia de nuestra clase controladora de SQLite con Dapper
        private Productos dbProductos;
        private Pedidos dbPedidos;

        // Lista maestra de productos (para búsqueda)
        private List<Producto> todosLosProductos;

        // Indica si estamos en modo edición (true) o creación (false)
        private bool modoEdicion = false;

        // ID original del producto en edición
        private int idProductoEditando;

        // Tab activo: true = Productos, false = Pedidos
        private bool tabProductosActivo = true;

        public MainPage()
        {
            InitializeComponent();
            dbProductos = new Productos();
            dbPedidos = new Pedidos();
            CargarProductos();
        }

        // Se ejecuta cada vez que la página se muestra (ej: volver de NuevoPedidoPage)
        protected override void OnAppearing()
        {
            base.OnAppearing();
            CargarProductos();

            if (!tabProductosActivo)
                CargarPedidos();
        }

        // ═══════════════════════════════════════════════════
        //  TABS: Cambiar entre Productos y Pedidos
        // ═══════════════════════════════════════════════════
        private void OnTabProductosClicked(object sender, EventArgs e)
        {
            tabProductosActivo = true;
            vistaProductos.IsVisible = true;
            vistaPedidos.IsVisible = false;

            // Estilo del tab activo
            tabProductos.TextColor = Color.FromArgb("#E53238");
            tabProductos.FontAttributes = FontAttributes.Bold;
            tabPedidos.TextColor = Color.FromArgb("#757575");
            tabPedidos.FontAttributes = FontAttributes.None;

            fabAgregar.Text = "＋";
            txtBuscar.Placeholder = "Buscar productos...";
        }

        private void OnTabPedidosClicked(object sender, EventArgs e)
        {
            tabProductosActivo = false;
            vistaProductos.IsVisible = false;
            vistaPedidos.IsVisible = true;

            // Estilo del tab activo
            tabPedidos.TextColor = Color.FromArgb("#E53238");
            tabPedidos.FontAttributes = FontAttributes.Bold;
            tabProductos.TextColor = Color.FromArgb("#757575");
            tabProductos.FontAttributes = FontAttributes.None;

            fabAgregar.Text = "📦";
            txtBuscar.Placeholder = "Buscar pedidos...";

            CargarPedidos();
        }

        // ═══════════════════════════════════════════
        //  FAB: Acción según el tab activo
        // ═══════════════════════════════════════════
        private async void OnFabClicked(object sender, EventArgs e)
        {
            if (tabProductosActivo)
            {
                // Modo Productos → abrir panel crear
                modoEdicion = false;
                lblFormTitulo.Text = "Nuevo Producto";
                btnGuardar.Text = "Guardar Producto";
                LimpiarFormulario();
                txtId.IsEnabled = true;
                panelFormulario.IsVisible = true;
            }
            else
            {
                // Modo Pedidos → navegar a NuevoPedidoPage
                await Navigation.PushAsync(new NuevoPedidoPage());
            }
        }

        // ═══════════════════════════════════════════
        //  READ: Cargar todos los productos
        // ═══════════════════════════════════════════
        private void CargarProductos()
        {
            try
            {
                todosLosProductos = dbProductos.ReadAll();
                cvProductos.ItemsSource = todosLosProductos;
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"No se pudieron cargar los productos: {ex.Message}", "OK");
            }
        }

        // ═══════════════════════════════════════════
        //  READ: Cargar todos los pedidos
        // ═══════════════════════════════════════════
        private void CargarPedidos()
        {
            try
            {
                var pedidos = dbPedidos.ReadAllPedidos();
                cvPedidos.ItemsSource = pedidos;
            }
            catch (Exception ex)
            {
                DisplayAlert("Error", $"No se pudieron cargar los pedidos: {ex.Message}", "OK");
            }
        }

        // ═══════════════════════════════════════════
        //  SEARCH: Filtrar productos por nombre
        // ═══════════════════════════════════════════
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            var filtro = e.NewTextValue?.Trim().ToLowerInvariant();

            if (tabProductosActivo)
            {
                if (string.IsNullOrEmpty(filtro))
                    cvProductos.ItemsSource = todosLosProductos;
                else
                    cvProductos.ItemsSource = todosLosProductos?
                        .Where(p => p.Nombre != null && p.Nombre.ToLowerInvariant().Contains(filtro))
                        .ToList();
            }
            else
            {
                // Filtrar pedidos por cliente o ID
                var todosPedidos = dbPedidos.ReadAllPedidos();
                if (string.IsNullOrEmpty(filtro))
                    cvPedidos.ItemsSource = todosPedidos;
                else
                    cvPedidos.ItemsSource = todosPedidos
                        .Where(p => (p.Cliente != null && p.Cliente.ToLowerInvariant().Contains(filtro))
                                 || p.Id.ToString().Contains(filtro))
                        .ToList();
            }
        }

        // ═══════════════════════════════════════════
        //  VER DETALLES DE UN PEDIDO
        // ═══════════════════════════════════════════
        private async void OnVerDetallesPedido(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is PedidoCab pedido)
            {
                try
                {
                    var detalles = dbPedidos.ReadDetallesConProducto(pedido.Id);

                    if (detalles == null || detalles.Count == 0)
                    {
                        await DisplayAlert("Sin detalles",
                            $"El pedido #{pedido.Id} no tiene detalles registrados.", "OK");
                        return;
                    }

                    // Construir el mensaje con los detalles
                    var mensaje = $"📅 {pedido.Fecha}\n👤 {pedido.Cliente}\n\n";
                    mensaje += "─────────────────────\n";

                    foreach (var det in detalles)
                    {
                        mensaje += $"• {det.NombreProducto}\n";
                        mensaje += $"  {det.Cantidad} × ${det.PrecioUnitario:F2} = ${det.Subtotal:F2}\n\n";
                    }

                    mensaje += "─────────────────────\n";
                    mensaje += $"💰 TOTAL: ${pedido.Total:F2}";

                    await DisplayAlert($"Pedido #{pedido.Id}", mensaje, "Cerrar");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"No se pudieron cargar los detalles: {ex.Message}", "OK");
                }
            }
        }

        // ═══════════════════════════════════════════
        //  CLOSE: Cerrar panel de formulario
        // ═══════════════════════════════════════════
        private void OnCerrarPanelClicked(object sender, EventArgs e)
        {
            panelFormulario.IsVisible = false;
            LimpiarFormulario();
        }

        // ═══════════════════════════════════════════
        //  SAVE: Crear o Actualizar producto
        // ═══════════════════════════════════════════
        private void OnGuardarClicked(object sender, EventArgs e)
        {
            try
            {
                if (modoEdicion)
                {
                    // UPDATE
                    dbProductos.Update(
                        idProductoEditando,
                        txtNombre.Text,
                        double.Parse(txtPrecio.Text),
                        int.Parse(txtStock.Text),
                        txtImgUrl.Text
                    );

                    DisplayAlert("Actualizado", $"El producto con ID {idProductoEditando} fue actualizado.", "OK");
                }
                else
                {
                    // CREATE
                    var nuevoProducto = dbProductos.Create(
                        int.Parse(txtId.Text),
                        txtNombre.Text,
                        double.Parse(txtPrecio.Text),
                        int.Parse(txtStock.Text),
                        txtImgUrl.Text
                    );

                    DisplayAlert("Producto Creado", $"Se ha registrado {nuevoProducto.Nombre} correctamente.", "OK");
                }

                panelFormulario.IsVisible = false;
                LimpiarFormulario();
                CargarProductos();
            }
            catch (Exception ex)
            {
                DisplayAlert("Error al guardar", ex.Message, "OK");
            }
        }

        // ═══════════════════════════════════════════
        //  EDIT (Button)
        // ═══════════════════════════════════════════
        private void OnEditarClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Producto producto)
                AbrirEdicion(producto);
        }

        // ═══════════════════════════════════════════
        //  EDIT (Swipe)
        // ═══════════════════════════════════════════
        private void OnEditarSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is Producto producto)
                AbrirEdicion(producto);
        }

        private void AbrirEdicion(Producto producto)
        {
            modoEdicion = true;
            idProductoEditando = producto.Id;
            lblFormTitulo.Text = "Editar Producto";
            btnGuardar.Text = "Actualizar Producto";

            txtId.Text = producto.Id.ToString();
            txtId.IsEnabled = false;
            txtNombre.Text = producto.Nombre;
            txtPrecio.Text = producto.Precio.ToString();
            txtStock.Text = producto.Stock.ToString();
            txtImgUrl.Text = producto.ImgUrl;
            imgProducto.Source = producto.ImgUrl;

            panelFormulario.IsVisible = true;
        }

        // ═══════════════════════════════════════════
        //  DELETE (Button)
        // ═══════════════════════════════════════════
        private async void OnEliminarClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Producto producto)
                await EliminarProducto(producto);
        }

        // ═══════════════════════════════════════════
        //  DELETE (Swipe)
        // ═══════════════════════════════════════════
        private async void OnEliminarSwipe(object sender, EventArgs e)
        {
            if (sender is SwipeItem swipe && swipe.BindingContext is Producto producto)
                await EliminarProducto(producto);
        }

        private async Task EliminarProducto(Producto producto)
        {
            bool confirmar = await DisplayAlert(
                "Confirmar Eliminación",
                $"¿Estás seguro de eliminar \"{producto.Nombre}\"?",
                "Eliminar",
                "Cancelar");

            if (!confirmar) return;

            try
            {
                dbProductos.Delete(producto.Id);
                await DisplayAlert("Eliminado", "El producto ha sido removido de la base de datos.", "OK");
                CargarProductos();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error al eliminar", ex.Message, "OK");
            }
        }

        // ═══════════════════════════════════════════
        //  HELPER: Limpiar campos del formulario
        // ═══════════════════════════════════════════
        private void LimpiarFormulario()
        {
            txtId.Text = "";
            txtNombre.Text = "";
            txtPrecio.Text = "";
            txtStock.Text = "";
            txtImgUrl.Text = "";
            imgProducto.Source = null;
        }
    }
}

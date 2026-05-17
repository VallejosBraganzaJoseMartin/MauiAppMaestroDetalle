namespace MauiAppMaestroDetalle
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Registrar rutas para navegación
            Routing.RegisterRoute(nameof(NuevoPedidoPage), typeof(NuevoPedidoPage));
        }
    }
}

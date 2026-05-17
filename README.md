# MauiAppMaestroDetalle

Aplicacion desarrollada en .NET MAUI que implementa un sistema de gestion de pedidos usando el patron **Maestro-Detalle**. Permite administrar un catalogo de productos y armar pedidos a partir de ellos, con persistencia local en SQLite y transacciones para garantizar la integridad de los datos.

## Catalogo de productos

La pantalla principal muestra los productos registrados en un grid de tarjetas. Desde aca se pueden crear, editar y eliminar productos, ademas de buscarlos por nombre.

<!-- ![Catalogo de productos](screenshots/catalogo.png) -->
<img width="381" height="883" alt="image" src="https://github.com/user-attachments/assets/0b31fe1b-91f7-47fc-aa70-e874e047205b" />

Cada producto tiene su imagen, nombre, precio y stock. Para agregar o editar un producto se abre un panel en la parte inferior de la pantalla.

<!-- ![Formulario de producto](screenshots/formulario_producto.png) -->
<img width="387" height="889" alt="image" src="https://github.com/user-attachments/assets/c63c88f5-6586-40f3-bce4-b0f528031929" />

## Creacion de pedidos

Al cambiar a la pestaña de pedidos y tocar el boton de nuevo pedido, se navega a una pantalla donde se arma el pedido paso a paso: se ingresa el cliente, se seleccionan productos del catalogo y se definen las cantidades.

<!-- ![Nuevo pedido](screenshots/nuevo_pedido.png) -->
<img width="376" height="888" alt="image" src="https://github.com/user-attachments/assets/ee54f651-3fbb-4c28-8b1b-5d8d6b7ca32b" />

El detalle se va armando en tiempo real con el subtotal de cada linea y el total general del pedido. Al confirmar, todo se guarda dentro de una transaccion SQL, asi que si algo falla no quedan datos incompletos.

<!-- ![Detalle del pedido en construccion](screenshots/detalle_pedido_nuevo.png) -->
<img width="382" height="892" alt="image" src="https://github.com/user-attachments/assets/36de25cc-96bb-4531-a5d3-e98b4ca6cb4c" />

## Historial de pedidos

Todos los pedidos confirmados quedan registrados y se pueden consultar desde la pestaña de pedidos. Se muestra el cliente, la fecha y el total de cada uno.

<!-- ![Historial de pedidos](screenshots/historial_pedidos.png) -->
<img width="384" height="887" alt="image" src="https://github.com/user-attachments/assets/39748e90-1995-4ab1-900e-b7b9fb1cac17" />

Al tocar "Ver detalles" se despliega el desglose completo del pedido con los productos, cantidades y subtotales.

<!-- ![Detalles de un pedido](screenshots/detalles_pedido.png) -->
<img width="386" height="892" alt="image" src="https://github.com/user-attachments/assets/ae44e8aa-2cfe-43ac-bc6b-3f0884d9521a" />

## Requisitos previos

- Visual Studio 2022 o superior
- Carga de trabajo de **.NET MAUI** instalada en Visual Studio
- .NET 10 SDK

## Como ejecutar

```bash
# Clonar el repositorio
git clone https://github.com/tu-usuario/MauiAppMaestroDetalle.git
cd MauiAppMaestroDetalle

# Restaurar dependencias
dotnet restore

# Compilar el proyecto
dotnet build

# Ejecutar (ejemplo para Windows)
dotnet run --project MauiAppMaestroDetalle -f net10.0-windows10.0.19041.0
```

O simplemente abrir `MauiAppMaestroDetalle.slnx` en Visual Studio, seleccionar el target (Android, Windows, etc.) y darle Run.

La base de datos SQLite se crea automaticamente la primera vez que se ejecuta la app, no requiere configuracion adicional.

## Tecnologias

- .NET MAUI
- SQLite (Microsoft.Data.Sqlite)
- Dapper

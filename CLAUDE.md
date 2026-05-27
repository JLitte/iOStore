# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Run the app (development)
dotnet run

# Build
dotnet build

# Apply pending EF Core migrations to the local DB
dotnet ef database update

# Add a new migration
dotnet ef migrations add <NombreMigracion>

# Restore packages
dotnet restore
```

The project targets **.NET 9** and uses SQL Server (local instance `VANI-PC`, database `iOStoreDbase1`). There is also a commented-out Somee.com connection string in `appsettings.json` for deployment.

## Architecture

**iOStore** is an ASP.NET Core 9 MVC e-commerce store for Apple products, with two routing areas:

- **Default area** — public-facing store: `HomeController` (catalog), `ProductoController` (detail), `CarritoController` (shopping cart), `PedidoController` (orders), `AccountController` (auth).
- **Admin area** (`/Admin/...`) — back-office restricted to `Administrador` and `AdminEmpleado` roles: `DashboardController`, `CategoriasController`, `UsuariosController`.

### Identity & Roles

`ApplicationUser` extends `IdentityUser` with `NombreCompleto`, `Direccion`, `Telefono`, `FechaRegistro`, `FechaIncorporacion` (employees only), and `Activo`.

Three roles are defined as constants in `Helpers/Roles.cs`:
- `Administrador` — full access
- `AdminEmpleado` — admin area + order management
- `Cliente` — registered customer (assigned automatically on register)

Seed users are created in `Program.cs` at startup: `admin@iostore.com` / `Admin123!` and `empleado@iostore.com` / `Empleado123!`.

### Authentication flow

Registration requires email confirmation via a **6-digit code** (not a link). `VerificationCodeService` (singleton, in-memory) generates/validates codes for two purposes: `ConfirmEmail` and `ResetPassword`. Codes expire in 15 minutes. The same code flow is used for password reset. `SmtpEmailSender` sends HTML emails via Gmail SMTP (credentials in `appsettings.json`).

### Data model

Key relationships:
- `Producto` ↔ `Categoria`: many-to-many through `ProductoCategoria` (explicit join entity).
- `Pedido` → `ApplicationUser` (customer, `UsuarioId`) and optionally → `ApplicationUser` (employee who handled it, `EmpleadoId` — for sales traceability).
- `Pedido` → `PedidoDetalle` → `Producto` (cascade delete on pedido side, restrict on product side).
- `CarritoItem`: unique index on `(UsuarioId, ProductoId)` — one row per product per user.

Order states: `Pendiente` → `Procesando` → `Enviado` → `Entregado` / `Cancelado`.

### Stored procedure

`ApplicationDbContext.GetProductosVendidosPorEmpleadoAsync(desde, hasta)` calls `SP_ProductosVendidosPorEmpleado` in SQL Server and returns `ProductoVendidoPorEmpleadoDto` records. The SP must be created manually in the DB (it is added via a migration in `Data/Migrations/`).

### Helpers

- `PaginatedList<T>` — generic async pagination helper used in `PedidoController` and admin controllers.
- `FormatHelper.TiempoTranscurrido(date)` — formats employee seniority for the dashboard.
- `Roles` — use these string constants everywhere instead of raw strings.

### Image handling

`IImageService` / `ImageService` (scoped) manages product images stored under `wwwroot/Images/`.

### Cart persistence

The shopping cart is persisted in the **database** (`CarritoItems` table), not in session. Session is configured (30-minute idle timeout) but used only for minor UI state.

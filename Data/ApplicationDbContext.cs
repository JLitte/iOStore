using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using iOStore.Models;

namespace iOStore.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // ── DbSets ───────────────────────────────────────────────────
        public DbSet<Producto>          Productos           { get; set; }
        public DbSet<Categoria>         Categorias          { get; set; }
        public DbSet<ProductoCategoria> ProductoCategorias  { get; set; }
        public DbSet<Pedido>            Pedidos             { get; set; }
        public DbSet<PedidoDetalle>     PedidoDetalles      { get; set; }
        public DbSet<CarritoItem>       CarritoItems        { get; set; }
        public DbSet<ProductoImagen>    ProductoImagenes    { get; set; }
        public DbSet<PedidoMovimiento>  PedidoMovimientos   { get; set; }
        public DbSet<ContactoPedido>    ContactoPedidos     { get; set; }
        public DbSet<TarifaEnvio>             TarifasEnvio              { get; set; }
        public DbSet<MetodoPago>              MetodosPago               { get; set; }
        public DbSet<NotificacionPedido>      NotificacionesPedido      { get; set; }
        public DbSet<ConfiguracionNotificacion> ConfiguracionNotificaciones { get; set; }
        public DbSet<PedidoEdicion>           PedidoEdiciones           { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── Producto ──────────────────────────────────────────
            builder.Entity<Producto>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.TipoProducto).IsRequired().HasMaxLength(100);
                e.Property(p => p.Modelo).IsRequired().HasMaxLength(100);
                e.Property(p => p.Precio).HasColumnType("decimal(18,2)");
                e.HasOne(p => p.PromocionMetodoPago)
                 .WithMany()
                 .HasForeignKey(p => p.PromocionMetodoPagoId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            // ── Categoria ─────────────────────────────────────────
            builder.Entity<Categoria>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Nombre).IsRequired().HasMaxLength(80);
            });

            // ── ProductoCategoria (M:N explícita) ─────────────────
            builder.Entity<ProductoCategoria>(e =>
            {
                e.HasKey(pc => new { pc.ProductoId, pc.CategoriaId });
                e.HasOne(pc => pc.Producto).WithMany(p => p.ProductoCategorias)
                 .HasForeignKey(pc => pc.ProductoId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(pc => pc.Categoria).WithMany(c => c.ProductoCategorias)
                 .HasForeignKey(pc => pc.CategoriaId).OnDelete(DeleteBehavior.Restrict);
            });

            // ── Pedido ────────────────────────────────────────────
            builder.Entity<Pedido>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.EstadoActual).IsRequired().HasConversion<int>();
                e.Property(p => p.Total).HasColumnType("decimal(18,2)");
                e.Property(p => p.CostoEnvio).HasColumnType("decimal(18,2)");
                e.Property(p => p.RecargoAplicado).HasColumnType("decimal(18,2)");
                e.Property(p => p.TotalConRecargo).HasColumnType("decimal(18,2)");
                e.Property(p => p.DireccionEnvio).HasMaxLength(200);
                e.Property(p => p.Observaciones).HasMaxLength(1000);
                e.Property(p => p.NumeroSeguimiento).HasMaxLength(20);
                e.Property(p => p.NombreCliente).HasMaxLength(100);
                e.Property(p => p.EmailCliente).HasMaxLength(256);
                e.Property(p => p.TelefonoCliente).HasMaxLength(20);
                e.Property(p => p.CodigoPostal).HasMaxLength(10);
                e.Property(p => p.TransportistaSeleccionado).HasMaxLength(50);
                e.Property(p => p.ReferenciaPago).HasMaxLength(100);
                e.Property(p => p.TipoMonedaPago).HasMaxLength(20);
                e.Property(p => p.TipoCambioAplicado).HasColumnType("decimal(18,4)");
                e.Property(p => p.PrecioFinalUSD).HasColumnType("decimal(18,2)");
                e.Property(p => p.PrecioFinalARS).HasColumnType("decimal(18,2)");
                e.Property(p => p.RecargoAplicadoPorc).HasColumnType("decimal(5,2)");
                e.Property(p => p.CostoEnvioAdmin).HasColumnType("decimal(18,2)").IsRequired(false);
                e.Property(p => p.NotaEnvioAdmin).HasMaxLength(500).IsRequired(false);

                e.HasOne(p => p.Usuario).WithMany(u => u.Pedidos)
                 .HasForeignKey(p => p.UsuarioId).OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.MetodoPago).WithMany(m => m.Pedidos)
                 .HasForeignKey(p => p.MetodoPagoId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
            });

            // ── PedidoDetalle ─────────────────────────────────────
            builder.Entity<PedidoDetalle>(e =>
            {
                e.HasKey(pd => pd.Id);
                e.Property(pd => pd.PrecioUnitario).HasColumnType("decimal(18,2)");
                e.Ignore(pd => pd.Subtotal);
                e.HasOne(pd => pd.Pedido).WithMany(p => p.PedidoDetalles)
                 .HasForeignKey(pd => pd.PedidoId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(pd => pd.Producto).WithMany(p => p.PedidoDetalles)
                 .HasForeignKey(pd => pd.ProductoId).OnDelete(DeleteBehavior.Restrict);
            });

            // ── CarritoItem ───────────────────────────────────────
            builder.Entity<CarritoItem>(e =>
            {
                e.HasKey(ci => ci.Id);
                e.HasOne(ci => ci.Usuario).WithMany(u => u.CarritoItems)
                 .HasForeignKey(ci => ci.UsuarioId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(ci => ci.Producto).WithMany(p => p.CarritoItems)
                 .HasForeignKey(ci => ci.ProductoId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(ci => new { ci.UsuarioId, ci.ProductoId }).IsUnique();
            });

            // ── ApplicationUser ───────────────────────────────────
            builder.Entity<ApplicationUser>(e =>
            {
                e.Property(u => u.NombreCompleto).IsRequired().HasMaxLength(100);
                e.Property(u => u.Direccion).HasMaxLength(200);
                e.Property(u => u.Telefono).HasMaxLength(20);
            });

            // ── ProductoImagen ────────────────────────────────────
            builder.Entity<ProductoImagen>(e =>
            {
                e.HasKey(pi => pi.Id);
                e.Property(pi => pi.Url).IsRequired().HasMaxLength(500);
                e.HasOne(pi => pi.Producto).WithMany(p => p.Imagenes)
                 .HasForeignKey(pi => pi.ProductoId).OnDelete(DeleteBehavior.Cascade);
                e.HasIndex(pi => new { pi.ProductoId, pi.Orden });
            });

            // ── PedidoMovimiento ──────────────────────────────────
            builder.Entity<PedidoMovimiento>(e =>
            {
                e.HasKey(pm => pm.Id);
                e.Property(pm => pm.EstadoAnterior).HasConversion<int>();
                e.Property(pm => pm.EstadoNuevo).HasConversion<int>();
                e.Property(pm => pm.Observacion).HasMaxLength(500);
                e.HasOne(pm => pm.Pedido).WithMany(p => p.Movimientos)
                 .HasForeignKey(pm => pm.PedidoId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(pm => pm.Empleado).WithMany(u => u.MovimientosHechos)
                 .HasForeignKey(pm => pm.EmpleadoId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(pm => pm.PedidoId);
                e.HasIndex(pm => pm.EmpleadoId);
                e.HasIndex(pm => pm.Fecha);
            });

            // ── ContactoPedido ────────────────────────────────────
            builder.Entity<ContactoPedido>(e =>
            {
                e.HasKey(cp => cp.Id);
                e.Property(cp => cp.Tipo).HasConversion<int>();
                e.Property(cp => cp.Observacion).HasMaxLength(500);
                e.HasOne(cp => cp.Pedido).WithMany(p => p.Contactos)
                 .HasForeignKey(cp => cp.PedidoId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(cp => cp.Empleado).WithMany(u => u.ContactosHechos)
                 .HasForeignKey(cp => cp.EmpleadoId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(cp => cp.PedidoId);
                e.HasIndex(cp => cp.EmpleadoId);
            });

            // ── TarifaEnvio ───────────────────────────────────────
            builder.Entity<TarifaEnvio>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Transportista).IsRequired().HasMaxLength(50);
                e.Property(t => t.ZonaDesde).IsRequired().HasMaxLength(10);
                e.Property(t => t.ZonaHasta).IsRequired().HasMaxLength(10);
                e.Property(t => t.Costo).HasColumnType("decimal(18,2)");
                e.HasIndex(t => new { t.Transportista, t.Activo });
            });

            // ── MetodoPago ────────────────────────────────────────
            builder.Entity<MetodoPago>(e =>
            {
                e.HasKey(m => m.Id);
                e.Property(m => m.Nombre).IsRequired().HasMaxLength(100);
                e.Property(m => m.Tipo).HasConversion<int>();
                e.Property(m => m.Banco).HasMaxLength(80);
                e.Property(m => m.RecargoPorc).HasColumnType("decimal(5,2)");
                e.Property(m => m.Descripcion).HasMaxLength(200);
                e.Property(m => m.LogoUrl).HasMaxLength(300);
                e.Property(m => m.TipoMoneda).IsRequired().HasMaxLength(20).HasDefaultValue("ARS");
                e.HasIndex(m => new { m.Activo, m.Orden });
            });

            // ── NotificacionPedido ────────────────────────────────
            builder.Entity<NotificacionPedido>(e =>
            {
                e.HasKey(n => n.Id);
                e.Property(n => n.TipoMensaje).IsRequired().HasMaxLength(50);
                e.Property(n => n.Destinatario).IsRequired().HasMaxLength(256);
                e.Property(n => n.Asunto).IsRequired().HasMaxLength(200);
                e.Property(n => n.Contenido).HasColumnType("nvarchar(max)");
                e.Property(n => n.ErrorDetalle).HasMaxLength(500);
                e.Property(n => n.EnviadoPorId).HasMaxLength(450);
                e.HasOne(n => n.Pedido).WithMany()
                 .HasForeignKey(n => n.PedidoId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(n => n.EnviadoPor).WithMany()
                 .HasForeignKey(n => n.EnviadoPorId).OnDelete(DeleteBehavior.SetNull).IsRequired(false);
                e.HasIndex(n => n.PedidoId);
                e.HasIndex(n => n.FechaIntento);
            });

            // ── ConfiguracionNotificacion ─────────────────────────
            builder.Entity<ConfiguracionNotificacion>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.SmtpHost).HasMaxLength(200);
                e.Property(c => c.SmtpUser).HasMaxLength(256);
                e.Property(c => c.SmtpPassword).HasMaxLength(500);
                e.Property(c => c.EmailRemitente).HasMaxLength(256);
                e.Property(c => c.NombreRemitente).IsRequired().HasMaxLength(100);
                e.Property(c => c.NombreEmpresa).IsRequired().HasMaxLength(100);
                e.Property(c => c.UrlTienda).HasMaxLength(300);
                e.Property(c => c.UrlSeguimiento).HasMaxLength(300);
                e.HasData(new ConfiguracionNotificacion
                {
                    Id               = 1,
                    SmtpPort         = 587,
                    SmtpUseSsl       = true,
                    NombreRemitente  = "iOStore",
                    NombreEmpresa    = "iOStore",
                    NotificarConfirmacion = true,
                    NotificarSeguimiento  = true,
                    NotificarEntregado    = true
                });
            });

            // ── PedidoEdicion ─────────────────────────────────────
            builder.Entity<PedidoEdicion>(e =>
            {
                e.HasKey(pe => pe.Id);
                e.Property(pe => pe.EditorId).IsRequired().HasMaxLength(450);
                e.Property(pe => pe.Campo).IsRequired().HasMaxLength(80);
                e.Property(pe => pe.ValorAnterior).HasMaxLength(500);
                e.Property(pe => pe.ValorNuevo).HasMaxLength(500);
                e.Property(pe => pe.Motivo).IsRequired().HasMaxLength(500);
                e.HasOne(pe => pe.Pedido).WithMany()
                 .HasForeignKey(pe => pe.PedidoId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(pe => pe.Editor).WithMany()
                 .HasForeignKey(pe => pe.EditorId).OnDelete(DeleteBehavior.Restrict);
                e.HasIndex(pe => pe.PedidoId);
                e.HasIndex(pe => pe.Fecha);
            });

            // ── Índices de performance ────────────────────────────
            builder.Entity<Producto>(e =>
            {
                e.HasIndex(p => p.Activo);
                e.HasIndex(p => p.Modelo);
                e.HasIndex(p => p.TipoProducto);
                e.HasIndex(p => p.FechaCreacion);
                e.HasIndex(p => new { p.Activo, p.FechaCreacion });
            });

            builder.Entity<Pedido>(e =>
            {
                e.HasIndex(p => p.EstadoActual);
                e.HasIndex(p => p.FechaPedido);
                e.HasIndex(p => p.UsuarioId);
                e.HasIndex(p => p.NumeroSeguimiento).IsUnique().HasFilter("[NumeroSeguimiento] IS NOT NULL");
                e.HasIndex(p => new { p.FechaPedido, p.EstadoActual });
            });

            // ── Data Seeding ──────────────────────────────────────
            SeedData(builder);
        }

        private static void SeedData(ModelBuilder builder)
        {
            builder.Entity<Categoria>().HasData(
                new Categoria { Id = 1, Nombre = "iPhone",      Descripcion = "Teléfonos Apple iPhone",         Icono = "bi-phone",      Activa = true },
                new Categoria { Id = 2, Nombre = "iPad",        Descripcion = "Tablets Apple iPad",             Icono = "bi-tablet",     Activa = true },
                new Categoria { Id = 3, Nombre = "Mac",         Descripcion = "Computadoras Apple Mac",         Icono = "bi-laptop",     Activa = true },
                new Categoria { Id = 4, Nombre = "Apple Watch", Descripcion = "Relojes inteligentes Apple",     Icono = "bi-smartwatch", Activa = true },
                new Categoria { Id = 5, Nombre = "Accesorios",  Descripcion = "Accesorios y periféricos Apple", Icono = "bi-headphones", Activa = true }
            );

            // Seed de métodos de pago (Argentina)
            builder.Entity<MetodoPago>().HasData(
                // ── Pesos argentinos ──
                new MetodoPago { Id = 1,  Nombre = "Efectivo ARS",                    Tipo = TipoMetodoPago.Efectivo,         Cuotas = 1,  RecargoPorc = 0m,  Descripcion = "Pago en efectivo en pesos al retirar",             Activo = true, Orden = 1,  TipoMoneda = "ARS"           },
                new MetodoPago { Id = 2,  Nombre = "Transferencia bancaria",           Tipo = TipoMetodoPago.Transferencia,    Cuotas = 1,  RecargoPorc = 0m,  Descripcion = "Transferencia / depósito bancario en pesos",       Activo = true, Orden = 2,  TipoMoneda = "ARS"           },
                new MetodoPago { Id = 3,  Nombre = "MercadoPago",                      Tipo = TipoMetodoPago.BilleteraDigital, Cuotas = 1,  RecargoPorc = 0m,  Descripcion = "Pago con billetera MercadoPago",                   Activo = true, Orden = 3,  TipoMoneda = "ARS"           },
                new MetodoPago { Id = 4,  Nombre = "Tarjeta de débito",                Tipo = TipoMetodoPago.Debito,           Cuotas = 1,  RecargoPorc = 0m,  Descripcion = "Débito bancario, todos los bancos",                Activo = true, Orden = 4,  TipoMoneda = "ARS"           },
                new MetodoPago { Id = 5,  Nombre = "Crédito 1 cuota",                  Tipo = TipoMetodoPago.Credito,          Cuotas = 1,  RecargoPorc = 0m,  Descripcion = "1 cuota sin interés",                              Activo = true, Orden = 5,  TipoMoneda = "ARS"           },
                new MetodoPago { Id = 6,  Nombre = "Crédito 3 cuotas s/interés",       Tipo = TipoMetodoPago.Credito,          Cuotas = 3,  RecargoPorc = 0m,  Descripcion = "3 cuotas sin interés",                             Activo = true, Orden = 6,  TipoMoneda = "ARS"           },
                new MetodoPago { Id = 7,  Nombre = "Crédito 6 cuotas s/interés",       Tipo = TipoMetodoPago.Credito,          Cuotas = 6,  RecargoPorc = 0m,  Descripcion = "6 cuotas sin interés",                             Activo = true, Orden = 7,  TipoMoneda = "ARS"           },
                new MetodoPago { Id = 8,  Nombre = "Crédito 12 cuotas +15%",           Tipo = TipoMetodoPago.Credito,          Cuotas = 12, RecargoPorc = 15m, Descripcion = "12 cuotas con 15% de interés total",               Activo = true, Orden = 8,  TipoMoneda = "ARS"           },
                new MetodoPago { Id = 9,  Nombre = "Crédito 18 cuotas +25%",           Tipo = TipoMetodoPago.Credito,          Cuotas = 18, RecargoPorc = 25m, Descripcion = "18 cuotas con 25% de interés total",               Activo = true, Orden = 9,  TipoMoneda = "ARS"           },
                new MetodoPago { Id = 10, Nombre = "Crédito 24 cuotas +35%",           Tipo = TipoMetodoPago.Credito,          Cuotas = 24, RecargoPorc = 35m, Descripcion = "24 cuotas con 35% de interés total",               Activo = true, Orden = 10, TipoMoneda = "ARS"           },
                // ── Dólares ──
                new MetodoPago { Id = 11, Nombre = "Efectivo USD (cara grande)",       Tipo = TipoMetodoPago.Efectivo,         Cuotas = 1,  RecargoPorc = 0m,  Descripcion = "Billetes USD cotización blue",                     Activo = true, Orden = 11, TipoMoneda = "USD_CaraGrande" },
                new MetodoPago { Id = 12, Nombre = "Efectivo USD (cara chica)",        Tipo = TipoMetodoPago.Efectivo,         Cuotas = 1,  RecargoPorc = 5m,  Descripcion = "Billetes USD pequeños — precio blue +5%",          Activo = true, Orden = 12, TipoMoneda = "USD_CaraChica"  },
                new MetodoPago { Id = 13, Nombre = "Tarjeta de crédito USD",           Tipo = TipoMetodoPago.Credito,          Cuotas = 1,  RecargoPorc = 0m,  Descripcion = "Cargo en dólares a cotización tarjeta, 1 cuota",   Activo = true, Orden = 13, TipoMoneda = "USD_Tarjeta"    }
            );
        }

        // ── Stored Procedures ─────────────────────────────────────────
        /// <summary>sp_ProductosMasVendidos — 1 resultset.</summary>
        public async Task<List<ProductoMasVendidoDto>> GetProductosMasVendidosAsync(
            DateTime desde, DateTime hasta)
        {
            return await this.Database
                .SqlQueryRaw<ProductoMasVendidoDto>(
                    "EXEC sp_ProductosMasVendidos @Desde = {0}, @Hasta = {1}",
                    desde, hasta)
                .ToListAsync();
        }

        /// <summary>sp_EstadisticasEnvio — 2 resultsets: CPs | Modalidades.</summary>
        public async Task<(List<EstadisticaEnvioDto> CPs, List<ModalidadEnvioDto> Modalidades)>
            GetEstadisticasEnvioAsync(DateTime desde, DateTime hasta)
        {
            var cps       = new List<EstadisticaEnvioDto>();
            var modalidades = new List<ModalidadEnvioDto>();

            var conn   = Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "EXEC sp_EstadisticasEnvio @Desde, @Hasta";
                var p1 = cmd.CreateParameter(); p1.ParameterName = "@Desde"; p1.Value = desde; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.ParameterName = "@Hasta"; p2.Value = hasta; cmd.Parameters.Add(p2);

                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    cps.Add(new EstadisticaEnvioDto
                    {
                        CodigoPostal      = rdr.GetString(0),
                        CantidadPedidos   = rdr.GetInt32(1),
                        TotalCobradoEnvio = rdr.IsDBNull(2) ? 0m : rdr.GetDecimal(2)
                    });

                await rdr.NextResultAsync();
                while (await rdr.ReadAsync())
                    modalidades.Add(new ModalidadEnvioDto
                    {
                        Modalidad       = rdr.GetString(0),
                        CantidadPedidos = rdr.GetInt32(1),
                        TotalCobrado    = rdr.IsDBNull(2) ? 0m : rdr.GetDecimal(2),
                        PromedioEnvio   = rdr.IsDBNull(3) ? 0m : rdr.GetDecimal(3)
                    });
            }
            finally { if (!wasOpen) await conn.CloseAsync(); }

            return (cps, modalidades);
        }

        /// <summary>sp_EstadisticasPago — 2 resultsets: Métodos | Monedas.</summary>
        public async Task<(List<EstadisticaPagoDto> Metodos, List<MonedaDistribucionDto> Monedas)>
            GetEstadisticasPagoAsync(DateTime desde, DateTime hasta)
        {
            var metodos = new List<EstadisticaPagoDto>();
            var monedas = new List<MonedaDistribucionDto>();

            var conn    = Database.GetDbConnection();
            var wasOpen = conn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await conn.OpenAsync();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "EXEC sp_EstadisticasPago @Desde, @Hasta";
                var p1 = cmd.CreateParameter(); p1.ParameterName = "@Desde"; p1.Value = desde; cmd.Parameters.Add(p1);
                var p2 = cmd.CreateParameter(); p2.ParameterName = "@Hasta"; p2.Value = hasta; cmd.Parameters.Add(p2);

                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    metodos.Add(new EstadisticaPagoDto
                    {
                        MetodoPago      = rdr.GetString(0),
                        Moneda          = rdr.GetString(1),
                        CantidadPedidos = rdr.GetInt32(2),
                        TotalRecaudado  = rdr.IsDBNull(3) ? 0m : rdr.GetDecimal(3),
                        PromedioCuotas  = rdr.IsDBNull(4) ? 1m : (decimal)rdr.GetDouble(4),
                        MonedaTotal     = rdr.FieldCount > 5 && !rdr.IsDBNull(5) ? rdr.GetString(5) : "ARS"
                    });

                await rdr.NextResultAsync();
                while (await rdr.ReadAsync())
                    monedas.Add(new MonedaDistribucionDto
                    {
                        Moneda          = rdr.GetString(0),
                        CantidadPedidos = rdr.GetInt32(1),
                        TotalARS        = rdr.IsDBNull(2) ? 0m : rdr.GetDecimal(2),
                        TotalUSD        = rdr.IsDBNull(3) ? 0m : rdr.GetDecimal(3)
                    });
            }
            finally { if (!wasOpen) await conn.CloseAsync(); }

            return (metodos, monedas);
        }
    }

    // ── DTOs para Stored Procedures ──────────────────────────────────
    // Nombres de propiedad deben coincidir EXACTAMENTE con alias de columna del SP

    public class ProductoMasVendidoDto
    {
        public string  Producto          { get; set; } = string.Empty;
        public string  Modelo            { get; set; } = string.Empty;
        public int     UnidadesVendidas  { get; set; }
        public decimal TotalRecaudado    { get; set; }
        public int     CantidadPedidos   { get; set; }
    }

    public class EstadisticaEnvioDto
    {
        public string  CodigoPostal      { get; set; } = string.Empty;
        public int     CantidadPedidos   { get; set; }
        public decimal TotalCobradoEnvio { get; set; }
    }

    public class ModalidadEnvioDto
    {
        public string  Modalidad         { get; set; } = string.Empty;
        public int     CantidadPedidos   { get; set; }
        public decimal TotalCobrado      { get; set; }
        public decimal PromedioEnvio     { get; set; }
    }

    public class EstadisticaPagoDto
    {
        public string  MetodoPago        { get; set; } = string.Empty;
        public string  Moneda            { get; set; } = string.Empty;
        public int     CantidadPedidos   { get; set; }
        public decimal TotalRecaudado    { get; set; }
        public decimal PromedioCuotas    { get; set; }
        public string  MonedaTotal       { get; set; } = "ARS";  // 'USD' para billete, 'ARS' para el resto
    }

    public class MonedaDistribucionDto
    {
        public string  Moneda            { get; set; } = string.Empty;
        public int     CantidadPedidos   { get; set; }
        public decimal TotalARS          { get; set; }
        public decimal TotalUSD          { get; set; }
    }
}

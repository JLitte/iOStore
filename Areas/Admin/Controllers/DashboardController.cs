using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using iOStore.Data;
using iOStore.Helpers;
using iOStore.Models;
using iOStore.Services;

namespace iOStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrador,AdminEmpleado")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IClockService _clock;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IClockService clock)
        {
            _context     = context;
            _userManager = userManager;
            _clock       = clock;
        }

        public async Task<IActionResult> Index()
        {
            var vm = await BuildDashboardViewModelAsync(_clock.Today.AddDays(-30), _clock.Today);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Filtrar(DateTime desde, DateTime hasta)
        {
            var vm = await BuildDashboardViewModelAsync(desde, hasta);
            vm.Desde = desde;
            vm.Hasta = hasta;
            return View("Index", vm);
        }

        // ── AJAX: top productos con selector de rango ─────────────────
        [HttpGet]
        public async Task<IActionResult> TopProductosAjax(string rango = "mes")
        {
            var (desde, hasta) = RangoAFechas(rango);
            var hastaFin = hasta.AddDays(1).AddSeconds(-1);
            try
            {
                var datos = await _context.GetProductosMasVendidosAsync(desde, hastaFin);
                return Json(new { ok = true, datos });
            }
            catch
            {
                return Json(new { ok = false });
            }
        }

        // ── AJAX: estadísticas de envío ───────────────────────────────
        [HttpGet]
        public async Task<IActionResult> EstadisticasEnvioAjax(string rango = "mes")
        {
            var (desde, hasta) = RangoAFechas(rango);
            var hastaFin = hasta.AddDays(1).AddSeconds(-1);
            try
            {
                var (cps, modalidades) = await _context.GetEstadisticasEnvioAsync(desde, hastaFin);
                return Json(new { ok = true, cps, modalidades });
            }
            catch
            {
                return Json(new { ok = false });
            }
        }

        // ── AJAX: estadísticas de pago + ventas por moneda ───────────
        [HttpGet]
        public async Task<IActionResult> EstadisticasPagoAjax(string rango = "mes")
        {
            var (desde, hasta) = RangoAFechas(rango);
            var hastaFin = hasta.AddDays(1).AddSeconds(-1);
            try
            {
                var (metodos, monedas) = await _context.GetEstadisticasPagoAsync(desde, hastaFin);

                var ventasMoneda = await _context.Pedidos
                    .AsNoTracking()
                    .Include(p => p.MetodoPago)
                    .Where(p => p.FechaPedido >= desde && p.FechaPedido <= hastaFin
                                && p.EstadoActual != EstadoPedido.Devuelto
                                && p.EstadoActual != EstadoPedido.Cancelado)
                    .ToListAsync();

                var ventasMonedaAgrupado = ventasMoneda
                    .GroupBy(p => p.TipoMonedaPago ?? "ARS")
                    .Select(g => new
                    {
                        Moneda   = g.Key,
                        // ARS / USD_Tarjeta: crédito → Total (sin recargo bancario), otros → TotalConRecargo
                        Total    = g.Sum(p =>
                            p.MetodoPago?.Tipo == TipoMetodoPago.Credito
                                ? p.Total
                                : p.TotalConRecargo),
                        // USD billete: PrecioFinalUSD es el monto en dólares
                        // USD_Tarjeta / ARS: 0 (se muestra en Total, en pesos)
                        TotalUSD = g.Sum(p =>
                            (p.TipoMonedaPago == "USD_CaraGrande" || p.TipoMonedaPago == "USD_CaraChica")
                                ? (p.PrecioFinalUSD ?? p.TotalConRecargo)
                                : 0m),
                        Cantidad = g.Count()
                    })
                    .ToList();

                return Json(new { ok = true, metodos, monedas, ventasMoneda = ventasMonedaAgrupado });
            }
            catch
            {
                return Json(new { ok = false });
            }
        }

        // ── AJAX: últimos pedidos con selector de rango ───────────────
        [HttpGet]
        public async Task<IActionResult> UltimosPedidosAjax(string rango = "mes")
        {
            var (desde, hasta) = RangoAFechas(rango);
            var hastaFin = hasta.AddDays(1).AddSeconds(-1);

            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= desde && p.FechaPedido <= hastaFin)
                .OrderByDescending(p => p.FechaPedido)
                .Take(10)
                .Select(p => new
                {
                    p.Id,
                    p.NumeroSeguimiento,
                    p.NombreCliente,
                    // Misma lógica que "Total Recaudado": crédito → Total, resto → TotalConRecargo
                    Total          = p.MetodoPago != null && p.MetodoPago.Tipo == TipoMetodoPago.Credito
                                         ? p.Total
                                         : p.TotalConRecargo,
                    Estado         = FormatHelper.DisplayEstado(p.EstadoActual),
                    BadgeColor     = FormatHelper.BadgeEstado(p.EstadoActual),
                    Fecha          = p.FechaPedido.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            return Json(new { ok = true, datos = pedidos });
        }

        // ── Exportar Dashboard a Excel ────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> ExportarExcel(string rango = "mes")
        {
            var (desde, hasta) = RangoAFechas(rango);
            var hastaFin = hasta.AddDays(1).AddSeconds(-1);

            var vm                         = await BuildDashboardViewModelAsync(desde, hasta);
            var topProductos               = await _context.GetProductosMasVendidosAsync(desde, hastaFin);
            var (metodosPago, _)           = await _context.GetEstadisticasPagoAsync(desde, hastaFin);
            var (_, modalidadesEnvio)      = await _context.GetEstadisticasEnvioAsync(desde, hastaFin);

            var ventasMonedaRaw = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.MetodoPago)
                .Where(p => p.FechaPedido >= desde && p.FechaPedido <= hastaFin
                            && p.EstadoActual != EstadoPedido.Devuelto
                            && p.EstadoActual != EstadoPedido.Cancelado)
                .ToListAsync();

            var ventasMoneda = ventasMonedaRaw
                .GroupBy(p => p.TipoMonedaPago ?? "ARS")
                .Select(g => new
                {
                    Moneda   = g.Key,
                    Total    = g.Sum(p => p.MetodoPago?.Tipo == TipoMetodoPago.Credito ? p.Total : p.TotalConRecargo),
                    Cantidad = g.Count()
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            using var wb = new XLWorkbook();

            // ── Hoja 1: Últimos pedidos ───────────────────────────────
            var ws1 = wb.Worksheets.Add("Últimos pedidos");
            var h1 = new[] { "Nº Seguimiento", "Cliente", "Total", "Estado", "Fecha" };
            for (int c = 0; c < h1.Length; c++) ws1.Cell(1, c + 1).Value = h1[c];
            ws1.Row(1).Style.Font.Bold = true;
            int row = 2;
            foreach (var p in vm.UltimosPedidos)
            {
                ws1.Cell(row, 1).Value = p.NumeroSeguimiento;
                ws1.Cell(row, 2).Value = p.NombreCliente;
                ws1.Cell(row, 3).Value = (double)p.Total;
                ws1.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                ws1.Cell(row, 4).Value = FormatHelper.DisplayEstado(p.EstadoActual);
                ws1.Cell(row, 5).Value = p.FechaPedido.ToString("dd/MM/yyyy HH:mm");
                row++;
            }
            ws1.Columns().AdjustToContents();

            // ── Hoja 2: Métodos de pago ───────────────────────────────
            var ws2 = wb.Worksheets.Add("Métodos de pago");
            var h2 = new[] { "Método", "Moneda", "Pedidos", "Total recaudado", "Cuotas promedio" };
            for (int c = 0; c < h2.Length; c++) ws2.Cell(1, c + 1).Value = h2[c];
            ws2.Row(1).Style.Font.Bold = true;
            row = 2;
            foreach (var m in metodosPago)
            {
                ws2.Cell(row, 1).Value = m.MetodoPago;
                ws2.Cell(row, 2).Value = m.MonedaTotal;
                ws2.Cell(row, 3).Value = m.CantidadPedidos;
                ws2.Cell(row, 4).Value = (double)m.TotalRecaudado;
                ws2.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                ws2.Cell(row, 5).Value = (double)m.PromedioCuotas;
                ws2.Cell(row, 5).Style.NumberFormat.Format = "0.0";
                row++;
            }
            ws2.Columns().AdjustToContents();

            // ── Hoja 3: Ventas por moneda ─────────────────────────────
            var ws3 = wb.Worksheets.Add("Ventas por moneda");
            var h3 = new[] { "Moneda", "Total", "Pedidos" };
            for (int c = 0; c < h3.Length; c++) ws3.Cell(1, c + 1).Value = h3[c];
            ws3.Row(1).Style.Font.Bold = true;
            row = 2;
            foreach (var v in ventasMoneda)
            {
                ws3.Cell(row, 1).Value = v.Moneda;
                ws3.Cell(row, 2).Value = (double)v.Total;
                ws3.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00";
                ws3.Cell(row, 3).Value = v.Cantidad;
                row++;
            }
            ws3.Columns().AdjustToContents();

            // ── Hoja 4: Top productos vendidos (SP) ───────────────────
            var ws4 = wb.Worksheets.Add("Top productos vendidos");
            var h4 = new[] { "#", "Producto", "Modelo", "Unidades", "Pedidos" };
            for (int c = 0; c < h4.Length; c++) ws4.Cell(1, c + 1).Value = h4[c];
            ws4.Row(1).Style.Font.Bold = true;
            row = 2;
            int rank = 1;
            foreach (var p in topProductos)
            {
                ws4.Cell(row, 1).Value = rank++;
                ws4.Cell(row, 2).Value = p.Producto;
                ws4.Cell(row, 3).Value = p.Modelo;
                ws4.Cell(row, 4).Value = p.UnidadesVendidas;
                ws4.Cell(row, 5).Value = p.CantidadPedidos;
                row++;
            }
            ws4.Columns().AdjustToContents();

            // ── Hoja 5: Pedidos por estado ────────────────────────────
            var ws5 = wb.Worksheets.Add("Pedidos por estado");
            ws5.Cell(1, 1).Value = "Estado";
            ws5.Cell(1, 2).Value = "Cantidad";
            ws5.Row(1).Style.Font.Bold = true;
            row = 2;
            foreach (var kv in vm.PedidosPorEstado)
            {
                ws5.Cell(row, 1).Value = kv.Key;
                ws5.Cell(row, 2).Value = kv.Value;
                row++;
            }
            ws5.Columns().AdjustToContents();

            // ── Hoja 6: Productos más vendidos (ViewModel) ────────────
            var ws6 = wb.Worksheets.Add("Productos más vendidos");
            var h6 = new[] { "#", "Producto", "Modelo", "Unidades", "Total USD" };
            for (int c = 0; c < h6.Length; c++) ws6.Cell(1, c + 1).Value = h6[c];
            ws6.Row(1).Style.Font.Bold = true;
            row = 2; rank = 1;
            foreach (var p in vm.ProductosVendidos)
            {
                ws6.Cell(row, 1).Value = rank++;
                ws6.Cell(row, 2).Value = p.TipoProducto;
                ws6.Cell(row, 3).Value = p.Modelo;
                ws6.Cell(row, 4).Value = p.CantidadVendida;
                ws6.Cell(row, 5).Value = (double)p.TotalVentas;
                ws6.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                row++;
            }
            ws6.Columns().AdjustToContents();

            // ── Hoja 7: Estadísticas de envío ─────────────────────────
            if (modalidadesEnvio.Count > 0)
            {
                var ws7 = wb.Worksheets.Add("Estadísticas de envío");
                var h7 = new[] { "Modalidad", "Pedidos", "Total cobrado", "Promedio envío" };
                for (int c = 0; c < h7.Length; c++) ws7.Cell(1, c + 1).Value = h7[c];
                ws7.Row(1).Style.Font.Bold = true;
                row = 2;
                foreach (var e in modalidadesEnvio)
                {
                    ws7.Cell(row, 1).Value = e.Modalidad;
                    ws7.Cell(row, 2).Value = e.CantidadPedidos;
                    ws7.Cell(row, 3).Value = (double)e.TotalCobrado;
                    ws7.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
                    ws7.Cell(row, 4).Value = (double)e.PromedioEnvio;
                    ws7.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
                    row++;
                }
                ws7.Columns().AdjustToContents();
            }

            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            string fecha   = _clock.Today.ToString("yyyyMMdd");
            string nombre  = $"Dashboard_iOStore_{rango}_{fecha}.xlsx";
            return File(ms.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nombre);
        }

        private async Task<DashboardViewModel> BuildDashboardViewModelAsync(DateTime desde, DateTime hasta)
        {
            var hastaFin = hasta.AddDays(1).AddSeconds(-1);
            var hoy      = _clock.Today;
            var hoyFin   = hoy.AddDays(1).AddSeconds(-1);
            var mesFin   = new DateTime(hoy.Year, hoy.Month, 1).AddMonths(1).AddSeconds(-1);
            var mesInicio = new DateTime(hoy.Year, hoy.Month, 1);

            // ── KPIs ──────────────────────────────────────────────────
            var totalPedidos = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= desde && p.FechaPedido <= hastaFin)
                .CountAsync();

            // 4a+4c: solo Entregado (= 5); 4c: crédito → Total (banco retiene recargo),
            //        no-crédito → TotalConRecargo (tienda retiene recargo) — igual que sp_EstadisticasPago
            var totalVentas = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= desde && p.FechaPedido <= hastaFin
                            && p.EstadoActual == EstadoPedido.Entregado)
                .Select(p => (decimal?)(
                    p.MetodoPago != null && p.MetodoPago.Tipo == TipoMetodoPago.Credito
                        ? p.Total
                        : p.TotalConRecargo))
                .SumAsync() ?? 0;

            var productosActivos = await _context.Productos.AsNoTracking().CountAsync(p => p.Activo);
            var totalClientes    = (await _userManager.GetUsersInRoleAsync(Roles.Cliente)).Count;

            var ventasHoy = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= hoy && p.FechaPedido <= hoyFin
                            && p.EstadoActual == EstadoPedido.Entregado)
                .Select(p => (decimal?)(
                    p.MetodoPago != null && p.MetodoPago.Tipo == TipoMetodoPago.Credito
                        ? p.Total
                        : p.TotalConRecargo))
                .SumAsync() ?? 0;

            var ventasMes = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= mesInicio && p.FechaPedido <= mesFin
                            && p.EstadoActual == EstadoPedido.Entregado)
                .Select(p => (decimal?)(
                    p.MetodoPago != null && p.MetodoPago.Tipo == TipoMetodoPago.Credito
                        ? p.Total
                        : p.TotalConRecargo))
                .SumAsync() ?? 0;

            var pedidosPendientes = await _context.Pedidos
                .AsNoTracking()
                .CountAsync(p => p.EstadoActual == EstadoPedido.Pendiente);

            var pedidosVencidos = await _context.Pedidos
                .AsNoTracking()
                .CountAsync(p => p.EstadoActual == EstadoPedido.Pendiente
                              && p.FechaLimitePago != null
                              && p.FechaLimitePago < _clock.Now);

            var stockBajoCount = await _context.Productos
                .AsNoTracking()
                .CountAsync(p => p.Activo && p.Stock <= 5);

            // ── Ventas por divisa (período, solo Entregado) ───────────
            var ventasARSP = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= desde && p.FechaPedido <= hastaFin
                            && p.EstadoActual == EstadoPedido.Entregado
                            && (p.TipoMonedaPago == null || p.TipoMonedaPago == "ARS"))
                .Select(p => (decimal?)(
                    p.MetodoPago != null && p.MetodoPago.Tipo == TipoMetodoPago.Credito
                        ? p.Total : p.TotalConRecargo))
                .SumAsync() ?? 0;

            var ventasUSDCGP = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= desde && p.FechaPedido <= hastaFin
                            && p.EstadoActual == EstadoPedido.Entregado
                            && p.TipoMonedaPago == "USD_CaraGrande")
                .Select(p => (decimal?)(p.PrecioFinalUSD ?? p.TotalConRecargo))
                .SumAsync() ?? 0;

            var ventasUSDCCP = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= desde && p.FechaPedido <= hastaFin
                            && p.EstadoActual == EstadoPedido.Entregado
                            && p.TipoMonedaPago == "USD_CaraChica")
                .Select(p => (decimal?)(p.PrecioFinalUSD ?? p.TotalConRecargo))
                .SumAsync() ?? 0;

            var ventasUSDTP = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= desde && p.FechaPedido <= hastaFin
                            && p.EstadoActual == EstadoPedido.Entregado
                            && p.TipoMonedaPago == "USD_Tarjeta")
                .Select(p => (decimal?)(
                    p.MetodoPago != null && p.MetodoPago.Tipo == TipoMetodoPago.Credito
                        ? p.Total : p.TotalConRecargo))
                .SumAsync() ?? 0;

            // ── Productos más vendidos (período) ──────────────────────
            // 4a: solo Entregado. TotalVentas usa precio base ARS; la versión con
            //     recargo proporcional está en sp_ProductosMasVendidos (usado por TopProductosAjax).
            var productosVendidos = await _context.PedidoDetalles
                .AsNoTracking()
                .Include(pd => pd.Pedido)
                .Include(pd => pd.Producto)
                .Where(pd => pd.Pedido.FechaPedido >= desde && pd.Pedido.FechaPedido <= hastaFin
                             && pd.Pedido.EstadoActual != EstadoPedido.Devuelto
                             && pd.Pedido.EstadoActual != EstadoPedido.Cancelado)
                .GroupBy(pd => new { pd.Producto.TipoProducto, pd.Producto.Modelo })
                .Select(g => new ProductoVendidoItem
                {
                    TipoProducto    = g.Key.TipoProducto,
                    Modelo          = g.Key.Modelo,
                    CantidadVendida = g.Sum(x => x.Cantidad),
                    TotalVentas     = g.Sum(x => x.Cantidad * x.PrecioUnitario)
                })
                .OrderByDescending(x => x.TotalVentas)
                .Take(10)
                .ToListAsync();

            // ── Productos con bajo stock ───────────────────────────────
            var productosStockBajo = await _context.Productos
                .AsNoTracking()
                .Where(p => p.Activo && p.Stock <= 5)
                .OrderBy(p => p.Stock)
                .Select(p => new ProductoBajoStockItem
                {
                    Id           = p.Id,
                    TipoProducto = p.TipoProducto,
                    Modelo       = p.Modelo,
                    Stock        = p.Stock
                })
                .ToListAsync();

            // ── Últimos 10 pedidos ────────────────────────────────────
            var ultimosPedidos = await _context.Pedidos
                .AsNoTracking()
                .OrderByDescending(p => p.FechaPedido)
                .Take(10)
                .Select(p => new UltimoPedidoItem
                {
                    Id               = p.Id,
                    NumeroSeguimiento = p.NumeroSeguimiento ?? $"#{p.Id}",
                    NombreCliente    = p.NombreCliente ?? "—",
                    // Misma lógica que "Total Recaudado": crédito → Total (banco retiene recargo),
                    // resto → TotalConRecargo (igual que sp_EstadisticasPago)
                    Total            = p.MetodoPago != null && p.MetodoPago.Tipo == TipoMetodoPago.Credito
                                           ? p.Total
                                           : p.TotalConRecargo,
                    EstadoActual     = p.EstadoActual,
                    FechaPedido      = p.FechaPedido
                })
                .ToListAsync();

            // ── Antigüedad de empleados ────────────────────────────────
            var empleados       = await _userManager.GetUsersInRoleAsync(Roles.AdminEmpleado);
            var administradores = await _userManager.GetUsersInRoleAsync(Roles.Administrador);
            var todosEmpleados  = empleados.Concat(administradores)
                .Where(u => u.FechaIncorporacion.HasValue && u.Activo)
                .Select(u => new AntiguedadEmpleadoItem
                {
                    Nombre             = u.NombreCompleto,
                    Email              = u.Email ?? "",
                    FechaIncorporacion = u.FechaIncorporacion!.Value,
                    Antiguedad         = FormatHelper.TiempoTranscurrido(u.FechaIncorporacion.Value)
                })
                .OrderBy(x => x.FechaIncorporacion)
                .ToList();

            // ── Pedidos por estado ─────────────────────────────────────
            var pedidosPorEstadoRaw = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= desde && p.FechaPedido <= hastaFin)
                .GroupBy(p => p.EstadoActual)
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            // ── Ventas por día ─────────────────────────────────────────
            // 4a+4c: mismo filtro y fórmula que los cards de totales
            var ventasPorDia = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.FechaPedido >= desde && p.FechaPedido <= hastaFin
                            && p.EstadoActual != EstadoPedido.Devuelto
                            && p.EstadoActual != EstadoPedido.Cancelado)
                .Select(p => new
                {
                    Fecha = p.FechaPedido.Date,
                    Monto = p.MetodoPago != null && p.MetodoPago.Tipo == TipoMetodoPago.Credito
                                ? p.Total
                                : p.TotalConRecargo
                })
                .GroupBy(x => x.Fecha)
                .Select(g => new { Fecha = g.Key, Total = g.Sum(x => x.Monto) })
                .OrderBy(x => x.Fecha)
                .ToListAsync();

            // ── Productos ingresados ───────────────────────────────────
            var productosIngresados = await _context.Productos
                .AsNoTracking()
                .Where(p => p.FechaCreacion >= desde && p.FechaCreacion <= hastaFin)
                .CountAsync();

            return new DashboardViewModel
            {
                Desde                 = desde,
                Hasta                 = hasta,
                TotalPedidosPeriodo   = totalPedidos,
                TotalVentasPeriodo    = totalVentas,
                VentasARSPeriodo      = ventasARSP,
                VentasUSDCGPeriodo    = ventasUSDCGP,
                VentasUSDCCPeriodo    = ventasUSDCCP,
                VentasUSDTPeriodo     = ventasUSDTP,
                ProductosActivos      = productosActivos,
                TotalClientes         = totalClientes,
                VentasHoy             = ventasHoy,
                VentasMes             = ventasMes,
                PedidosPendientes     = pedidosPendientes,
                PedidosVencidosCount  = pedidosVencidos,
                StockBajoCount        = stockBajoCount,
                ProductosVendidos     = productosVendidos,
                ProductosIngresados   = productosIngresados,
                ProductosBajoStock    = productosStockBajo,
                UltimosPedidos        = ultimosPedidos,
                AntiguedadEmpleados   = todosEmpleados,
                PedidosPorEstado      = pedidosPorEstadoRaw.ToDictionary(
                                            x => FormatHelper.DisplayEstado(x.Estado),
                                            x => x.Cantidad),
                VentasPorDia          = ventasPorDia.ToDictionary(
                                            x => x.Fecha.ToString("dd/MM"),
                                            x => x.Total)
            };
        }

        private (DateTime desde, DateTime hasta) RangoAFechas(string rango)
        {
            var hoy = _clock.Today;
            return rango switch
            {
                "hoy"    => (hoy, hoy),
                "semana" => (hoy.AddDays(-6), hoy),
                "ano"    => (new DateTime(hoy.Year, 1, 1), hoy),
                _        => (new DateTime(hoy.Year, hoy.Month, 1), hoy)  // "mes" default
            };
        }
    }

    // ── ViewModels de Dashboard ───────────────────────────────────────────
    public class DashboardViewModel
    {
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int     TotalPedidosPeriodo  { get; set; }
        public decimal TotalVentasPeriodo   { get; set; }
        // Ventas por divisa (solo Entregado, sin interés bancario para crédito)
        public decimal VentasARSPeriodo     { get; set; }
        public decimal VentasUSDCGPeriodo   { get; set; }   // en USD
        public decimal VentasUSDCCPeriodo   { get; set; }   // en USD
        public decimal VentasUSDTPeriodo    { get; set; }   // en ARS
        public int     ProductosActivos     { get; set; }
        public int     TotalClientes        { get; set; }
        public decimal VentasHoy            { get; set; }
        public decimal VentasMes            { get; set; }
        public int     PedidosPendientes    { get; set; }
        public int     PedidosVencidosCount { get; set; }
        public int     StockBajoCount       { get; set; }
        public int     ProductosIngresados  { get; set; }

        public List<ProductoVendidoItem>    ProductosVendidos   { get; set; } = new();
        public List<ProductoBajoStockItem>  ProductosBajoStock  { get; set; } = new();
        public List<UltimoPedidoItem>       UltimosPedidos      { get; set; } = new();
        public List<AntiguedadEmpleadoItem> AntiguedadEmpleados { get; set; } = new();
        public Dictionary<string, int>      PedidosPorEstado    { get; set; } = new();
        public Dictionary<string, decimal>  VentasPorDia        { get; set; } = new();
    }

    public class ProductoVendidoItem
    {
        public string  TipoProducto    { get; set; } = string.Empty;
        public string  Modelo          { get; set; } = string.Empty;
        public int     CantidadVendida { get; set; }
        public decimal TotalVentas     { get; set; }
    }

    public class ProductoBajoStockItem
    {
        public int    Id           { get; set; }
        public string TipoProducto { get; set; } = string.Empty;
        public string Modelo       { get; set; } = string.Empty;
        public int    Stock        { get; set; }
    }

    public class UltimoPedidoItem
    {
        public int           Id               { get; set; }
        public string        NumeroSeguimiento { get; set; } = string.Empty;
        public string        NombreCliente    { get; set; } = string.Empty;
        public decimal       Total            { get; set; }
        public EstadoPedido  EstadoActual     { get; set; }
        public DateTime      FechaPedido      { get; set; }
    }

    public class AntiguedadEmpleadoItem
    {
        public string   Nombre             { get; set; } = string.Empty;
        public string   Email              { get; set; } = string.Empty;
        public DateTime FechaIncorporacion { get; set; }
        public string   Antiguedad         { get; set; } = string.Empty;
    }
}

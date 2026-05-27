using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using iOStore.Data;
using iOStore.Models;
using Microsoft.EntityFrameworkCore;

namespace iOStore.Services
{
    public class FacturaService : IFacturaService
    {
        private readonly ApplicationDbContext _context;

        public FacturaService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<byte[]> GenerarOrdenCompraPdfAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Include(p => p.MetodoPago)
                .Include(p => p.PedidoDetalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == pedidoId)
                ?? throw new InvalidOperationException($"Pedido {pedidoId} no encontrado.");

            return Document.Create(doc => doc.Page(page => ComponerPagina(page, pedido)))
                           .GeneratePdf();
        }

        private static void ComponerPagina(PageDescriptor page, Pedido pedido)
        {
            decimal costoEnvio = pedido.CostoEnvioEfectivo;
            bool esUSD      = pedido.TipoMonedaPago is "USD_CaraGrande" or "USD_CaraChica" or "USDT";
            bool hayRecargo  = pedido.RecargoAplicadoPorc > 0 && !esUSD;
            bool envioGratis = pedido.EsEnvioGratis || costoEnvio == 0;

            // Monto del producto: para ARS, mostrar TotalConRecargo si hay recargo de cuotas
            decimal montoBase = esUSD
                ? (pedido.PrecioFinalUSD ?? pedido.TotalConRecargo)
                : (hayRecargo ? pedido.TotalConRecargo : pedido.Total);

            string totalProducto = esUSD
                ? $"USD {montoBase:N2}"
                : $"${montoBase:N0} ARS";

            string totalEnvio = envioGratis ? "GRATIS" : $"${costoEnvio:N0} ARS";

            string totalFinal = esUSD
                ? $"USD {montoBase:N2}" +
                  (costoEnvio > 0 ? $" + ${costoEnvio:N0} ARS envío" : "")
                : $"${(montoBase + (envioGratis ? 0 : costoEnvio)):N0} ARS";

            string nro = pedido.NumeroSeguimiento ?? $"#{pedido.Id}";

            // ── Estilos base ─────────────────────────────────────────
            static TextStyle Bold()   => TextStyle.Default.FontSize(11).Bold();
            static TextStyle Muted()  => TextStyle.Default.FontSize(10).FontColor("#6e6e73");
            static TextStyle Normal() => TextStyle.Default.FontSize(11);

            page.Size(PageSizes.A4);
            page.MarginHorizontal(1.5f, Unit.Centimetre);
            page.MarginVertical(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontFamily("Arial").FontSize(11).FontColor("#1d1d1f"));

            // ── Encabezado ────────────────────────────────────────────
            page.Header().BorderBottom(2).BorderColor("#1d1d1f").PaddingBottom(12).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("iO").Style(Bold().FontSize(20));
                    text.Span("Store").Style(Normal().FontSize(20).FontColor("#6e6e73"));
                });
                row.ConstantItem(180).AlignRight().Column(col =>
                {
                    col.Item().Text("Orden de compra").Style(Bold().FontSize(14));
                    col.Item().Text($"#{nro}").Style(Muted());
                    col.Item().Text(pedido.FechaPedido.ToString("dd/MM/yyyy HH:mm")).Style(Muted());
                });
            });

            // ── Contenido ─────────────────────────────────────────────
            page.Content().PaddingTop(16).Column(col =>
            {
                // Fila: datos cliente + envío
                col.Item().Row(row =>
                {
                    // Cliente
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("DATOS DEL CLIENTE").Style(Muted().LetterSpacing(0.06f));
                        c.Item().Height(6);
                        FilaInfo(c, "Nombre",    pedido.NombreCliente   ?? "—");
                        FilaInfo(c, "Email",     pedido.EmailCliente    ?? "—");
                        FilaInfo(c, "Teléfono",  pedido.TelefonoCliente ?? "—");
                    });

                    row.ConstantItem(12);

                    // Envío
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("DATOS DE ENVÍO").Style(Muted().LetterSpacing(0.06f));
                        c.Item().Height(6);
                        FilaInfo(c, "Dirección",    pedido.DireccionEnvio           ?? "—");
                        FilaInfo(c, "Código postal", pedido.CodigoPostal             ?? "—");
                        FilaInfo(c, "Transportista", pedido.TransportistaSeleccionado ?? "—");
                    });
                });

                col.Item().Height(16);

                // Tabla de productos
                col.Item().Text("PRODUCTOS").Style(Muted().LetterSpacing(0.06f));
                col.Item().Height(6);
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(4);
                        c.RelativeColumn(1);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                    });

                    // Cabecera
                    static IContainer CeldaHead(IContainer c) =>
                        c.Background("#f5f5f7").Padding(6);

                    table.Header(h =>
                    {
                        h.Cell().Element(CeldaHead).Text("Producto").Style(Muted());
                        h.Cell().Element(CeldaHead).AlignCenter().Text("Cant.").Style(Muted());
                        h.Cell().Element(CeldaHead).AlignRight().Text("Precio unit.").Style(Muted());
                        h.Cell().Element(CeldaHead).AlignRight().Text("Subtotal").Style(Muted());
                    });

                    static IContainer Celda(IContainer c) =>
                        c.BorderBottom(1).BorderColor("#f5f5f7").Padding(6);

                    foreach (var det in pedido.PedidoDetalles)
                    {
                        table.Cell().Element(Celda).Column(inner =>
                        {
                            inner.Item().Text($"{det.Producto?.TipoProducto}").Style(Bold());
                            inner.Item().Text(det.Producto?.Modelo ?? "").Style(Muted());
                        });
                        table.Cell().Element(Celda).AlignCenter().Text(det.Cantidad.ToString());
                        table.Cell().Element(Celda).AlignRight()
                             .Text($"${det.PrecioUnitario:N2}").Style(Muted());
                        table.Cell().Element(Celda).AlignRight()
                             .Text($"${det.Cantidad * det.PrecioUnitario:N2}").Style(Bold());
                    }
                });

                col.Item().Height(16);

                // Resumen de pago
                col.Item().Text("RESUMEN DEL PAGO").Style(Muted().LetterSpacing(0.06f));
                col.Item().Height(6);
                col.Item().Column(c =>
                {
                    FilaInfo(c, "Método de pago", pedido.MetodoPago?.Nombre ?? "—");
                    if (pedido.TipoCambioAplicado.HasValue && pedido.TipoCambioAplicado > 0)
                        FilaInfo(c, "Tipo de cambio", $"${pedido.TipoCambioAplicado:N0} ARS");
                    if (pedido.CuotasSeleccionadas > 1)
                        FilaInfo(c, "Cuotas", $"{pedido.CuotasSeleccionadas} cuotas");
                });

                col.Item().Height(12);

                // Totales
                col.Item().Background("#f5f5f7").Padding(14).Column(totCol =>
                {
                    if (hayRecargo)
                    {
                        // Desglose: precio base, recargo de cuotas, luego envío y total final
                        FilaTotalSimple(totCol, "Subtotal productos", $"${pedido.Total:N0} ARS");
                        FilaTotalSimple(totCol, $"Recargo financiero ({pedido.RecargoAplicadoPorc:0.##}%)", $"+${pedido.RecargoAplicado:N0} ARS");
                    }
                    else
                    {
                        FilaTotalSimple(totCol, "Subtotal productos", totalProducto);
                    }
                    FilaTotalSimple(totCol, "Costo de envío", totalEnvio,
                        envioGratis ? "#1d8348" : null);
                    totCol.Item().Height(8);
                    totCol.Item().BorderTop(1).BorderColor("#d1d1d6").Height(1);
                    totCol.Item().Height(8);
                    totCol.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Total a pagar").Style(Bold().FontSize(13));
                        row.AutoItem().Text(totalFinal).Style(Bold().FontSize(13));
                    });
                });

                col.Item().Height(12);

                // Badge de confirmación
                col.Item().AlignLeft().Background("#e8f5e9").Padding(6).PaddingHorizontal(14)
                          .Text("✓  Despacho confirmado")
                          .Style(Normal().FontSize(10).FontColor("#2e7d32").Bold());
            });

            // ── Pie de página ─────────────────────────────────────────
            page.Footer().BorderTop(1).BorderColor("#e5e5ea").PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("iOStore — Apple Premium Reseller Argentina · ")
                     .Style(Muted().FontSize(9));
                    t.Span(pedido.FechaPedido.Year.ToString()).Style(Muted().FontSize(9));
                });
                row.AutoItem().Text(t =>
                {
                    t.Span("Página ").Style(Muted().FontSize(9));
                    t.CurrentPageNumber().Style(Muted().FontSize(9));
                    t.Span(" de ").Style(Muted().FontSize(9));
                    t.TotalPages().Style(Muted().FontSize(9));
                });
            });
        }

        private static void FilaInfo(ColumnDescriptor col, string label, string valor)
        {
            col.Item().BorderBottom(1).BorderColor("#f5f5f7").PaddingVertical(4).Row(row =>
            {
                row.RelativeItem().Text(label)
                   .Style(TextStyle.Default.FontSize(10).FontColor("#6e6e73"));
                row.RelativeItem().AlignRight().Text(valor)
                   .Style(TextStyle.Default.FontSize(10).SemiBold());
            });
        }

        private static void FilaTotalSimple(
            ColumnDescriptor col, string label, string valor, string? color = null)
        {
            col.Item().PaddingVertical(3).Row(row =>
            {
                row.RelativeItem().Text(label)
                   .Style(TextStyle.Default.FontSize(11));
                row.AutoItem().Text(valor)
                   .Style(color != null
                       ? TextStyle.Default.FontSize(11).SemiBold().FontColor(color)
                       : TextStyle.Default.FontSize(11).SemiBold());
            });
        }
    }
}

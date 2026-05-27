using iOStore.Models;
public class CarritoItemViewModel
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Modelo { get; set; } = string.Empty;
    public string TipoProducto { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public int Cantidad { get; set; }
    public string? ImagenUrl { get; set; }
    public decimal Subtotal => Precio * Cantidad;
    public int Stock { get; set; }
    public bool Activo { get; set; }
}
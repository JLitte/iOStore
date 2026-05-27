using iOStore.Models;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class PedidoDetalle
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PedidoId { get; set; }

    [Required]
    public int ProductoId { get; set; }

    [Required]
    public int Cantidad { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PrecioUnitario { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal => Cantidad * PrecioUnitario;

    // Relaciones
    [ForeignKey("PedidoId")]
    public virtual Pedido Pedido { get; set; } = null!;

    [ForeignKey("ProductoId")]
    public virtual Producto Producto { get; set; } = null!;
    
}
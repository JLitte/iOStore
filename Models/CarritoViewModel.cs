// ViewModel para el carrito
namespace iOStore.Models;
public class CarritoViewModel
{
    public List<CarritoItemViewModel> Items { get; set; } = new List<CarritoItemViewModel>();
    public decimal Total => Items.Sum(x => x.Subtotal);
    public int TotalItems => Items.Sum(x => x.Cantidad);
}
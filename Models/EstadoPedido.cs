namespace iOStore.Models
{
    public enum EstadoPedido
    {
        Pendiente          = 0,
        EnTramite          = 1,
        Preparando         = 2,
        Despachado         = 3,
        EnCamino           = 4,
        Entregado          = 5,
        SolicitaDevolucion = 6,
        EnDevolucion       = 7,
        Devuelto           = 8,
        Cancelado          = 9
    }
}

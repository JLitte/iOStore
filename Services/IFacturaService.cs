namespace iOStore.Services
{
    public interface IFacturaService
    {
        Task<byte[]> GenerarOrdenCompraPdfAsync(int pedidoId);
    }
}

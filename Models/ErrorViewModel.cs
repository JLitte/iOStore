namespace iOStore.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public int StatusCode { get; set; } = 500;
        public string Message { get; set; } = "Ocurrió un error inesperado. Por favor intentá nuevamente.";
    }
}

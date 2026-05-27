namespace iOStore.Models
{
    /// <summary>
    /// Resultado de una operación de negocio que puede fallar con mensaje.
    /// </summary>
    public class Result<T>
    {
        public bool    IsSuccess { get; }
        public T?      Value     { get; }
        public string? Error     { get; }

        private Result(bool success, T? value, string? error)
        {
            IsSuccess = success;
            Value     = value;
            Error     = error;
        }

        public static Result<T> Success(T value)       => new(true,  value,   null);
        public static Result<T> Failure(string error)  => new(false, default, error);
    }
}

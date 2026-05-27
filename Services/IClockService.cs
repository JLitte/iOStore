namespace iOStore.Services
{
    public interface IClockService
    {
        DateTime Now   { get; }
        DateTime Today { get; }
    }
}

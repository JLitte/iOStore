using iOStore.Helpers;

namespace iOStore.Services
{
    public class ClockService : IClockService
    {
        public DateTime Now   => ArClock.Now;
        public DateTime Today => ArClock.Today;
    }
}

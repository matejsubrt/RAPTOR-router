namespace RAPTOR_Router.Extensions
{
    internal static class DateTimeExtensions
    {
        /// <summary>
        /// Creates a DateTime object by combining a DateOnly object with a TimeOnly object
        /// </summary>
        /// <param name="date">The date</param>
        /// <param name="time">The time</param>
        /// <returns>The combined DateTime</returns>
        public static DateTime FromDateAndTime(DateOnly date, TimeOnly time)
        {
            return new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
        }
    }

    internal static class TimeOnlyExtensions
    {
        /// <summary>
        /// Adds a number of seconds to a TimeOnly object.
        /// </summary>
        /// <param name="time">The time to add to</param>
        /// <param name="seconds">The number of seconds to add</param>
        /// <returns>The resulting TimeOnly object</returns>
        public static TimeOnly AddSeconds(this TimeOnly time, int seconds)
        {
            long newTicks = time.Ticks + (long)seconds * 10_000_000;
            if (newTicks < 0)
            {
                return new TimeOnly(TimeOnly.MaxValue.Ticks + newTicks);
            }
            long ticksPerDay = TimeSpan.TicksPerDay;
            return new TimeOnly(newTicks % ticksPerDay);
        }
    }

    internal class TimeComparator
    {
        private readonly Func<DateTime, DateTime, bool> _improvesTime;

        public TimeComparator(bool forward)
        {
            _improvesTime = forward ?
                (a, b) => a < b :
                (a, b) => a > b;
        }

        public bool ImprovesTime(DateTime a, DateTime b)
        {
            return _improvesTime(a, b);
        }

        public bool ImprovesOrEqualsTime(DateTime a, DateTime b)
        {
            return a == b || _improvesTime(a, b);
        }
    }
}

namespace RAPTOR_Router.Extensions
{
    public static class TimeOnlyExtensions
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
}

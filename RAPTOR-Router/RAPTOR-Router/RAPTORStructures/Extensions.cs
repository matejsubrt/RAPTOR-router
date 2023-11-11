using RAPTOR_Router.GTFSParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace RAPTOR_Router.RAPTORStructures
{
    internal static class ListExtensions
    {
        /// <summary>
        /// Extracts the list of stop ids from the specified list of GTFSStopTimes
        /// </summary>
        /// <param name="stopTimes">The list of GTFSStopTimes to be extracted from</param>
        /// <returns>List of the stop ids of stops present in the stop times list</returns>
        public static List<string> GetStopIds(this List<GTFSStopTime> stopTimes)
        {
            List<string> ids = new List<string>();
            foreach(var stopTime in stopTimes)
            {
                ids.Add(stopTime.StopId);
            }
            return ids;
        }
    }
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
        public static TimeOnly AddSeconds(this TimeOnly time, int seconds)
        {
            long newTicks = time.Ticks + ((long)seconds * 10_000_000);
            if(newTicks < 0)
            {
                return new TimeOnly(TimeOnly.MaxValue.Ticks + newTicks);
            }
            return new TimeOnly(newTicks);
        }
    }
}

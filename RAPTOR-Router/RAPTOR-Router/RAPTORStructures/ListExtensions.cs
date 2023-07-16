using RAPTOR_Router.GTFSParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
    internal static class ListExtensions
    {
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
        public static DateTime FromDateAndTime(DateOnly date, TimeOnly time)
        {
            return new DateTime(date.Year, date.Month, date.Day, time.Hour, time.Minute, time.Second);
        }
    }
}

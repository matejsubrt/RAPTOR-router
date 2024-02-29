using RAPTOR_Router.GTFSParsing;

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

    internal static class DistanceExtensions
    {
        const double latConst = 111113.9; //distance between latitudes of 1 degree
        const double lonConst50N = 71583; //distance between 2 longitude lines at 50 degrees north

        public static int DistanceBetween(double lat1, double lon1, double lat2, double lon2)
        {
            var d1 = lat1 * (Math.PI / 180.0);
            var num1 = lon1 * (Math.PI / 180.0);
            var d2 = lat2 * (Math.PI / 180.0);
            var num2 = lon2 * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
            return (int)(6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3))));
        }
        /// <summary>
        /// Finds the approximation of the distance between the GPS coordinates of the specified stops - works specifically for the 50th parallel (i.e. south of Prague)
        /// </summary>
        /// <remarks>Does NOT take the curvature of the earth into account. Only works well for coordinates near the 50th parallel. Simpler to compute than the real earth-surface distance.</remarks>
        /// <param name="stop1">The first stop</param>
        /// <param name="stop2">The second stop</param>
        /// <returns>The approximate distance between the stops, assuming they are both near the </returns>
        public static int SimplifiedDistanceBetween(double lat1, double lon1, double lat2, double lon2)
        {

            var lat1m = lat1 * latConst;
            var lon1m = lon1 * lonConst50N;
            var lat2m = lat2 * latConst;
            var lon2m = lon2 * lonConst50N;

            var result = (int)(Math.Sqrt((lat2m - lat1m) * (lat2m - lat1m) + (lon2m - lon1m) * (lon2m - lon1m)));
            return result;
        }

        public static bool TooFarInOneDirection(double lat1, double lon1, double lat2, double lon2, int maxMeters)
        {
            var latDiffMeters = Math.Abs(lat1 - lat2) * latConst;
            var lonDiffMeters = Math.Abs(lon1 - lon2) * lonConst50N;

            return (latDiffMeters > maxMeters || lonDiffMeters > maxMeters);
        }
    }
}

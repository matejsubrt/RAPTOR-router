using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
    /// <summary>
    /// Class representing a unique stop - i.e. for one stop name there can be multiple stops for different vehicle types/directions
    /// </summary>
    internal class Stop
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public double Lat { get; private set; }
        public double Lon { get; private set; }
        public List<Route> StopRoutes { get; private set; } = new List<Route>();
        public List<Transfer> Transfers { get; private set; } = new List<Transfer>();

        public Stop(string id, string name, double lat, double lon)
        {
            Id = id;
            Name = name;
            Lat = lat;
            Lon = lon;
        }
        public string ToString()
        {
            return Name + "  " + Id;
        }
        /// <summary>
        /// Finds the real earth-surface distance between the GPS coordinates of the specified stops
        /// </summary>
        /// <remarks>Takes the curvature of the earth into account. Takes longer to compute.</remarks>
        /// <param name="stop1">The first stop</param>
        /// <param name="stop2">The second stop</param>
        /// <returns>The real earth-surface distance between the stops</returns>
        public static int DistanceBetween(Stop stop1, Stop stop2)
        {
            var d1 = stop1.Lat * (Math.PI / 180.0);
            var num1 = stop1.Lon * (Math.PI / 180.0);
            var d2 = stop2.Lat * (Math.PI / 180.0);
            var num2 = stop2.Lon * (Math.PI / 180.0) - num1;
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
        public static int SimplifiedDistanceBetween(Stop stop1, Stop stop2)
        {
            const double latConst = 111113.9; //distance between latitudes of 1 degree
            const double lonConst50N = 71583; //distance between 2 longitude lines at 50 degrees north
            var lat1 = stop1.Lat * latConst;
            var lon1 = stop1.Lon * lonConst50N;
            var lat2 = stop2.Lat * latConst;
            var lon2 = stop2.Lon * lonConst50N;

            var result = (int)(Math.Sqrt((lat2 - lat1) * (lat2 - lat1) + (lon2 - lon1) * (lon2 - lon1)));
            return result;
        }
    }
}

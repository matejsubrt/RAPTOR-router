using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Interfaces;

namespace RAPTOR_Router.Extensions
{
    internal static class DistanceExtensions
    {
        const double latConst = 111113.9; //distance between latitudes of 1 degree
        const double lonConst50N = 71583; //distance between 2 longitude lines at 50 degrees north

        /// <summary>
        /// Calculates the real-world distance between two GPS coordinates
        /// </summary>
        /// <returns>The curvature-adjusted distance between the two points in meters</returns>
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
        /// Calculates the real-world distance between two RoutePoints
        /// </summary>
        /// <returns>The curvature-adjusted distance between the two points in meters</returns>
        public static int DistanceBetween(IRoutePoint rp1, IRoutePoint rp2)
        {
            return DistanceBetween(rp1.Coords.Lat, rp1.Coords.Lon, rp2.Coords.Lat, rp2.Coords.Lon);
        }
        /// <summary>
        /// Calculates the real-world distance between two GPS coordinates
        /// </summary>
        /// <returns>The curvature-adjusted distance between the two points in meters</returns>
        public static int DistanceBetween(Coordinates c1, Coordinates c2)
        {
            return DistanceBetween(c1.Lat, c1.Lon, c2.Lat, c2.Lon);
        }
        /// <summary>
        /// Calculates the approximate distance between two GPS coordinates - works specifically for the 50th parallel (i.e. south of Prague)
        /// </summary>
        /// <remarks>Does NOT take the curvature of the earth into account. ONLY works well for coordinates near the 50th parallel. Simpler and faster to compute than the real earth-surface distance. For exact distances, see {@link DistanceBetween}</remarks>
        /// <returns>The approximate distance between the two points in meters.</returns>
        public static int SimplifiedDistanceBetween(double lat1, double lon1, double lat2, double lon2)
        {

            var lat1m = lat1 * latConst;
            var lon1m = lon1 * lonConst50N;
            var lat2m = lat2 * latConst;
            var lon2m = lon2 * lonConst50N;

            var result = (int)Math.Sqrt((lat2m - lat1m) * (lat2m - lat1m) + (lon2m - lon1m) * (lon2m - lon1m));
            return result;
        }
        /// <summary>
        /// Calculates the approximate distance between two RoutePoints - works specifically for the 50th parallel (i.e. south of Prague)
        /// </summary>
        /// <remarks>Does NOT take the curvature of the earth into account. ONLY works well for coordinates near the 50th parallel. Simpler and faster to compute than the real earth-surface distance.</remarks>
        /// <returns>The approximate distance between the two points in meters.</returns>
        public static int SimplifiedDistanceBetween(IRoutePoint rp1, IRoutePoint rp2)
        {
            return SimplifiedDistanceBetween(rp1.Coords.Lat, rp1.Coords.Lon, rp2.Coords.Lat, rp2.Coords.Lon);
        }
        /// <summary>
        /// Calculates the approximate distance between two GPS coordinates - works specifically for the 50th parallel (i.e. south of Prague)
        /// </summary>
        /// <remarks>Does NOT take the curvature of the earth into account. ONLY works well for coordinates near the 50th parallel. Simpler and faster to compute than the real earth-surface distance.</remarks>
        /// <returns>The approximate distance between the two points in meters.</returns>
        public static int SimplifiedDistanceBetween(Coordinates c1, Coordinates c2)
        {
            return SimplifiedDistanceBetween(c1.Lat, c1.Lon, c2.Lat, c2.Lon);
        }

        /// <summary>
        /// Calculates whether two GPS coordinates are too far apart in one direction (N/S or E/W) to be worth calculating the real distance
        /// </summary>
        /// <param name="maxMeters">The maximal number of meters to not be too far apart.</param>
        /// <returns>Bool specifying whether the two points are further apart than maxMeters</returns>
        public static bool TooFarInOneDirection(double lat1, double lon1, double lat2, double lon2, int maxMeters)
        {
            var latDiffMeters = Math.Abs(lat1 - lat2) * latConst;
            var lonDiffMeters = Math.Abs(lon1 - lon2) * lonConst50N;

            return latDiffMeters > maxMeters || lonDiffMeters > maxMeters;
        }
        /// <summary>
        /// Calculates whether two RoutePoints are too far apart in one direction (N/S or E/W) to be worth calculating the real distance
        /// </summary>
        /// <param name="maxMeters">The maximal number of meters to not be too far apart.</param>
        /// <returns>Bool specifying whether the two points are further apart than maxMeters</returns>
        public static bool TooFarInOneDirection(IRoutePoint rp1, IRoutePoint rp2, int maxMeters)
        {
            return TooFarInOneDirection(rp1.Coords.Lat, rp1.Coords.Lon, rp2.Coords.Lat, rp2.Coords.Lon, maxMeters);
        }
        /// <summary>
        /// Calculates whether two GPS coordinates are too far apart in one direction (N/S or E/W) to be worth calculating the real distance
        /// </summary>
        /// <param name="maxMeters">The maximal number of meters to not be too far apart.</param>
        /// <returns>Bool specifying whether the two points are further apart than maxMeters</returns>
        public static bool TooFarInOneDirection(Coordinates c1, Coordinates c2, int maxMeters)
        {
            return TooFarInOneDirection(c1.Lat, c1.Lon, c2.Lat, c2.Lon, maxMeters);
        }
    }
}

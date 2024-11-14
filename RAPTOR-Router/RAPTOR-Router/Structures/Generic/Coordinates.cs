using Microsoft.VisualBasic.CompilerServices;

namespace RAPTOR_Router.Structures.Generic
{
    /// <summary>
    /// Struct representing GPS coordinates
    /// </summary>
    public struct Coordinates
    {
        /// <summary>
        /// Latitude
        /// </summary>
        public double Lat { get; }
        /// <summary>
        /// Longitude
        /// </summary>
        public double Lon { get; }

        /// <summary>
        /// Creates a new Coordinates object
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        public Coordinates(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;
        }

        public static bool operator==(Coordinates c1, Coordinates c2)
        {
            return c1.Lat == c2.Lat && c1.Lon == c2.Lon;
        }
        public static bool operator!=(Coordinates c1, Coordinates c2)
        {
            return c1.Lat != c2.Lat || c1.Lon != c2.Lon;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Coordinates other)
            {
                return this == other;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Lat, Lon);
        }
    }
}

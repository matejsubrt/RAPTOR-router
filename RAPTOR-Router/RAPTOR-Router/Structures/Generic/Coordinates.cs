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

        /// <summary>
        /// Compares two coordinates for equality
        /// </summary>
        /// <param name="c1">The first coordinates</param>
        /// <param name="c2">The second coordinates</param>
        /// <returns>Whether the 2 coordinates are identical</returns>
        public static bool operator==(Coordinates c1, Coordinates c2)
        {
            return c1.Lat == c2.Lat && c1.Lon == c2.Lon;
        }

        /// <summary>
        /// Compares two coordinates for inequality
        /// </summary>
        /// <param name="c1">The first coordinates</param>
        /// <param name="c2">The second coordinates</param>
        /// <returns>Whether the 2 coordinates are non-equal</returns>
        public static bool operator!=(Coordinates c1, Coordinates c2)
        {
            return c1.Lat != c2.Lat || c1.Lon != c2.Lon;
        }

        /// <summary>
        /// Compares this coordinates object with another object for equality
        /// </summary>
        /// <param name="obj">The other object</param>
        /// <returns>Whether the other object is a coordinates object and is equal to this</returns>
        public override bool Equals(object? obj)
        {
            if (obj is Coordinates other)
            {
                return this == other;
            }
            return false;
        }

        /// <summary>
        /// Gets a hash code for the coordinates object
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Lat, Lon);
        }

        /// <summary>
        /// Validates whether the coordinate values are correct
        /// </summary>
        /// <returns>Whether the coordinate values are correct</returns>
        public bool ValidateValue()
        {
            return Lat >= -90 && Lat <= 90 && Lon >= -180 && Lon <= 180;
        }
    }
}

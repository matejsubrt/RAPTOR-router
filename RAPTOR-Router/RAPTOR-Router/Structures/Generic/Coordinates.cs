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
    }
}

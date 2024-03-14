using RAPTOR_Router.Models.Results;

namespace RAPTOR_Router.RouteFinders
{
    /// <summary>
    /// An interface for all RouteFinder objects, which are used to solve the connection search problem
    /// </summary>
    public interface IRouteFinder
    {
        /// <summary>
        /// Solves the provided connection search problem (i.e. SearchModel) given by source and destination stop names
        /// </summary>
        /// <param name="sourceStop">The exact name of the source stop</param>
        /// <param name="destStop">The exact name of the destination stop</param>
        /// <param name="time">The departure/arrival date and time</param>
        /// <returns>The resulting best connection, null if none found</returns>
        SearchResult FindConnection(string sourceStop, string destStop, DateTime time);
        /// <summary>
        /// Solves the provided connection search problem (i.e. SearchModel) given by source and destination coordinates
        /// </summary>
        /// <param name="srcLat">Source latitude</param>
        /// <param name="srcLon">Source longitude</param>
        /// <param name="destLat">Destination latitude</param>
        /// <param name="destLon">Destination longitude</param>
        /// <param name="time">The departure/arrival date and time</param>
        /// <returns>The resulting best connection, null if none found</returns>
        SearchResult FindConnection(double srcLat, double srcLon, double destLat, double destLon, DateTime time);
    }
}

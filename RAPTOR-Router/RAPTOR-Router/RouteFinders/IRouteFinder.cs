using RAPTOR_Router.Models.Results;

namespace RAPTOR_Router.RouteFinders
{
    /// <summary>
    /// An interface to use for any router class - any class that is supposed to find a connection in a SearchModel
    /// </summary>
    public interface IRouteFinder
    {
        /// <summary>
        /// The function of a router, which solves the provided connection search problem (i.e. SearchModel)
        /// </summary>
        /// <param name="sourceStop">The exact name of the source stop</param>
        /// <param name="destStop">The exact name of the destination stop</param>
        /// <param name="departureTime">The departure date and time</param>
        /// <returns>The resulting best connection, null if one id found</returns>
        SearchResult FindConnection(string sourceStop, string destStop, DateTime departureTime);
    }
    public interface IBikeRouteFinder
    {
        /// <summary>
        /// The function of a router, which solves the provided connection search problem (i.e. SearchModel)
        /// </summary>
        /// <param name="sourceStop">The exact name of the source stop</param>
        /// <param name="destStop">The exact name of the destination stop</param>
        /// <param name="departureTime">The departure date and time</param>
        /// <returns>The resulting best connection, null if one id found</returns>
        SearchResult FindConnection(string sourceStop, string destStop, DateTime departureTime);
        SearchResult FindConnection(double srcLat, double srcLon, double destLat, double destLon, DateTime departureTime);
    }
}

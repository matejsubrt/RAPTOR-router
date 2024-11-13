using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Structures.Generic;

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
        /// Solves the provided connection search problem (i.e. SearchModel) given by source and destination stop names
        /// </summary>
        /// <param name="sourceStop">The exact name of the source stop</param>
        /// <param name="destStop">The exact name of the destination stop</param>
        /// <param name="time">The departure/arrival date and time</param>
        /// <returns>The resulting best connection, null if none found</returns>
        List<SearchResult>? FindConnectionWithAlternatives(string sourceStopName, string destStopName, DateTime time);

        List<SearchResult>? FindConnectionWithAlternatives(Coordinates srcCoords, Coordinates destCoords, DateTime time);

        List<SearchResult>? FindConnectionWithAlternatives(Coordinates srcCoords, string destStopName, DateTime time);

        List<SearchResult>? FindConnectionWithAlternatives(string sourceStopName, Coordinates destCoords, DateTime time);

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

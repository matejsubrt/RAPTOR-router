using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Requests;

namespace RAPTOR_Router.RouteFinders
{
    /// <summary>
    /// An interface for RangeRouteFinder objects, which are used to solve the connection search problem within a time range
    /// </summary>
    public interface IRangeRouteFinder
    {
        /// <summary>
        /// Finds the best connections within a range according to the connection request object
        /// </summary>
        /// <param name="request">The connection request object</param>
        /// <returns>The connection response object, including the result and the error information</returns>
        Task<ConnectionApiResponseResult> FindConnectionsAsync(ConnectionRequest request);
    }

    /// <summary>
    /// An interface for RouteFinder objects, which are used to solve the simple (single) connection search problem
    /// </summary>
    public interface ISimpleRouteFinder
    {
        /// <summary>
        /// Finds the best connection(s) according to the connection request object
        /// </summary>
        /// <param name="request">The connection request object</param>
        /// <returns>The connection response object, including the result and the error information</returns>
        ConnectionApiResponseResult FindConnection(ConnectionRequest request);
    }

    /// <summary>
    /// An interface for a routing provider (i.e. a class that can solve connection search problems and provide this functionality to a range/other route finder)
    /// </summary>
    public interface ISimpleRoutingProvider
    {
        /// <summary>
        /// Finds the best connection(s) between the 2 stops using their names
        /// </summary>
        /// <param name="srcStopName">The name of the source stop (exact)</param>
        /// <param name="destStopName">The name of the destination stop (exact)</param>
        /// <param name="searchBeginTime">The time at which the search starts (i.e. departure time if this is a forward search, arrival date otherwise</param>
        /// <param name="includeViableAlternatives">Whether to also include connections with different number of trips than the best one found, assuming they do not differ much in quality.</param>
        /// <returns>The list of best found connections (if allowViableAlternatives is false, only contains 0 or 1 item)</returns>
        List<SearchResult>? FindConnection(string srcStopName, string destStopName, DateTime searchBeginTime, bool includeViableAlternatives);

        /// <summary>
        /// Finds the best connection(s) between the 2 coordinate points
        /// </summary>
        /// <param name="srcCoords">The source point coordinates</param>
        /// <param name="destCoords">The destination point coordinates</param>
        /// <param name="searchBeginTime">The time at which the search starts (i.e. departure time if this is a forward search, arrival date otherwise</param>
        /// <param name="includeViableAlternatives">Whether to also include connections with different number of trips than the best one found, assuming they do not differ much in quality.</param>
        /// <returns>The list of best found connections (if allowViableAlternatives is false, only contains 0 or 1 item)</returns>
        List<SearchResult>? FindConnection(Coordinates srcCoords, Coordinates destCoords, DateTime searchBeginTime, bool includeViableAlternatives);

        /// <summary>
        /// Finds the best connection(s) from the source coordinates to the destination stop with the given name
        /// </summary>
        /// <param name="srcCoords">The source point coordinates</param>
        /// <param name="destStopName">The destination stop name (exact)</param>
        /// <param name="searchBeginTime">The time at which the search starts (i.e. departure time if this is a forward search, arrival date otherwise</param>
        /// <param name="includeViableAlternatives">Whether to also include connections with different number of trips than the best one found, assuming they do not differ much in quality.</param>
        /// <returns>The list of best found connections (if allowViableAlternatives is false, only contains 0 or 1 item)</returns>
        List<SearchResult>? FindConnection(Coordinates srcCoords, string destStopName, DateTime searchBeginTime, bool includeViableAlternatives);

        /// <summary>
        /// Finds the best connection(s) from the source stop with the given name to the destination coordinates
        /// </summary>
        /// <param name="srcStopName">The source stop name (exact)</param>
        /// <param name="destCoords">The destination point coordinates</param>
        /// <param name="searchBeginTime">The time at which the search starts (i.e. departure time if this is a forward search, arrival date otherwise</param>
        /// <param name="includeViableAlternatives">Whether to also include connections with different number of trips than the best one found, assuming they do not differ much in quality.</param>
        /// <returns>The list of best found connections (if allowViableAlternatives is false, only contains 0 or 1 item)</returns>
        List<SearchResult>? FindConnection(string srcStopName, Coordinates destCoords, DateTime searchBeginTime, bool includeViableAlternatives);
    }
}

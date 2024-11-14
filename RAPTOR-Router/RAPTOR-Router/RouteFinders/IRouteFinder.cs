using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Requests;

namespace RAPTOR_Router.RouteFinders
{
    /// <summary>
    /// An interface for all RouteFinder objects, which are used to solve the connection search problem
    /// </summary>
    public interface IRangeRouteFinder
    {
        Task<CompleteSearchResult> FindConnectionsAsync(ConnectionRequest request);
    }

    public interface ISimpleRouteFinder
    {
       CompleteSearchResult FindConnection(ConnectionRequest request);
    }

    public interface ISimpleRoutingProvider
    {
        /// <summary>
        /// Solves the provided connection search problem (i.e. SearchModel) given by source and destination stop names
        /// </summary>
        /// <param name="sourceStopName">The exact name of the source stop</param>
        /// <param name="destStopName">The exact name of the destination stop</param>
        /// <param name="time">The departure/arrival date and time</param>
        /// <param name="includeViableAlternatives">Whether to include viable alternatives in the search - i.e. if results with different transfer counts are similar in time and comfort, return all</param>
        /// <returns>The resulting best connection, null if none found</returns>
        List<SearchResult>? FindConnection(string sourceStopName, string destStopName, DateTime time, bool includeViableAlternatives);

        List<SearchResult>? FindConnection(Coordinates srcCoords, Coordinates destCoords, DateTime time, bool includeViableAlternatives);

        List<SearchResult>? FindConnection(Coordinates srcCoords, string destStopName, DateTime time, bool includeViableAlternatives);

        List<SearchResult>? FindConnection(string sourceStopName, Coordinates destCoords, DateTime time, bool includeViableAlternatives);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Requests
{
    /// <summary>
    /// An enum representing an error due to which a connection request failed.
    /// </summary>
    public enum ConnectionSearchError
    {
        /// <summary>
        /// Indicates that no error occurred.
        /// </summary>
        NoError,

        /// <summary>
        /// The provided date and time are invalid.
        /// </summary>
        InvalidDateTime,

        /// <summary>
        /// The settings provided for the connection search are invalid.
        /// </summary>
        InvalidSettings,

        /// <summary>
        /// The source coordinates are invalid.
        /// </summary>
        InvalidSrcCoordinates,

        /// <summary>
        /// The destination coordinates are invalid.
        /// </summary>
        InvalidDestCoordinates,

        /// <summary>
        /// Both source and destination coordinates are invalid.
        /// </summary>
        InvalidBothCoordinates,

        /// <summary>
        /// No stops were found near the source coordinates.
        /// </summary>
        NoStopsNearSrcCoords,

        /// <summary>
        /// No stops were found near the destination coordinates.
        /// </summary>
        NoStopsNearDestCoords,

        /// <summary>
        /// No stops were found near either the source or destination coordinates.
        /// </summary>
        NoStopsNearBothCoords,

        /// <summary>
        /// The source stop name is invalid.
        /// </summary>
        InvalidSrcStopName,

        /// <summary>
        /// The destination stop name is invalid.
        /// </summary>
        InvalidDestStopName,

        /// <summary>
        /// Both source and destination stop names are invalid.
        /// </summary>
        InvalidBothStopNames,

        /// <summary>
        /// No connection was found between the source and destination.
        /// </summary>
        NoConnectionFound,
    }


    /// <summary>
    /// An enum representing an error due to which an alternatives search request failed.
    /// </summary>
    public enum AlternativesSearchError
    {
        /// <summary>
        /// Indicates that no error occurred.
        /// </summary>
        NoError,

        /// <summary>
        /// The provided date and time are invalid.
        /// </summary>
        InvalidDateTime,

        /// <summary>
        /// The source stop ID does not exist.
        /// </summary>
        NonExistentSrcStopId,

        /// <summary>
        /// The destination stop ID does not exist.
        /// </summary>
        NonExistentDestStopId,

        /// <summary>
        /// Both the source and destination stop IDs do not exist.
        /// </summary>
        NonExistentBothStopIds,

        /// <summary>
        /// The requested result count for the search is invalid.
        /// </summary>
        InvalidCount,

        /// <summary>
        /// No trips were found for the given search criteria.
        /// </summary>
        NoTripsFound
    }

    /// <summary>
    /// Class providing extension methods for the ConnectionSearchError enum. Used to convert the enum values to human-readable error messages.
    /// </summary>
    public static class SearchErrorExtensions
    {
        /// <summary>
        /// Converts a ConnectionSearchError enum value to a human-readable error message.
        /// </summary>
        /// <param name="error">The error value</param>
        /// <returns>The error message for the value</returns>
        public static string ToMessage(this ConnectionSearchError error)
        {
            return error switch
            {
                ConnectionSearchError.NoError => "No error",
                ConnectionSearchError.InvalidDateTime => "Invalid dateTime format",
                ConnectionSearchError.InvalidSettings => "Invalid settings",
                ConnectionSearchError.InvalidSrcCoordinates => "Invalid source coordinates",
                ConnectionSearchError.InvalidDestCoordinates => "Invalid destination coordinates",
                ConnectionSearchError.InvalidBothCoordinates => "Invalid source and destination coordinates",
                ConnectionSearchError.NoStopsNearSrcCoords => "No stops near source coordinates",
                ConnectionSearchError.NoStopsNearDestCoords => "No stops near destination coordinates",
                ConnectionSearchError.NoStopsNearBothCoords => "No stops near source and destination coordinates",
                ConnectionSearchError.InvalidSrcStopName => "Invalid source stop name",
                ConnectionSearchError.InvalidDestStopName => "Invalid destination stop name",
                ConnectionSearchError.InvalidBothStopNames => "Invalid source and destination stop names",
                ConnectionSearchError.NoConnectionFound => "No connection found",
                _ => "Unknown error",
            };
        }
    }

    /// <summary>
    /// Class providing extension methods for the AlternativesSearchError enum. Used to convert the enum values to human-readable error messages.
    /// </summary>
    public static class AlternativesSearchErrorExtensions
    {
        /// <summary>
        /// Converts an AlternativesSearchError enum value to a human-readable error message.
        /// </summary>
        /// <param name="error">The error value</param>
        /// <returns>The error message for the value</returns>
        public static string ToMessage(this AlternativesSearchError error)
        {
            return error switch
            {
                AlternativesSearchError.NoError => "No error",
                AlternativesSearchError.InvalidDateTime => "Invalid dateTime format",
                AlternativesSearchError.NonExistentSrcStopId => "Non-existent source stop ID",
                AlternativesSearchError.NonExistentDestStopId => "Non-existent destination stop ID",
                AlternativesSearchError.NonExistentBothStopIds => "Non-existent source and destination stop IDs",
                AlternativesSearchError.InvalidCount => "Invalid count. The count must be a value between 1 and 10",
                AlternativesSearchError.NoTripsFound => "No trips found",
                _ => "Unknown error",
            };
        }
    }
}

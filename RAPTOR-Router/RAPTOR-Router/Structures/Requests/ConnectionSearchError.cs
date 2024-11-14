using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Requests
{
    public enum ConnectionSearchError
    {
        NoError,
        InvalidDateTime,
        InvalidSettings,
        InvalidSrcCoordinates,
        InvalidDestCoordinates,
        InvalidBothCoordinates,
        NoStopsNearSrcCoords,
        NoStopsNearDestCoords,
        NoStopsNearBothCoords,
        InvalidSrcStopName,
        InvalidDestStopName,
        InvalidBothStopNames,
        NoConnectionFound,
    }

    public enum AlternativesSearchError
    {
        NoError,
        InvalidDateTime,
        NonExistentSrcStopId,
        NonExistentDestStopId,
        NonExistentBothStopIds,
        InvalidCount,
        NoTripsFound
    }

    public static class SearchErrorExtensions
    {
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

    public static class AlternativesSearchErrorExtensions
    {
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

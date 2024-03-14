using System.Text.Json.Serialization;

namespace RAPTOR_Router.GBFSParsing.GBFSStructures
{
    /// <summary>
    /// Class representing the stations status data from a GBFS feed
    /// </summary>
    internal class GBFSStationStatus
    {
        [JsonPropertyName("last_updated")]
        public int LastUpdated { get; set; }

        [JsonPropertyName("ttl")]
        public int Ttl { get; set; }

        [JsonPropertyName("data")]
        public GBFSStatusData Data { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }

    /// <summary>
    /// Class representing the list of statuses for all stations in a GBFS feed
    /// </summary>
    internal class GBFSStatusData
    {
        [JsonPropertyName("stations")]
        public List<GBFSSingleStationStatus> Stations { get; set; }
    }

    /// <summary>
    /// Class representing the status of a single station in a GBFS feed
    /// </summary>
    internal class GBFSSingleStationStatus
    {
        [JsonPropertyName("station_id")]
        public string StationId { get; set; }

        [JsonPropertyName("num_bikes_available")]
        public int NumBikesAvailable { get; set; }

        [JsonPropertyName("num_docks_available")]
        public int NumDocksAvailable { get; set; }

        [JsonPropertyName("is_installed")]
        public bool IsInstalled { get; set; }

        [JsonPropertyName("is_renting")]
        public bool IsRenting { get; set; }

        [JsonPropertyName("is_returning")]
        public bool IsReturning { get; set; }

        [JsonPropertyName("last_reported")]
        public int LastReported { get; set; }
    }
}

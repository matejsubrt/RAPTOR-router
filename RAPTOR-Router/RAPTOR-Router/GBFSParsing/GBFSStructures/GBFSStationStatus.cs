using System.Text.Json.Serialization;

namespace RAPTOR_Router.GBFSParsing.GBFSStructures
{
    /// <summary>
    /// Class representing the stations status data from a GBFS feed
    /// </summary>
    internal class GBFSStationStatus
    {
        /// <summary>
        /// The last time the data was updated
        /// </summary>
        [JsonPropertyName("last_updated")]
        public int LastUpdated { get; set; }

        /// <summary>
        /// The time to live of the data
        /// </summary>

        [JsonPropertyName("ttl")]
        public int Ttl { get; set; }

        /// <summary>
        /// The data itself
        /// </summary>

        [JsonPropertyName("data")]
        public GBFSStatusData Data { get; set; }

        /// <summary>
        /// The GBFS version
        /// </summary>

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }

    /// <summary>
    /// Class representing the list of statuses for all stations in a GBFS feed
    /// </summary>
    internal class GBFSStatusData
    {
        /// <summary>
        /// The list of statuses for all stations
        /// </summary>
        [JsonPropertyName("stations")]
        public List<GBFSSingleStationStatus> Stations { get; set; }
    }

    /// <summary>
    /// Class representing the status of a single station in a GBFS feed
    /// </summary>
    internal class GBFSSingleStationStatus
    {
        /// <summary>
        /// The id of the station
        /// </summary>
        [JsonPropertyName("station_id")]
        public string StationId { get; set; }

        /// <summary>
        /// The number of bikes available at the station
        /// </summary>
        [JsonPropertyName("num_bikes_available")]
        public int NumBikesAvailable { get; set; }

        /// <summary>
        /// The number of docks available at the station
        /// </summary>
        [JsonPropertyName("num_docks_available")]
        public int NumDocksAvailable { get; set; }

        /// <summary>
        /// Whether the station is installed
        /// </summary>
        [JsonPropertyName("is_installed")]
        public bool IsInstalled { get; set; }

        /// <summary>
        /// Whether the station is renting
        /// </summary>
        [JsonPropertyName("is_renting")]
        public bool IsRenting { get; set; }

        /// <summary>
        /// Whether the station is returning
        /// </summary>
        [JsonPropertyName("is_returning")]
        public bool IsReturning { get; set; }

        /// <summary>
        /// The last time the station was reported
        /// </summary>
        [JsonPropertyName("last_reported")]
        public int LastReported { get; set; }
    }
}

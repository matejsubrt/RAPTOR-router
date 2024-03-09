using System.Text.Json.Serialization;

namespace RAPTOR_Router.GBFSParsing.GBFSStructures
{
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

    internal class GBFSStatusData
    {
        [JsonPropertyName("stations")]
        public List<GBFSSingleStationStatus> Stations { get; set; }
    }

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

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RAPTOR_Router.GBFSParsing.GBFSStructures
{
    public class GBFSStationInfo
    {
        [JsonPropertyName("last_updated")]
        public long LastUpdated { get; set; }

        [JsonPropertyName("ttl")]
        public int Ttl { get; set; }

        [JsonPropertyName("data")]
        public GBFSData Data { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
    }

    public class GBFSData
    {
        [JsonPropertyName("stations")]
        public List<GBFSStation> Stations { get; set; }
    }

    public class GBFSStation
    {
        [JsonPropertyName("station_id")]
        public string StationId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("short_name")]
        public string ShortName { get; set; }

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }

        [JsonPropertyName("region_id")]
        public string RegionId { get; set; }

        [JsonPropertyName("rental_uris")]
        public RentalUris RentalUris { get; set; }

        [JsonPropertyName("is_virtual_station")]
        public bool IsVirtualStation { get; set; }

        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }
    }

    public class RentalUris
    {
        [JsonPropertyName("android")]
        public string Android { get; set; }

        [JsonPropertyName("ios")]
        public string Ios { get; set; }

        [JsonPropertyName("web")]
        public string Web { get; set; }
    }

}

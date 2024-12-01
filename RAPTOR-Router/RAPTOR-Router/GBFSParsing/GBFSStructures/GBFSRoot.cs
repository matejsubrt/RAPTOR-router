using System.Text.Json.Serialization;

namespace RAPTOR_Router.GBFSParsing.GBFSStructures
{
    /// <summary>
    /// Class representing the GBFS feed root object
    /// </summary>
    public class GBFSRoot
    {
        /// <summary>
        /// The timestamp (in Unix time format) when the data was last updated.
        /// </summary>
        [JsonPropertyName("last_updated")]
        public long LastUpdated { get; set; }

        /// <summary>
        /// The Time-To-Live (TTL) value indicating how long the data is valid for.
        /// This value typically defines the freshness of the data.
        /// </summary>
        [JsonPropertyName("ttl")]
        public int Ttl { get; set; }

        /// <summary>
        /// The actual data containing information about bike stations, including their availability, status, and locations.
        /// </summary>
        [JsonPropertyName("data")]
        public required GBFSData Data { get; set; }

        /// <summary>
        /// The version of the GBFS feed format.
        /// </summary>
        [JsonPropertyName("version")]
        public required string Version { get; set; }
    }

    /// <summary>
    /// Class representing the GBFS list of bike station information
    /// </summary>
    public class GBFSData
    {
        /// <summary>
        /// The list of stations in the feed
        /// </summary>
        [JsonPropertyName("stations")]
        public required List<GBFSStation> Stations { get; set; }
    }

    /// <summary>
    /// Class representing a single bike station in the GBFS feed
    /// </summary>
    public class GBFSStation
    {
        /// <summary>
        /// The unique identifier for the station.
        /// </summary>
        [JsonPropertyName("station_id")]
        public required string StationId { get; set; }

        /// <summary>
        /// The name of the station (e.g., "Main Street Station").
        /// </summary>
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        /// <summary>
        /// A short or abbreviated name for the station (e.g., "Main St.").
        /// </summary>
        [JsonPropertyName("short_name")]
        public required string ShortName { get; set; }

        /// <summary>
        /// The latitude of the station in decimal degrees.
        /// </summary>
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        /// <summary>
        /// The longitude of the station in decimal degrees.
        /// </summary>
        [JsonPropertyName("lon")]
        public double Lon { get; set; }

        /// <summary>
        /// The identifier for the region in which the station is located.
        /// </summary>
        [JsonPropertyName("region_id")]
        public required string RegionId { get; set; }

        /// <summary>
        /// Contains the URIs (URLs) related to rental services and interfaces for the station.
        /// </summary>
        [JsonPropertyName("rental_uris")]
        public required RentalUris RentalUris { get; set; }

        /// <summary>
        /// Indicates whether the station is a virtual station.
        /// Virtual stations may not have a physical location.
        /// </summary>
        [JsonPropertyName("is_virtual_station")]
        public bool IsVirtualStation { get; set; }

        /// <summary>
        /// The total capacity of the station, which could refer to the number of bikes or docking spaces available.
        /// </summary>
        [JsonPropertyName("capacity")]
        public int Capacity { get; set; }
    }


    /// <summary>
    /// Class representing the URIs for accessing the rental service across different platforms: Android, iOS, and web.
    /// </summary>
    public class RentalUris
    {
        /// <summary>
        /// The URI for the Android app associated with the rental service.
        /// </summary>
        [JsonPropertyName("android")]
        public required string Android { get; set; }

        /// <summary>
        /// The URI for the iOS app associated with the rental service.
        /// </summary>
        [JsonPropertyName("ios")]
        public required string Ios { get; set; }

        /// <summary>
        /// The URI for the web interface or website of the rental service.
        /// </summary>
        [JsonPropertyName("web")]
        public required string Web { get; set; }
    }


}

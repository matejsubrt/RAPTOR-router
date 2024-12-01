using CsvHelper.Configuration.Attributes;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing the stops information from the stops.txt GTFS file.
    /// The properties correspond to the entries in the file.
    /// </summary>
    public class GTFSStop : IIdentifiable
    {
        /// <summary>
        /// The unique identifier for the stop.
        /// </summary>
        [Name("stop_id")]
        public string Id { get; set; }

        /// <summary>
        /// The name of the stop, typically used in public-facing contexts.
        /// </summary>
        [Name("stop_name")]
        public string Name { get; set; }

        /// <summary>
        /// The latitude of the stop's location in decimal degrees.
        /// </summary>
        [Name("stop_lat")]
        public double Lat { get; set; }

        /// <summary>
        /// The longitude of the stop's location in decimal degrees.
        /// </summary>
        [Name("stop_lon")]
        public double Lon { get; set; }

        /// <summary>
        /// The zone ID associated with the stop. Used for fare calculation.
        /// </summary>
        [Name("zone_id")]
        public string ZoneId { get; set; }

        /*
        /// <summary>
        /// The URL for a web page about the stop.
        /// </summary>
        [Name("stop_url")]
        public string Url { get; set; }
        */

        /// <summary>
        /// The type of location represented by the stop.
        /// </summary>
        /// <remarks>
        /// Common values:
        /// <list type="bullet">
        /// <item><description>0 - Stop or platform</description></item>
        /// <item><description>1 - Station</description></item>
        /// <item><description>2 - Entrance or exit</description></item>
        /// <item><description>3 - Generic node</description></item>
        /// <item><description>4 - Boarding area</description></item>
        /// </list>
        /// </remarks>
        [Name("location_type")]
        public int LocationType { get; set; }

        /*
        /// <summary>
        /// The parent station associated with the stop, if applicable.
        /// </summary>
        [Name("parent_station")]
        public string ParentStation { get; set; }

        /// <summary>
        /// Indicates wheelchair accessibility for the stop.
        /// </summary>
        /// <remarks>
        /// Common values:
        /// <list type="bullet">
        /// <item><description>0 - No information</description></item>
        /// <item><description>1 - Accessible</description></item>
        /// <item><description>2 - Not accessible</description></item>
        /// </list>
        /// </remarks>
        [Name("wheelchair_boarding")]
        public int WheelchairBoarding { get; set; }

        /// <summary>
        /// The level ID associated with the stop, if applicable.
        /// </summary>
        [Name("level_id")]
        public string LevelId { get; set; }

        /// <summary>
        /// The platform code for the stop, if applicable.
        /// </summary>
        [Name("platform_code")]
        public string PlatformCode { get; set; }

        /// <summary>
        /// ASW node ID associated with the stop.
        /// </summary>
        [Name("asw_node_id")]
        public string AswNodeId { get; set; }

        /// <summary>
        /// ASW stop ID associated with the stop.
        /// </summary>
        [Name("asw_stop_id")]
        public string AswStopId { get; set; }
        */
    }

}

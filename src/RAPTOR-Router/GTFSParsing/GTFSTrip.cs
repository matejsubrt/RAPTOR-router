using CsvHelper.Configuration.Attributes;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing the trips information from the trips.txt GTFS file.
    /// The properties correspond to the entries in the file.
    /// </summary>
    public class GTFSTrip : IIdentifiable
    {
        /// <summary>
        /// The unique identifier for the route associated with this trip.
        /// </summary>
        [Name("route_id")]
        public required string RouteId { get; set; }

        /// <summary>
        /// The service ID associated with this trip, indicating the calendar service pattern.
        /// </summary>
        [Name("service_id")]
        public required string ServiceId { get; set; }

        /// <summary>
        /// The unique identifier for the trip.
        /// </summary>
        [Name("trip_id")]
        public required string Id { get; set; }

        /*
        /// <summary>
        /// The headsign used for the trip, typically indicating the destination.
        /// </summary>
        [Name("trip_headsign")]
        public string Headsign { get; set; }

        /// <summary>
        /// A short name or label for the trip.
        /// </summary>
        [Name("trip_short_name")]
        public string ShortName { get; set; }

        /// <summary>
        /// The direction ID for the trip, indicating travel direction.
        /// </summary>
        /// <remarks>
        /// Common values:
        /// <list type="bullet">
        /// <item><description>0 - Travel in one direction (e.g., outbound)</description></item>
        /// <item><description>1 - Travel in the opposite direction (e.g., inbound)</description></item>
        /// </list>
        /// </remarks>
        [Name("direction_id")]
        public bool DirectionId { get; set; }

        /// <summary>
        /// The block ID that this trip belongs to, which groups sequential trips.
        /// </summary>
        [Name("block_id")]
        public string BlockId { get; set; }

        /// <summary>
        /// The shape ID defining the path for this trip.
        /// </summary>
        [Name("shape_id")]
        public string ShapeId { get; set; }

        /// <summary>
        /// Indicates if the trip is wheelchair accessible.
        /// </summary>
        /// <remarks>
        /// Common values:
        /// <list type="bullet">
        /// <item><description>0 - No information</description></item>
        /// <item><description>1 - Accessible</description></item>
        /// <item><description>2 - Not accessible</description></item>
        /// </list>
        /// </remarks>
        [Name("wheelchair_accessible")]
        public int WheelchairAccessible { get; set; }

        /// <summary>
        /// Indicates if bikes are allowed on this trip.
        /// </summary>
        /// <remarks>
        /// Common values:
        /// <list type="bullet">
        /// <item><description>0 - No information</description></item>
        /// <item><description>1 - Bikes allowed</description></item>
        /// <item><description>2 - Bikes not allowed</description></item>
        /// </list>
        /// </remarks>
        [Name("bikes_allowed")]
        public int BikesAllowed { get; set; }

        /// <summary>
        /// Indicates whether the trip has exceptional circumstances.
        /// </summary>
        [Name("exceptional")]
        public bool Exceptional { get; set; }

        /// <summary>
        /// The sub-agency ID associated with this trip.
        /// </summary>
        [Name("sub_agency_id")]
        public int SubAgencyId { get; set; }
        */
    }

}

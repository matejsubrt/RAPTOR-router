using CsvHelper.Configuration.Attributes;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing the stop times information from the stop_times.txt GTFS file.
    /// The properties correspond to the entries in the file.
    /// </summary>
    public class GTFSStopTime
    {
        /// <summary>
        /// The ID of the trip that this stop time is part of.
        /// </summary>
        [Name("trip_id")]
        public required string TripId { get; set; }

        /// <summary>
        /// The arrival time at the stop.
        /// </summary>
        /// <remarks>
        /// The time is represented in HH:MM:SS format.
        /// </remarks>
        [Name("arrival_time")]
        [TypeConverter(typeof(GTFSTimeOnlyConverter))]
        public required TimeOnly ArrivalTime { get; set; }

        /// <summary>
        /// The departure time from the stop.
        /// </summary>
        /// <remarks>
        /// The time is represented in HH:MM:SS format.
        /// </remarks>
        [Name("departure_time")]
        [TypeConverter(typeof(GTFSTimeOnlyConverter))]
        public required TimeOnly DepartureTime { get; set; }

        /// <summary>
        /// The ID of the stop where this stop time occurs.
        /// </summary>
        [Name("stop_id")]
        public required string StopId { get; set; }

        /*
        /// <summary>
        /// The sequence number of this stop in the trip's schedule.
        /// </summary>
        [Name("stop_sequence")]
        public int StopSequence { get; set; }

        /// <summary>
        /// The headsign to be displayed on the vehicle, indicating the destination.
        /// </summary>
        [Name("stop_headsign")]
        public string StopHeadsign { get; set; }

        /// <summary>
        /// The pickup type for this stop.
        /// </summary>
        [Name("pickup_type")]
        public int PickupType { get; set; }

        /// <summary>
        /// The drop-off type for this stop.
        /// </summary>
        [Name("drop_off_type")]
        public int DropoffType { get; set; }

        /// <summary>
        /// The distance traveled along the shape at this stop.
        /// </summary>
        [Name("shape_dist_traveled")]
        public double ShapeDistTravelled { get; set; }

        /// <summary>
        /// The type of trip operation for this stop time (e.g., regular, scheduled, etc.).
        /// </summary>
        [Name("trip_operation_type")]
        public int OperationType { get; set; }

        /// <summary>
        /// Indicates if bikes are allowed on this trip at this stop.
        /// </summary>
        [Name("bikes_allowed")]
        public int BikesAllowed { get; set; }
        */
    }

}

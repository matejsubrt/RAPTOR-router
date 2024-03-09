using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing the stop times information from the stop_times.txt gtfs file.
    /// The properties correspond to the entries in the file.
    /// </summary>
    public class GTFSStopTime : IIdentifiable
    {
        [Name("trip_id")]
        public string TripId { get; set; }
        [Name("arrival_time")]
        [TypeConverter(typeof(GTFSTimeOnlyConverter))]
        public TimeOnly ArrivalTime { get; set; }
        [Name("departure_time")]
        [TypeConverter(typeof(GTFSTimeOnlyConverter))]
        public TimeOnly DepartureTime { get; set; }
        [Name("stop_id")]
        public string StopId { get; set; }
        /*
        [Name("stop_sequence")]
        public int StopSequence { get; set; }
        [Name("stop_headsign")]
        public string StopHeadsign { get; set; }
        [Name("pickup_type")]
        public int PickupType { get; set; }
        [Name("drop_off_type")]
        public int DropoffType { get; set; }
        [Name("shape_dist_traveled")]
        public double ShapeDistTravelled { get; set; }
        [Name("trip_operation_type")]
        public int OperationType { get; set; }
        [Name("bikes_allowed")]
        public int BikesAllowed { get; set; }
        */

        public string GetId()
        {
            return TripId;
        }
    }
}

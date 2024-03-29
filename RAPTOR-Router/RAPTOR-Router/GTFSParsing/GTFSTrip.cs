﻿using CsvHelper.Configuration.Attributes;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing the trips information from the trips.txt gtfs file.
    /// The properties correspond to the entries in the file.
    /// </summary>
    public class GTFSTrip : IIdentifiable
    {
        [Name("route_id")]
        public string RouteId { get; set; }
        [Name("service_id")]
        public string ServiceId { get; set; }
        [Name("trip_id")]
        public string Id { get; set; }
        /*
        [Name("trip_headsign")]
        public string Headsign { get; set; }
        [Name("trip_short_name")]
        public string ShortName { get; set; }
        [Name("direction_id")]
        public bool DirectionId { get; set; }
        [Name("block_id")]
        public string BlockId { get; set; }
        [Name("shape_id")]
        public string ShapeId { get; set; }
        [Name("wheelchair_accessible")]
        public int WheelchairAccessible { get; set; }
        [Name("bikes_allowed")]
        public int BikesAllowed { get; set; }
        [Name("exceptional")]
        public bool Exceptional { get; set; }
        [Name("sub_agency_id")]
        public int SubAgencyId { get; set; }
        */

        public string GetId()
        {
            return Id;
        }
    }
}

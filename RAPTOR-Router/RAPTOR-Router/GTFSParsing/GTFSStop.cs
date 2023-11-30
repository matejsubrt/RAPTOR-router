using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing the stops information from the stops.txt gtfs file.
    /// The properties correspond to the entries in the file.
    /// </summary>
    internal class GTFSStop : IIdentifiable
    {
        [Name("stop_id")]
        public string Id { get; set; }

        [Name("stop_name")]
        public string Name { get; set; }

        [Name("stop_lat")]
        public double Lat { get; set; }

        [Name("stop_lon")]
        public double Lon { get; set; }

        [Name("zone_id")]
        public string ZoneId { get; set; }

        //[Name("stop_url")]
        //public string Url { get; set; }

        [Name("location_type")]
        public int LocationType { get; set; }

        /*
        [Name("parent_station")]
        public string ParentStation { get; set; }

        [Name("wheelchair_boarding")]
        public int WheelchairBoarding { get; set; }

        [Name("level_id")]
        public string LevelId { get; set; }

        [Name("platform_code")]
        public string PlatformCode { get; set; }

        [Name("asw_node_id")]
        public string AswNodeId { get; set; }

        [Name("asw_stop_id")]
        public string AswStopId { get; set; }
        */
        public string GetId()
        {
            return Id;
        }
    }
}

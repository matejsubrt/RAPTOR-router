using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration.Attributes;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing the routes information from the routes.txt gtfs file.
    /// The properties correspond to the entries in the file.
    /// </summary>
    internal class GTFSRoute : IIdentifiable
    {
        [Name("route_id")]
        public string Id { get; set; }
        [Name("agency_id")]
        public string AgencyId { get; set; }
        [Name("route_short_name")]
        public string ShortName { get; set; }
        [Name("route_long_name")]
        public string LongName { get; set; }
        [Name("route_type")]
        public int Type { get; set; }
        [Name("route_url")]
        public string Url { get; set; }
        [Name("route_color")]
        public string Color { get; set; }
        [Name("route_text_color")]
        public string TextColor { get; set; }
        [Name("is_night")]
        public bool IsNight { get; set; }
        [Name("is_regional")]
        public bool IsRegional { get; set; }
        [Name("is_substitute_transport")]
        public bool IsSubstituteTransport { get; set; }

        public string GetId()
        {
            return Id;
        }
    }
}

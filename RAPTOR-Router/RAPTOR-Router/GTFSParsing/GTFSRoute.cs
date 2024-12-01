using CsvHelper.Configuration.Attributes;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing the routes information from the routes.txt GTFS file.
    /// The properties correspond to the entries in the file.
    /// </summary>
    public class GTFSRoute : IIdentifiable
    {
        /// <summary>
        /// The unique identifier for the route.
        /// </summary>
        [Name("route_id")]
        public string Id { get; set; }

        /*
        /// <summary>
        /// The ID of the agency responsible for the route.
        /// </summary>
        [Name("agency_id")]
        public string AgencyId { get; set; }
        */

        /// <summary>
        /// The short name of the route, typically a route number or code.
        /// </summary>
        [Name("route_short_name")]
        public string ShortName { get; set; }

        /// <summary>
        /// The long name of the route, typically a descriptive name.
        /// </summary>
        [Name("route_long_name")]
        public string LongName { get; set; }

        /// <summary>
        /// The type of transportation used on the route.
        /// </summary>
        /// <remarks>
        /// Common values:
        /// <list type="bullet">
        /// <item><description>0 - Tram, Streetcar, Light Rail</description></item>
        /// <item><description>1 - Subway, Metro</description></item>
        /// <item><description>2 - Rail</description></item>
        /// <item><description>3 - Bus</description></item>
        /// <item><description>4 - Ferry</description></item>
        /// <item><description>5 - Cable car</description></item>
        /// <item><description>6 - Gondola, Suspended cable car</description></item>
        /// <item><description>7 - Funicular</description></item>
        /// </list>
        /// </remarks>
        [Name("route_type")]
        public int Type { get; set; }

        /*
        /// <summary>
        /// The URL for a web page about the route.
        /// </summary>
        [Name("route_url")]
        public string Url { get; set; }
        */

        /// <summary>
        /// The color used to represent the route in a visual display.
        /// </summary>
        [Name("route_color")]
        public string Color { get; set; }

        /*
        /// <summary>
        /// The color used for text on the route's display, to be used alongside the route color.
        /// </summary>
        [Name("route_text_color")]
        public string TextColor { get; set; }

        /// <summary>
        /// Indicates whether the route operates during nighttime.
        /// </summary>
        [Name("is_night")]
        public bool IsNight { get; set; }

        /// <summary>
        /// Indicates whether the route is classified as regional.
        /// </summary>
        [Name("is_regional")]
        public bool IsRegional { get; set; }

        /// <summary>
        /// Indicates whether the route is a substitute transport route (e.g., replacement bus service).
        /// </summary>
        [Name("is_substitute_transport")]
        public bool IsSubstituteTransport { get; set; }
        */
    }

}

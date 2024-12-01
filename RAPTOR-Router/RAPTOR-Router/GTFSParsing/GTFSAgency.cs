using CsvHelper.Configuration.Attributes;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing the agencies information from the agencies.txt gtfs file.
    /// The properties correspond to the entries in the file.
    /// </summary>
    public class GTFSAgency : IIdentifiable
    {
        /// <summary>
        /// The id of the agency
        /// </summary>
        [Name("agency_id")]
        public required string Id { get; set; }

        /// <summary>
        /// The name of the agency
        /// </summary>
        [Name("agency_name")]
        public string? Name { get; set; }

        /// <summary>
        /// The url of the agencies website
        /// </summary>
        [Name("agency_url")]
        public string? Url { get; set; }

        /// <summary>
        /// The timezone of the agency
        /// </summary>
        [Name("agency_timezone")]
        public string? Timezone { get; set; }

        /// <summary>
        /// The language of the agency
        /// </summary>
        [Name("agency_lang")]
        public string? Language { get; set; }

        /// <summary>
        /// The phone number of the agency
        /// </summary>
        [Name("agency_phone")]
        public string? Phone { get; set; }
    }
}

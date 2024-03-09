using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// Class representing the agencies information from the agencies.txt gtfs file.
    /// The properties correspond to the entries in the file.
    /// </summary>
    public class GTFSAgency : IIdentifiable
    {
        [Name("agency_id")]
        public string Id { get; set; }
        [Name("agency_name")]
        public string Name { get; set; }
        [Name("agency_url")]
        public string Url { get; set; }
        [Name("agency_timezone")]
        public string Timezone { get; set; }
        [Name("agency_lang")]
        public string Language { get; set; }
        [Name("agency_phone")]
        public string Phone { get; set; }

        public string GetId()
        {
            return Id;
        }
    }
}

using RAPTOR_Router.Structures.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Requests
{
    public class ConnectionRequest
    {
        public string srcStopName { get; set; }
        public double srcLat { get; set; }
        public double srcLon { get; set; }
        public string destStopName { get; set; }
        public double destLat { get; set; }
        public double destLon { get; set; }
        public DateTime? dateTime { get; set; }
        public int rangeLength { get; set; }

        public bool byEarliestDeparture { get; set; }
        public bool range { get; set; }
        public bool srcByCoords { get; set; }
        public bool destByCoords { get; set; }

        public Settings settings { get; set; }
    }
}

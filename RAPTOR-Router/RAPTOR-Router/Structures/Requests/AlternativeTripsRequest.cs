using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Requests
{
    public class AlternativeTripsRequest
    {
        public string srcStopId { get; set; }
        public string destStopId { get; set; }
        public string dateTime { get; set; }
        public bool previous { get; set; }
        public int count { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
    internal struct StopTime
    {
        public TimeOnly ArrivalTime { get; set; }
        public TimeOnly DepartureTime { get; set; }

        public StopTime(TimeOnly arrivalTime, TimeOnly departureTime)
        {
            ArrivalTime = arrivalTime;
            DepartureTime = departureTime;
        }
    }
}

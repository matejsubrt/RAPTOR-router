using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
    /// <summary>
    /// Struct representing a stop time of a single trip in a single stop
    /// </summary>
    internal struct StopTime
    {
        /// <summary>
        /// The time, at which a trip arrives at the stop
        /// </summary>
        public TimeOnly ArrivalTime { get; set; }
        /// <summary>
        /// The time, at which the trip departs from the stop
        /// </summary>
        public TimeOnly DepartureTime { get; set; }
        /// <summary>
        /// Creates a new StopTime object
        /// </summary>
        /// <param name="arrivalTime">The arrival time</param>
        /// <param name="departureTime">The departure time</param>

        public StopTime(TimeOnly arrivalTime, TimeOnly departureTime)
        {
            ArrivalTime = arrivalTime;
            DepartureTime = departureTime;
        }
    }
}

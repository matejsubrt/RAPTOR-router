using RAPTOR_Router.GTFSParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
    /// <summary>
    /// Class representing a unique public transit trip
    /// </summary>
    internal class Trip
    {
        public Route Route;
        public List<StopTime> StopTimes;
        public Trip(List<GTFSStopTime> gtfsTripStopTimes, Route route)
        {
            this.Route = route;
            this.StopTimes = new();
            foreach (GTFSStopTime gtfsTripStopTime in gtfsTripStopTimes)
            {
                StopTime stopTime = new StopTime(gtfsTripStopTime.ArrivalTime, gtfsTripStopTime.DepartureTime);
                StopTimes.Add(stopTime);
            }
        }
        public override string ToString()
        {
            return Route.ShortName + ": " + StopTimes[0].DepartureTime;
        }
        /// <summary>
        /// Compares 2 trips by their departure times from their first stop
        /// </summary>
        /// <param name="trip1">First trip</param>
        /// <param name="trip2">Second trip</param>
        /// <returns>1 if trip1 departureTime is later, 0 if equal, -1 if earlier</returns>
        public static int CompareTrips(Trip trip1, Trip trip2)
        {
            return trip1.StopTimes[0].DepartureTime.CompareTo(trip2.StopTimes[0].DepartureTime);

        }
    }
}

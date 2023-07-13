using RAPTOR_Router.GTFSParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RAPTOR_Router.RAPTORStructures
{
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
        public static int CompareTrips(Trip trip1, Trip trip2)
        {
            return trip1.StopTimes[0].DepartureTime.CompareTo(trip2.StopTimes[0].DepartureTime);

        }
    }
}

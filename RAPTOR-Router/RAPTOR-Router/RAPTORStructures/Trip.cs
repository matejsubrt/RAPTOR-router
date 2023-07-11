using RAPTOR_Router.GTFSParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
    internal class Trip
    {
        public Route Route { get; }
        public DateOnly Date { get; set; }
        public List<StopTime> StopTimes { get; set; } = new List<StopTime>();
        public Trip(List<GTFSStopTime> gtfsTripStopTimes, Route route, DateOnly date)
        {
            Route = route;
            Date = date;
            foreach(GTFSStopTime gtfsTripStopTime in gtfsTripStopTimes)
            {
                StopTime stopTime = new StopTime(gtfsTripStopTime.ArrivalTime, gtfsTripStopTime.DepartureTime);
                StopTimes.Add(stopTime);
            }
        }
        public string ToString()
        {
            return Route.ShortName + ": " + Date + " " + StopTimes[0].DepartureTime;
        }
        public static int CompareTrips(Trip trip1, Trip trip2)
        {
            if(trip1.Date < trip2.Date)
            {
                return -1;
            }
            else if(trip1.Date > trip2.Date)
            {
                return 1;
            }
            else
            {
                return trip1.StopTimes[0].DepartureTime.CompareTo(trip2.StopTimes[0].DepartureTime);
            }            
        }
    }
}

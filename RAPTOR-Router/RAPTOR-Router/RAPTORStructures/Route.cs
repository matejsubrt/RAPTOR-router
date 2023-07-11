using RAPTOR_Router.GTFSParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
    internal class Route
    {
        public string Id { get; }
        public string GTFSId { get; }
        public string ShortName { get; }
        public string LongName { get; }
        public List<Stop> RouteStops { get; set; } = new();
        public List<Trip> RouteTrips { get; set; } = new();

        public Route(string id, string gtfsId, string shortName, string longName)
        {
            Id = id;
            GTFSId = gtfsId;
            ShortName = shortName;
            LongName = longName;
        }
        public int GetStopIndex(Stop stop)
        {
            int res = RouteStops.IndexOf(stop);
            if(res == -1)
            {
                throw new InvalidOperationException("Stop not found");
            }
            return res;
        }
        public Trip GetEarliestTripAtStop(Stop stop, DateTime dateTime)
        {
            int stopIndex = RouteStops.IndexOf(stop);
            int i = 0;
            DateTime departureDateTime = new DateTime(RouteTrips[i].Date.Year, RouteTrips[i].Date.Month, RouteTrips[i].Date.Day, RouteTrips[i].StopTimes[stopIndex].DepartureTime.Hour, RouteTrips[i].StopTimes[stopIndex].DepartureTime.Minute, RouteTrips[i].StopTimes[stopIndex].DepartureTime.Second);
            while (departureDateTime < dateTime)
            {                
                i++;
                if (i >= RouteTrips.Count)
                {
                    return null;
                }
                departureDateTime = new DateTime(RouteTrips[i].Date.Year, RouteTrips[i].Date.Month, RouteTrips[i].Date.Day, RouteTrips[i].StopTimes[stopIndex].DepartureTime.Hour, RouteTrips[i].StopTimes[stopIndex].DepartureTime.Minute, RouteTrips[i].StopTimes[stopIndex].DepartureTime.Second);
            }
            return RouteTrips[i];
        }
    }
}

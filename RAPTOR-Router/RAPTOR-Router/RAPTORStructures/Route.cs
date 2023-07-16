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
        public Dictionary<DateOnly, List<Trip>> RouteTrips { get; set; } = new();
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
            if (res == -1)
            {
                throw new InvalidOperationException("Stop not found");
            }
            return res;
        }
        public Trip GetEarliestTripAtStop(Stop stop, DateOnly date, TimeOnly time, int maxDaysAfter, out DateOnly tripDate)
        {
            if(stop.Name == "Ostrčilovo náměstí" && date.Day == 15 && time.Hour == 21 && time.Minute == 36 && time.Second == 9)
            {
                Console.WriteLine();
            }
            int stopIndex = GetStopIndex(stop);
            DateOnly currDate = date;
            DateOnly maxDate = date.AddDays(maxDaysAfter);

            List<Trip> tripsOnDate;
            if (RouteTrips.ContainsKey(currDate))
            {
                tripsOnDate = RouteTrips[currDate];

                TimeOnly departureTime;
                //Scan the first day for trips leaving after specified time
                for (int i = 0; i < tripsOnDate.Count; i++)
                {
                    departureTime = tripsOnDate[i].StopTimes[stopIndex].DepartureTime;

                    if(departureTime < tripsOnDate[i].StopTimes[0].DepartureTime)
                    {
                        tripDate = currDate.AddDays(1);
                        return tripsOnDate[i];
                    }

                    if (departureTime >= time)
                    {
                        tripDate = currDate;
                        return tripsOnDate[i];
                    }
                }
            }

            
            //scan the following days till maxDay and select first available trip
            while(currDate < maxDate)
            {
                currDate = currDate.AddDays(1);

                if(RouteTrips.ContainsKey(currDate) && RouteTrips[currDate].Count > 0)
                {
                    tripDate = currDate;
                    return RouteTrips[currDate][0];
                }
            }
            //No trip found in the specified timeframe
            tripDate = new DateOnly();
            return null;
        }

        public override string ToString()
        {
            return ShortName + ": From " + RouteStops[0] + " to " + RouteStops[RouteStops.Count - 1];
        }
    }
}

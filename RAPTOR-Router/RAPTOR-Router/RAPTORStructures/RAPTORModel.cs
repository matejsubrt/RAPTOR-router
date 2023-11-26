using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.GTFSParsing;

namespace RAPTOR_Router.RAPTORStructures
{
    /// <summary>
    /// Class representing the model of the timetable to be used for the connection search
    /// </summary>
    internal class RAPTORModel
    {
        /// <summary>
        /// The maximum number of meters between stops for them to be considered a transfer
        /// </summary>
        const int MAX_TRANSFER_DISTANCE = 750;
        public Dictionary<string, Route> routes { get; private set; } = new Dictionary<string, Route>();
        public Dictionary<string, Stop> stops { get; private set; } = new Dictionary<string, Stop>();

        /// <summary>
        /// Constructs the RAPTOR model object from the provided GTFS object
        /// </summary>
        /// <param name="gtfs">The GTFS object containing the timetable GTFS data to be used for routing</param>
        public RAPTORModel(GTFS gtfs)
        {
            LoadDataFromGtfs(gtfs);
        }
        /// <summary>
        /// Loads the raw gtfs date into useful structures to later be used by the router
        /// </summary>
        /// <param name="gtfs">The raw GTFS data to process</param>
        private void LoadDataFromGtfs(GTFS gtfs)
        {
            LoadStopsFromGtfsStops(gtfs.stops);

            LoadRoutes(gtfs);

            LoadStopRoutes();

            LoadStopTransfers();
        }

        /// <summary>
        /// Loads all the stops from the GTFS stops
        /// </summary>
        /// <param name="gtfsStops">The dictionary of stop ids and their GTFSStop objects</param>
        private void LoadStopsFromGtfsStops(Dictionary<string, GTFSStop> gtfsStops)
        {
            foreach(var gtfsStop in gtfsStops.Values)
            {
                if(gtfsStop.LocationType == 0)
                {
                    Stop stop = new Stop(gtfsStop.Id, gtfsStop.Name, gtfsStop.Lat, gtfsStop.Lon);
                    stops.Add(gtfsStop.Id, stop);
                }                
            }
        }

        /// <summary>
        /// Collects the unique routes from the raw GTFS data (where the GTFSRoutes are not unique), loads all the trips serving the routes and all the stops they serve
        /// </summary>
        /// <param name="gtfs">The raw GTFS data to process</param>
        private void LoadRoutes(GTFS gtfs)
        {
            Dictionary<string, Route> uniqueRoutes = new Dictionary<string, Route>();
            var gtfsTrips = gtfs.trips.Values;

            foreach (GTFSTrip gtfsTrip in gtfsTrips)
            {
                Route route;
                GTFSRoute gtfsRoute = gtfs.routes[gtfsTrip.RouteId];
                GTFSCalendar gtfsCalendar = gtfs.calendars[gtfsTrip.ServiceId];
                List<GTFSStopTime> gtfsStopTimes = gtfs.stopTimes[gtfsTrip.Id];
                List<GTFSCalendarDate> gtfsCalendarDates = new List<GTFSCalendarDate>();
                if (gtfs.calendarDates.ContainsKey(gtfsTrip.ServiceId))
                {
                    gtfsCalendarDates = gtfs.calendarDates[gtfsTrip.ServiceId];
                }

                //Get ids of all stops in the trip, create a unique identifier by combining the trip's routeId with its stopIds
                List<string> tripStopIds = gtfsStopTimes.GetStopIds();
                string uniqueRouteId = String.Concat(gtfsRoute.Id, String.Join(String.Empty, tripStopIds));
                if (uniqueRoutes.ContainsKey(uniqueRouteId))
                {
                    route = uniqueRoutes[uniqueRouteId];
                }
                else
                {
                    //create new route
                    route = new Route(uniqueRouteId, gtfsRoute.Id, gtfsRoute.ShortName, gtfsRoute.LongName);
                    uniqueRoutes.Add(uniqueRouteId, route);
                    //fill the route's RouteStops
                    foreach (string stopId in tripStopIds)
                    {
                        route.RouteStops.Add(stops[stopId]);
                    }
                }
                Trip trip = new Trip(gtfsStopTimes, route);

                //add all instances of current trip to the route's RouteTrips
                //mam kalendar - rika kdy jede, a calendardates - kdy nejede
                DateOnly from = gtfsCalendar.StartDate;
                DateOnly to = gtfsCalendar.EndDate;
                foreach (DateOnly date in DatesBetween(from, to))
                {
                    if (IsNormallyOperating(gtfsCalendar, gtfsCalendarDates, date))
                    {
                        if (route.RouteTrips.ContainsKey(date))
                        {
                            route.RouteTrips[date].Add(trip);
                        }
                        else
                        {
                            route.RouteTrips.Add(date, new List<Trip> { trip });
                        }
                    }
                }
                IEnumerable<GTFSCalendarDate> exceptionOperationDates =
                    from gtfsCalendarDate
                    in gtfsCalendarDates
                    where gtfsCalendarDate.ExceptionType == 1
                    select gtfsCalendarDate;
                foreach (GTFSCalendarDate calendarDate in exceptionOperationDates)
                {
                    if (route.RouteTrips.ContainsKey(calendarDate.Date))
                    {
                        route.RouteTrips[calendarDate.Date].Add(trip);
                    }
                    else
                    {
                        route.RouteTrips.Add(calendarDate.Date, new List<Trip> { trip });
                    }
                }

                foreach (List<Trip> tripsOnDate in route.RouteTrips.Values)
                {
                    tripsOnDate.Sort(Trip.CompareTrips);
                }
            }
            routes = uniqueRoutes;

            bool IsNormallyOperating(GTFSCalendar calendar, List<GTFSCalendarDate> calendarDates, DateOnly date)
            {
                int index = calendarDates.FindIndex(item => item.Date == date);

                //Normally operates on the date and is not cancelled
                bool result = (calendar.IsOperating(date) && (index < 0 || calendarDates[index].ExceptionType != 2));
                return result;
            }
            IEnumerable<DateOnly> DatesBetween(DateOnly from, DateOnly to)
            {
                if (to < from)
                {
                    throw new InvalidOperationException();
                }
                DateOnly currDate = from;
                while (currDate <= to)
                {
                    yield return currDate;
                    currDate = currDate.AddDays(1);
                }
            }
        }

        /// <summary>
        /// Loads all the routes serving all the stops
        /// </summary>
        private void LoadStopRoutes()
        {
            foreach(var route in routes.Values)
            {
                foreach(var routeStop in route.RouteStops)
                {
                    if (!routeStop.StopRoutes.Contains(route))
                    {
                        routeStop.StopRoutes.Add(route);
                    }
                }
            }
        }

        /// <summary>
        /// Loads all the possible transfers at each stop
        /// </summary>
        private void LoadStopTransfers()
        {
            foreach(Stop sourceStop in stops.Values)
            {
                foreach(Stop destStop in stops.Values)
                {
                    var distance = Stop.SimplifiedDistanceBetween(sourceStop, destStop);
                    if(distance > 0 && distance < MAX_TRANSFER_DISTANCE)
                    {
                        Transfer transferToDest = new Transfer(sourceStop, destStop, distance);
                        sourceStop.Transfers.Add(transferToDest);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a list of Stops that have a specified name (i.e. all the individual stops in a node with the specified name)
        /// </summary>
        /// <param name="stopName">The stop name to search for</param>
        /// <returns>List of all the stops with the specified name</returns>
        public List<Stop> GetStopsByName(string stopName)
        {
            List<Stop> result = new();
            foreach(Stop stop in stops.Values)
            {
                if(stop.Name == stopName)
                {
                    result.Add(stop);
                }
            }
            return result;
        }
    }
}

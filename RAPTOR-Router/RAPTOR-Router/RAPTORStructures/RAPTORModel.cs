using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.GTFSParsing;

namespace RAPTOR_Router.RAPTORStructures
{
    internal class RAPTORModel
    {
        const int MAX_TRANSFER_DISTANCE = 750;
        public Dictionary<string, Route> routes { get; private set; } = new Dictionary<string, Route>();
        public Dictionary<string, Stop> stops { get; private set; } = new Dictionary<string, Stop>();

        public RAPTORModel(GTFS gtfs)
        {
            LoadDataFromGtfs(gtfs);
        }
        private void LoadDataFromGtfs(GTFS gtfs)
        {
            var watch = new Stopwatch();
            Console.WriteLine("GTFS loaded");
            Console.WriteLine("Starting stopwatch");
            watch.Start();
            LoadStopsFromGtfsStops(gtfs.stops);
            Console.WriteLine(watch.Elapsed + " - Stops loaded");

            LoadAllUniqueRoutesFromGtfs(gtfs);
            Console.WriteLine(watch.Elapsed + " - Routes loaded");

            LoadStopRoutes();
            Console.WriteLine(watch.Elapsed + " - StopRoutes loaded");

            LoadStopTransfers();
            Console.WriteLine(watch.Elapsed + " - Transfers loaded");
            watch.Stop();
        }
        private void LoadAllUniqueRoutesFromGtfs(GTFS gtfs)
        {
            Dictionary<string, Route> uniqueRoutes = new Dictionary<string, Route>();

            List<GTFSTrip> gtfsTrips = gtfs.trips.Values.ToList();

            foreach(GTFSTrip gtfsTrip in gtfsTrips)
            {
                Route route;
                GTFSRoute gtfsRoute = gtfs.routes[gtfsTrip.RouteId];
                GTFSCalendar gtfsCalendar = gtfs.calendars[gtfsTrip.ServiceId];
                List<GTFSStopTime> gtfsStopTimes = gtfs.stopTimes[gtfsTrip.Id];
                List<GTFSCalendarDate> gtfsCalendarDates = new List<GTFSCalendarDate>();
                if (gtfs.calendarDates.ContainsKey(gtfsTrip.ServiceId)){
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
                //add all instances of current trip to the route's RouteTrips
                //mam kalendar - rika kdy jede, a calendardates - kdy nejede
                DateOnly from = gtfsCalendar.StartDate;
                DateOnly to = gtfsCalendar.EndDate;
                foreach(DateOnly date in DatesBetween(from, to))
                {
                    int index = gtfsCalendarDates.FindIndex(item => item.Date == date);
                    if (gtfsCalendar.IsOperating(date) && (index < 0 || gtfsCalendarDates[index].ExceptionType != 2))
                    {
                        Trip trip = new Trip(gtfsStopTimes, route, date);
                        route.RouteTrips.Add(trip);
                    }
                }
                IEnumerable<GTFSCalendarDate> exceptionOperationDates = 
                    from gtfsCalendarDate 
                    in gtfsCalendarDates 
                    where gtfsCalendarDate.ExceptionType == 1 
                    select gtfsCalendarDate;
                foreach(GTFSCalendarDate calendarDate in exceptionOperationDates)
                {
                    Trip trip = new Trip(gtfsStopTimes, route, calendarDate.Date);
                    route.RouteTrips.Add(trip);
                }
                route.RouteTrips.Sort(Trip.CompareTrips);
            }
            routes = uniqueRoutes;
        }
        private IEnumerable<DateOnly> DatesBetween(DateOnly from, DateOnly to)
        {
            if(to < from)
            {
                throw new InvalidOperationException();
            }
            DateOnly currDate = from;
            while(currDate <= to)
            {
                yield return currDate;
                currDate = currDate.AddDays(1);
            }
        }
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
        /*
        private void LoadUniqueRoutesFromGtfsRoutes(
            Dictionary<string, GTFSRoute> gtfsRoutes, 
            Dictionary<string, GTFSCalendar> gtfsCalendars, 
            Dictionary<string, GTFSTrip> gtfsTrips,
            Dictionary<string, List<GTFSStopTime>> gtfsStopTimes
        ){
            Dictionary<string, Route> uniqueRoutes = new Dictionary<string, Route>();

            foreach(var keyValuePair in gtfsTrips)
            {
                Route route;
                GTFSTrip gtfsTrip = keyValuePair.Value;

                GTFSRoute gtfsRoute = gtfsRoutes[gtfsTrip.RouteId];
                GTFSCalendar gtfsCalendar = gtfsCalendars[gtfsTrip.ServiceId];
                List<GTFSStopTime> gtfsTripStopTimes = gtfsStopTimes[gtfsTrip.Id];

                List<string> tripStopsIds = gtfsTripStopTimes.GetStopIds();

                //as every route in routes.txt can have multiple different versions of trips (i.e. tram returning to depot after 1/2 of the route)
                string uniqueRouteIdentifier = String.Concat(gtfsRoute.Id, String.Join(String.Empty, tripStopsIds));

                if (uniqueRoutes.ContainsKey(uniqueRouteIdentifier))
                {
                    route = uniqueRoutes[uniqueRouteIdentifier];
                }
                else
                {
                    route = new Route(uniqueRouteIdentifier, gtfsRoute.Id, gtfsRoute.ShortName, gtfsRoute.LongName);
                    route.RouteStops = GetStopsFromIds(tripStopsIds);
                    uniqueRoutes.Add(uniqueRouteIdentifier, route);
                }
                //List<Trip> allTripsByCalendar = gtfsTrip.GenerateAllTripsbyCalendar(gtfsCalendar, gtfsTripStopTimes);

                //Trip trip = new Trip(gtfsTripStopTimes, route);
                //route.RouteTrips.Add(trip);
            }
            routes = uniqueRoutes;
            Console.WriteLine();
        }
        */
        /*
        private List<Stop> GetStopsFromIds(List<string> ids)
        {
            List<Stop> stopsList = new List<Stop>();
            foreach(string id in ids)
            {
                stopsList.Add(stops[id]);
            }
            return stopsList;
        }
        */
    }
}

using RAPTOR_Router.Extensions;
using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Transit;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace RAPTOR_Router.Models.Static
{
    /// <summary>
    /// Class holding all the information about the public transit system and its stops, routes, trips, transfers and timetables
    /// </summary>
    public class TransitModel
    {
        /// <summary>
        /// The maximum number of meters between stops for them to be considered a transfer
        /// </summary>
        public const int MAX_TRANSFER_DISTANCE = 750;
        /// <summary>
        /// Dictionary of all the routes indexed by their unique identifier
        /// </summary>
        public Dictionary<string, Route> routes { get; private set; } = new Dictionary<string, Route>();
        /// <summary>
        /// Dictionary of all the stops indexed by their unique identifier
        /// </summary>
        public Dictionary<string, Stop> stops { get; private set; } = new Dictionary<string, Stop>();

        /// <summary>
        /// Constructs the RAPTOR model object from the provided GTFS object
        /// </summary>
        /// <param name="gtfs">The GTFS object containing the timetable GTFS data to be used for routing</param>
        public TransitModel(GTFS gtfs, List<ForbiddenCrossingLine> forbiddenCrossings)
        {
            LoadDataFromGtfs(gtfs, forbiddenCrossings);
        }
        /// <summary>
        /// Loads the raw gtfs date into useful structures to later be used by the router
        /// </summary>
        /// <param name="gtfs">The raw GTFS data to process</param>
        public void LoadDataFromGtfs(GTFS gtfs, List<ForbiddenCrossingLine> forbiddenCrossings)
        {
            LoadStopsFromGtfsStops(gtfs.stops);

            LoadRoutes(gtfs);

            LoadStopRoutes();

            LoadStopTransfers(forbiddenCrossings);
        }

        /// <summary>
        /// Loads all the stops from the GTFS stops
        /// </summary>
        /// <param name="gtfsStops">The dictionary of stop ids and their GTFSStop objects</param>
        private void LoadStopsFromGtfsStops(Dictionary<string, GTFSStop> gtfsStops)
        {
            foreach (var gtfsStop in gtfsStops.Values)
            {
                if (gtfsStop.LocationType == 0)
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

                string tripId = gtfsTrip.Id;
                if (gtfs.calendarDates.ContainsKey(gtfsTrip.ServiceId))
                {
                    gtfsCalendarDates = gtfs.calendarDates[gtfsTrip.ServiceId];
                }

                //Get ids of all stops in the trip, create a unique identifier by combining the trip's routeId with its stopIds
                List<string> tripStopIds = gtfsStopTimes.GetStopIds();
                string uniqueRouteId = string.Concat(gtfsRoute.Id, string.Join(string.Empty, tripStopIds));
                if (uniqueRoutes.ContainsKey(uniqueRouteId))
                {
                    route = uniqueRoutes[uniqueRouteId];
                }
                else
                {
                    //create new route
                    route = new Route(uniqueRouteId, gtfsRoute);
                    uniqueRoutes.Add(uniqueRouteId, route);
                    //fill the route's RouteStops
                    foreach (string stopId in tripStopIds)
                    {
                        route.RouteStops.Add(stops[stopId]);
                    }
                }
                Trip trip = new Trip(gtfsStopTimes, route, tripId);

                //add all instances of current trip to the route's RouteTrips
                //calendar - when does a trip normally operate, calendar date - exceptions
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
                bool result = calendar.IsOperating(date) && (index < 0 || calendarDates[index].ExceptionType != 2);
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
        /// For every stop loads all the routes that serve it
        /// </summary>
        private void LoadStopRoutes()
        {
            foreach (var route in routes.Values)
            {
                foreach (var routeStop in route.RouteStops)
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
        /// <param name="forbiddenCrossings">The list of lines forbidden to cross by a transfer</param>
        private void LoadStopTransfers(List<ForbiddenCrossingLine> forbiddenCrossings)
        {
            HashSet<Stop> stopsWithAllTransfersComputed = new HashSet<Stop>();
            foreach (Stop sourceStop in stops.Values)
            {
                foreach (Stop destStop in stops.Values)
                {
                    if (stopsWithAllTransfersComputed.Contains(destStop))
                    {
                        continue;
                    }
                    // Speeds up the calculation by pruning off stops that are already too far from each other in just one coordinate
                    if (DistanceExtensions.TooFarInOneDirection(sourceStop, destStop, MAX_TRANSFER_DISTANCE))
                    {
                        continue;
                    }
                    else
                    {
                        var distance = DistanceExtensions.SimplifiedDistanceBetween(sourceStop, destStop);

                        // Only add transfer, if the stops are at different locations OR if the stops are at the same location, but have different ids (xxxxxx and xxxxxxP) -> some stops are split into multiple stops in the GTFS
                        if ((distance > 0 || IsCityRegionalSameStopTransfer(sourceStop, destStop)) && distance < MAX_TRANSFER_DISTANCE && !forbiddenCrossings.ForbidsTransferBetween(sourceStop, destStop))
                        {
                            Transfer transferToDest = new Transfer(sourceStop, destStop, distance);
                            Transfer transferToSrc = new Transfer(destStop, sourceStop, distance);

                            transferToDest.OppositeTransfer = transferToSrc;
                            transferToSrc.OppositeTransfer = transferToDest;

                            sourceStop.Transfers.Add(transferToDest);
                            destStop.Transfers.Add(transferToSrc);

                        }
                    }
                    //sourceStop.AllTransfersComputed = true;
                    stopsWithAllTransfersComputed.Add(sourceStop);
                }
            }
            bool IsCityRegionalSameStopTransfer(Stop stop1, Stop stop2)
            {
                string cityStopId;
                string regionalStopId;
                if (stop1.Id.EndsWith("P"))
                {
                    cityStopId = stop1.Id;
                    regionalStopId = stop2.Id;
                }
                else if (stop2.Id.EndsWith("P"))
                {
                    cityStopId = stop2.Id;
                    regionalStopId = stop1.Id;
                }
                else
                {
                    return false;
                }

                return cityStopId.Substring(0, cityStopId.Length - 1) == regionalStopId;
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
            foreach (Stop stop in stops.Values)
            {
                if (stop.Name == stopName)
                {
                    result.Add(stop);
                }
            }
            return result;
        }

        /// <summary>
        /// Gets all the stops within the given radius of the given coordinates
        /// </summary>
        /// <param name="lat">The latitude of the point</param>
        /// <param name="lon">The longitude of the point</param>
        /// <param name="radius">The maximum distance of a near stop from the coordinates</param>
        /// <returns>List of all the stops within the given radius from the coordinates</returns>
        public List<Stop> GetStopsByLocation(double lat, double lon, int radius)
        {
            List<Stop> result = new();
            foreach (Stop stop in stops.Values)
            {
                if (DistanceExtensions.SimplifiedDistanceBetween(stop.Coords.Lat, stop.Coords.Lon, lat, lon) < radius)
                {
                    result.Add(stop);
                }
            }
            return result;
        }

        /// <summary>
        /// Finds out whether a stop exists within the given radius of the given coordinates
        /// </summary>
        /// <param name="lat">The latitude of the point</param>
        /// <param name="lon">The longitude of the point</param>
        /// <param name="radius">The maximum distance of a near stop from the coordinates</param>
        /// <returns>Bool specifying whether a near stop exists</returns>
        public bool NearStopExists(double lat, double lon, int radius)
        {
            foreach (Stop stop in stops.Values)
            {
                if (DistanceExtensions.SimplifiedDistanceBetween(stop.Coords.Lat, stop.Coords.Lon, lat, lon) < radius)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

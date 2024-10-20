using RAPTOR_Router.Models.Dynamic;
using RAPTOR_Router.Models.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Bike;
using System.Collections;

namespace RAPTOR_Router.RouteFinders
{
    public class DirectRouteFinder
    {
        /// <summary>
        /// The transit model holding all the static information about the transit network
        /// </summary>
        private TransitModel transitModel;

        private DelayModel delayModel;


        private List<Stop> srcStops;
        private List<Stop> destStops;

        internal DirectRouteFinder(TransitModel transitModel, DelayModel delayModel)
        {
            this.transitModel = transitModel;
            this.delayModel = delayModel;
        }

        //private List<Stop> GetNearRoutePoints(string stopName)
        //{
        //    List<Stop> stops = transitModel.GetStopsByName(stopName);
        //    List<BikeStation> bikeStations = new List<BikeStation>();
        //    return new Tuple<List<Stop>, List<BikeStation>>(stops, bikeStations);
        //}

        private List<Stop> GetAlternativeStops(Stop stop)
        {
            List<Stop> alternativeStops = new List<Stop>();
            alternativeStops.Add(stop); // Add the original stop
            alternativeStops.AddRange(transitModel.GetStopsByName(stop.Name)); // Add all stops with the same name
            alternativeStops.AddRange(transitModel.GetStopsByLocation(stop.Coords.Lat, stop.Coords.Lon, 150)); // Add all stops within 150m
            return alternativeStops;
        }


        public List<SearchResult.UsedTrip> GetAlternativeTripe(string srcStopName, string destStopName, DateTime time,
            int count, bool previous)
        {
            List<Stop> srcStops = transitModel.GetStopsByName(srcStopName);
            List<Stop> destStops = transitModel.GetStopsByName(destStopName);

            if (srcStops.Count == 0 || destStops.Count == 0)
            {
                return null;
            }

            Stop srcStop = srcStops.First();
            Stop destStop = destStops.First();
            return GetAlternativeTrips(srcStop.Id, destStop.Id, time, count, previous);
        }

        public List<SearchResult.UsedTrip> GetAlternativeTrips(string srcStopId, string destStopId, DateTime time, int count,
            bool previous)
        {
            if (count > 10)
            {
                return null;
            }
            DateTime worstAllowedReachTime = previous ? time.AddDays(-Settings.MAX_TRIP_LENGTH_DAYS) : time.AddDays(Settings.MAX_TRIP_LENGTH_DAYS);


            Stop srcStop = transitModel.stops[srcStopId];
            Stop destStop = transitModel.stops[destStopId];

            List<Stop> srcStops = GetAlternativeStops(srcStop);
            List<Stop> destStops = GetAlternativeStops(destStop);


            // Gather all routes passing through any of the source stops
            Dictionary<Route, Stop> routesFromSrcStops = new();

            foreach (Stop stop in srcStops)
            {
                foreach (Route route in stop.StopRoutes)
                {
                    routesFromSrcStops.TryAdd(route, stop);
                }
            }


            // Filter out routes that do not pass through any of the destination stops
            //Dictionary<Route, Tuple<Tuple<Stop, int>, Tuple<Stop, int>>> connectingRoutes = new();
            Dictionary<Route, Tuple<int, int>> connectingRoutes = new();

            foreach (var (route, stop) in routesFromSrcStops)
            {
                var srcIndex = route.RouteStops.IndexOf(stop);
                foreach(Stop destStop1 in destStops)
                {
                    var destIndex = route.RouteStops.IndexOf(destStop1);
                    if (destIndex != -1 && destIndex > srcIndex)
                    {
                        //Tuple<Stop, int> srcTuple = new Tuple<Stop, int>(stop, srcIndex);
                        //Tuple<Stop, int> destTuple = new Tuple<Stop, int>(destStop1, destIndex);
                        //Tuple<Tuple<Stop, int>, Tuple<Stop, int>> tuple = new (srcTuple, destTuple);
                        Tuple<int, int> tuple = new(srcIndex, destIndex);
                        connectingRoutes.TryAdd(route, tuple);
                        break;
                    }
                }
            }

            // For every route from any source stop to any destination stop \
            Dictionary<Trip, Stop> trips = new();
            SortedDictionary<DateTime, Tuple<Trip, int, int, bool, int, int>> sortedTrips = new();
            foreach (var (route, (srcIndex, destIndex)) in connectingRoutes)
            {
                Stop srcStop1 = route.RouteStops[srcIndex];
                // This is not very effective, but as there should always be only a few connecting routes, it should be fine
                DateOnly dateOnly = DateOnly.FromDateTime(time);
                TimeOnly timeOnly = TimeOnly.FromDateTime(time);

                //TODO: check if the trip should be included in the result or not - probably yes unless it is the one we are searching for alternatives for
                Trip firstTripDepartingAfterTime = route.GetEarliestTripDepartingAfterTimeAtStop(srcStop1, dateOnly,
                    timeOnly, worstAllowedReachTime, delayModel, out DateOnly tripDate);
                if (firstTripDepartingAfterTime == null)
                {
                    continue;
                }
                List<Tuple<DateOnly, Trip>> alternativeTrips = GetAlternativeTripsOnRoute(firstTripDepartingAfterTime, tripDate, count, previous);

                foreach (var (altTripDate, altTrip) in alternativeTrips)
                {
                    bool hasSrcDelayData = delayModel.TryGetDelay(altTripDate, altTrip.Id, srcIndex,
                        out int srcArrivalDelay, out int srcDepartureDelay);
                    bool hasDestDelayData = delayModel.TryGetDelay(altTripDate, altTrip.Id, destIndex, out int destArrivalDelay, out int destDepartureDelay);
                    TimeOnly arrivalTime = altTrip.StopTimes[destIndex].ArrivalTime.AddSeconds(destArrivalDelay);
                    DateOnly arrivalDate = (arrivalTime < altTrip.StopTimes[0].DepartureTime) ? altTripDate.AddDays(1) : altTripDate;
                    DateTime arrivalDateTime = DateTimeExtensions.FromDateAndTime(arrivalDate, arrivalTime);
                    int currTripDelay = GetCurrentTripDelay(altTrip, altTripDate);
                    sortedTrips.Add(arrivalDateTime.AddSeconds(destArrivalDelay), new Tuple<Trip, int, int, bool, int, int>(altTrip, srcIndex, destIndex, hasSrcDelayData, srcDepartureDelay, currTripDelay));
                }
            }

            // Clean up the trips - if a trip A departs before a trip B, but arrives after B, remove A
            List<DateTime> toRemove = new();


            RemoveOverlappingTrips();

            List<Tuple<DateTime, Trip, int, int, bool, int, int>> resultTrips = new();
            if (previous)
            {
                foreach (var item in sortedTrips.Skip(sortedTrips.Count - count))
                {
                    var (trip, srcIndex, destIndex, hasDelayData, srcDepDelay, currDelay) = item.Value;
                    resultTrips.Add(new Tuple<DateTime, Trip, int, int, bool, int, int>(item.Key, trip, srcIndex, destIndex, hasDelayData, srcDepDelay, currDelay));
                }
            }
            else
            {
                foreach (var item in sortedTrips.Take(count))
                {
                    var (trip, srcIndex, destIndex, hasDelayData, srcDepDelay, currDelay) = item.Value;
                    resultTrips.Add(new Tuple<DateTime, Trip, int, int, bool, int, int>(item.Key, trip, srcIndex, destIndex, hasDelayData, srcDepDelay, currDelay));
                }
            }


            List<SearchResult.UsedTrip> result = new();
            foreach (var (arrivalTime, trip, srcIndex, destIndex, hasDelayData, srcDepDelay, currDelay) in resultTrips)
            {
                List<SearchResult.StopPass> stopsPasses =
                    SearchResult.GetStopPassesList(trip.Route.RouteStops, trip.StopTimes, arrivalTime);
                
                SearchResult.UsedTrip usedTrip = new SearchResult.UsedTrip(stopsPasses, srcIndex, destIndex,
                    trip.Route.ShortName, trip.Route.Color, trip.Route.Type, hasDelayData, srcDepDelay, currDelay, trip.Id);
                result.Add(usedTrip);
            }

            return result;


            List<Tuple<DateOnly, Trip>> GetAlternativeTripsOnRoute(Trip firstTrip, DateOnly tripDate, int count, bool previous)
            {
                List<Tuple<DateOnly, Trip>> result = new();
                if (!previous)
                {
                    result.Add(new Tuple<DateOnly, Trip>(tripDate, firstTrip));
                }


                Route route = firstTrip.Route;
                List<Trip> tripsOnDate = route.RouteTrips[tripDate];
                int tripIndex = tripsOnDate.IndexOf(firstTrip);

                if (tripIndex == -1)
                {
                    throw new ArgumentException("The trip is not on the route on the specified date");
                }

                if (previous)
                {
                    int currIndex = tripIndex - 1;
                    while (currIndex >= 0 && result.Count < count)
                    {
                        Tuple<DateOnly, Trip> tuple = new Tuple<DateOnly, Trip>(tripDate, tripsOnDate[currIndex]);
                        result.Add(tuple);
                        currIndex--;
                    }

                    if (result.Count < count)
                    {
                        DateOnly prevDate = tripDate.AddDays(-1);
                        bool notZero = route.RouteTrips.TryGetValue(prevDate, out List<Trip> tripsOnPrevDate);
                        if (!notZero)
                        {
                            return result;
                        }
                        currIndex = tripsOnPrevDate.Count - 1;
                        while (currIndex >= 0 && result.Count < count)
                        {
                            Tuple<DateOnly, Trip> tuple = new Tuple<DateOnly, Trip>(prevDate, tripsOnPrevDate[currIndex]);
                            result.Add(tuple);
                            currIndex--;
                        }
                    }
                }
                else
                {
                    int currIndex = tripIndex + 1;
                    while (currIndex < tripsOnDate.Count && result.Count < count)
                    {
                        Tuple<DateOnly, Trip> tuple = new Tuple<DateOnly, Trip>(tripDate, tripsOnDate[currIndex]);
                        result.Add(tuple);
                        currIndex++;
                    }

                    if (result.Count < count)
                    {
                        DateOnly nextDate = tripDate.AddDays(1);
                        bool notZero = route.RouteTrips.TryGetValue(nextDate, out List<Trip> tripsOnNextDate);
                        if (!notZero)
                        {
                            return result;
                        }
                        currIndex = 0;
                        while (currIndex < tripsOnNextDate.Count && result.Count < count)
                        {
                            Tuple<DateOnly, Trip> tuple = new Tuple<DateOnly, Trip>(nextDate, tripsOnNextDate[currIndex]);
                            result.Add(tuple);
                            currIndex++;
                        }
                    }
                }

                return result;
            }

            void RemoveOverlappingTrips()
            {
                var tripsToRemove = new List<DateTime>();

                foreach (var outer in sortedTrips)
                {
                    var (tripA, srcIndexA, destIndexA, hasDelayDataA, srcDepDelayA, currDelayA) = outer.Value;
                    //var tripA = outer.Value.Item1;
                    DateTime tripAArrivalTime = outer.Key;
                    DateOnly tripAArrivalDate = DateOnly.FromDateTime(tripAArrivalTime);
                    DateOnly tripADepartureDate = (tripA.StopTimes[srcIndexA].DepartureTime > tripA.StopTimes[destIndexA].ArrivalTime)
                        ? tripAArrivalDate.AddDays(1)
                        : tripAArrivalDate;
                    int tripADepartureDelay = delayModel.TryGetDelay(tripADepartureDate, tripA.Id, srcIndexA, out int arrivalDelayA, out int departureDelayA)
                        ? departureDelayA
                        : 0;
                    DateTime tripADepartureTime = DateTimeExtensions.FromDateAndTime(tripADepartureDate, tripA.StopTimes[srcIndexA].DepartureTime).AddSeconds(tripADepartureDelay);
                    tripAArrivalTime = tripAArrivalTime.AddSeconds(currDelayA);

                    foreach (var inner in sortedTrips)
                    {
                        if (outer.Key == inner.Key) continue; // Skip comparing the same trip

                        var (tripB, srcIndexB, destIndexB, hasDelayDataB, srcDepDelayB, currDelayB) = inner.Value;
                        DateTime tripBArrivalTime = inner.Key;
                        DateOnly tripBArrivalDate = DateOnly.FromDateTime(tripBArrivalTime);
                        DateOnly tripBDepartureDate = (tripB.StopTimes[srcIndexB].DepartureTime > tripB.StopTimes[destIndexB].ArrivalTime)
                            ? tripBArrivalDate.AddDays(1)
                            : tripBArrivalDate;
                        int tripBDepartureDelay = delayModel.TryGetDelay(tripBDepartureDate, tripA.Id, srcIndexA, out int arrivalDelayB, out int departureDelayB)
                            ? departureDelayB
                            : 0;
                        DateTime tripBDepartureTime = DateTimeExtensions.FromDateAndTime(tripBDepartureDate, tripB.StopTimes[srcIndexB].DepartureTime).AddSeconds(tripBDepartureDelay);
                        tripBArrivalTime = tripBArrivalTime.AddSeconds(currDelayB);

                        //var tripB = inner.Value.Item1;

                        // Check if trip A departs before trip B but arrives after trip B
                        if (tripADepartureTime < tripBDepartureTime && tripAArrivalTime > tripBArrivalTime)
                        {
                            tripsToRemove.Add(outer.Key);
                            break; // Move on to the next trip after marking for removal
                        }
                    }
                }

                // Remove trips that have been marked for removal
                foreach (var key in tripsToRemove)
                {
                    sortedTrips.Remove(key);
                }
            }

            int GetCurrentTripDelay(Trip trip, DateOnly tripStartDate)
            {
                bool tripHasDelayData = delayModel.TripHasDelayData(tripStartDate, trip.Id);
                if (!tripHasDelayData)
                {
                    return 0;
                }
                TripStopDelays stopDelays = delayModel.GetTripStopDelays(tripStartDate, trip.Id);
                List<StopTime> stopTimes = trip.StopTimes;

                TimeOnly currTime = TimeOnly.FromDateTime(DateTime.Now);


                //bool haveLastStopDelay = stopDelays.TryGetStopDelay(0, out int lastReachedStopArrivalDelay, out int lastReachedStopDepartureDelay);
                int lastReachedStopDepartureDelay = 0;
                for (int i = 1; i < stopTimes.Count; i++)
                {
                    bool haveLastStopDelay = stopDelays.TryGetStopDelay(i, out int currReachedStopArrivalDelay, out int currReachedStopDepartureDelay);
                    if (!haveLastStopDelay)
                    {
                        break;
                    }
                    StopTime stopTime = stopTimes[i];
                    TimeOnly regularStopDepartureTime = stopTime.DepartureTime;
                    TimeOnly actualStopDepartureTime = regularStopDepartureTime.AddSeconds(currReachedStopDepartureDelay);

                    if (actualStopDepartureTime > currTime)
                    {
                        break;
                    }
                    else
                    {
                        lastReachedStopDepartureDelay = currReachedStopDepartureDelay;
                    }
                }
                return lastReachedStopDepartureDelay;
            }
        }
    }
}

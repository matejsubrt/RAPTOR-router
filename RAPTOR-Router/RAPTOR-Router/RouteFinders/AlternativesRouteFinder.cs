using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Requests;

namespace RAPTOR_Router.RouteFinders
{
    /// <summary>
    /// 
    /// </summary>
    public class AlternativesRouteFinder
    {
        private class AlternativeEntry : IComparable<AlternativeEntry>
        {
            public DateTime dateTime;
            public Trip altTrip;
            public DateOnly tripDate;
            public int srcIndex;
            public int destIndex;
            public bool hasSrcDelayData;
            public int srcDepartureDelay;
            public int currTripDelay;

            public AlternativeEntry(DateTime dateTime, Trip altTrip, DateOnly tripDate, int srcIndex, int destIndex, bool hasSrcDelayData, int srcDepartureDelay, int currTripDelay)
            {
                this.dateTime = dateTime;
                this.altTrip = altTrip;
                this.tripDate = tripDate;
                this.srcIndex = srcIndex;
                this.destIndex = destIndex;
                this.hasSrcDelayData = hasSrcDelayData;
                this.srcDepartureDelay = srcDepartureDelay;
                this.currTripDelay = currTripDelay;
            }

            public int CompareTo(AlternativeEntry? other)
            {
                if (other == null)
                    return 1;
                int dateTimeComparison = dateTime.CompareTo(other.dateTime);

                if (dateTimeComparison != 0)
                    return dateTimeComparison;

                
                int tripComparison = altTrip.Id.CompareTo(other.altTrip.Id);
                if (tripComparison != 0)
                    return tripComparison;

                return srcIndex.CompareTo(other.srcIndex);
            }

            public override string ToString()
            {
                return altTrip.Route.ShortName + ": " + dateTime.ToString();
            }
        }


        /// <summary>
        /// The transit model holding all the static information about the transit network
        /// </summary>
        private readonly TransitModel transitModel;

        /// <summary>
        /// The delay model holding the delay data for the transit network
        /// </summary>
        private readonly DelayModel delayModel;

        internal AlternativesRouteFinder(TransitModel transitModel, DelayModel delayModel)
        {
            this.transitModel = transitModel;
            this.delayModel = delayModel;
        }

        private List<Stop> GetAlternativeStops(Stop stop)
        {
            List<Stop> alternativeStops = new List<Stop>();
            alternativeStops.Add(stop); // Add the original stop
            alternativeStops.AddRange(transitModel.GetStopsByName(stop.Name)); // Add all stops with the same name
            alternativeStops.AddRange(transitModel.GetStopsByLocation(stop.Coords.Lat, stop.Coords.Lon, 150)); // Add all stops within 150m
            return alternativeStops;
        }


        public List<SearchResult.UsedTrip>? GetAlternativeTripe(string srcStopName, string destStopName, DateTime time,
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


        public AlternativeTripsApiResponseResult GetAlternativeTrips(AlternativeTripsRequest request)
        {
            AlternativeTripsApiResponseResult result = new();

            AlternativesSearchError error = request.Validate(transitModel);
            if (error != AlternativesSearchError.NoError)
            {
                result.Error = error;
                return result;
            }

            List<SearchResult.UsedTrip>? alternativeTrips = GetAlternativeTripe(request.srcStopId!, request.destStopId!, request.dateTime!.Value, request.count, request.previous);

            if (alternativeTrips is null || alternativeTrips.Count == 0)
            {
                result.Error = AlternativesSearchError.NoTripsFound;
            }
            else
            {
                result.Error = AlternativesSearchError.NoError;
                result.Alternatives = alternativeTrips;
            }

            return result;
        }


        private List<SearchResult.UsedTrip>? GetAlternativeTrips(string srcStopId, string destStopId, DateTime time, int count, bool previous)
        {
            Stop srcStop = transitModel.stops[srcStopId];
            Stop destStop = transitModel.stops[destStopId];

            // Get all stops that are alternatives to the source and destination stops
            // i.e. stops with the same name and stops within 150m
            List<Stop> srcStops = GetAlternativeStops(srcStop);
            List<Stop> destStops = GetAlternativeStops(destStop);

            // Gather all routes passing through any of the source stops
            Dictionary<Route, Stop> routesFromSrcStops = GetRoutesFromSrcStops();

            // Filter out routes that do not pass through any of the destination stops
            Dictionary<Route, Tuple<int, int>> connectingRoutes = GetConnectingRoutes();

            // For every route from any source stop to any destination stop,
            // find its <count> best connecting alternative trips
            SortedSet<AlternativeEntry> sortedTrips = GetSortedConnectingTrips();

            // Remove all trips that are dominated by another trip
            RemoveDominatedTrips();

            // Get the <count> best connecting trips out of all the alternatives
            List<AlternativeEntry> resultTrips = GetBestConnectingTrips();

            // Convert the alternative entries to used trips that will be returned
            List<SearchResult.UsedTrip> result = GetUsedTripsFromAlternativeEntries();

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
                        bool notZero = route.RouteTrips.TryGetValue(prevDate, out List<Trip>? tripsOnPrevDate);
                        if (!notZero)
                        {
                            return result;
                        }
                        currIndex = tripsOnPrevDate!.Count - 1;
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
                        bool notZero = route.RouteTrips.TryGetValue(nextDate, out List<Trip>? tripsOnNextDate);
                        if (!notZero)
                        {
                            return result;
                        }
                        currIndex = 0;
                        while (currIndex < tripsOnNextDate!.Count && result.Count < count)
                        {
                            Tuple<DateOnly, Trip> tuple = new Tuple<DateOnly, Trip>(nextDate, tripsOnNextDate[currIndex]);
                            result.Add(tuple);
                            currIndex++;
                        }
                    }
                }

                return result;
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

                TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Prague");
                DateTime dateTimeInZone = TimeZoneInfo.ConvertTime(DateTime.Now, timeZone);
                TimeOnly currTime = TimeOnly.FromDateTime(dateTimeInZone);

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

            Dictionary<Route, Stop> GetRoutesFromSrcStops()
            {
                Dictionary<Route, Stop> routesFromSrcStops = new();

                foreach (Stop stop in srcStops)
                {
                    foreach (Route route in stop.StopRoutes)
                    {
                        routesFromSrcStops.TryAdd(route, stop);
                    }
                }

                return routesFromSrcStops;
            }

            Dictionary<Route, Tuple<int, int>> GetConnectingRoutes()
            {
                Dictionary<Route, Tuple<int, int>> connectingRoutes = new();

                foreach (var (route, stop) in routesFromSrcStops)
                {
                    var srcIndex = route.RouteStops.IndexOf(stop);
                    foreach (Stop destStop1 in destStops)
                    {
                        var destIndex = route.RouteStops.IndexOf(destStop1);
                        if (destIndex != -1 && destIndex > srcIndex)
                        {
                            Tuple<int, int> tuple = new(srcIndex, destIndex);
                            connectingRoutes.TryAdd(route, tuple);
                            break;
                        }
                    }
                }

                return connectingRoutes;
            }

            SortedSet<AlternativeEntry> GetSortedConnectingTrips()
            {
                SortedSet<AlternativeEntry> sortedTrips = new();
                foreach (var (route, (srcIndex, destIndex)) in connectingRoutes)
                {
                    Stop srcStop1 = route.RouteStops[srcIndex];

                    //TODO: check if the trip should be included in the result or not - probably yes unless it is the one we are searching for alternatives for
                    Trip? firstTripDepartingAfterTime = route.GetEarliestTripDepartingAfterTimeAtStop(srcStop1, time, delayModel, out DateOnly tripDate);
                    if (firstTripDepartingAfterTime is null)
                    {
                        continue;
                    }
                    List<Tuple<DateOnly, Trip>> alternativeTrips = GetAlternativeTripsOnRoute(firstTripDepartingAfterTime, tripDate, count, previous);

                    foreach (var (altTripDate, altTrip) in alternativeTrips)
                    {
                        bool hasSrcDelayData = delayModel.TryGetDelay(altTripDate, altTrip.Id, srcIndex, out int srcArrivalDelay, out int srcDepartureDelay);
                        bool hasDestDelayData = delayModel.TryGetDelay(altTripDate, altTrip.Id, destIndex, out int destArrivalDelay, out int destDepartureDelay);

                        DateTime arrivalDateTime = altTrip.GetArrivalDateTime(destIndex, altTripDate);

                        int currTripDelay = GetCurrentTripDelay(altTrip, altTripDate);


                        AlternativeEntry alternativeEntry = new AlternativeEntry
                        (
                            arrivalDateTime.AddSeconds(destArrivalDelay),
                            altTrip,
                            altTripDate,
                            srcIndex,
                            destIndex,
                            hasSrcDelayData,
                            srcDepartureDelay,
                            currTripDelay
                        );
                        sortedTrips.Add(alternativeEntry);
                    }
                }

                return sortedTrips;
            }

            void RemoveDominatedTrips()
            {
                var entries = sortedTrips.ToList();

                for (int i = 0; i < entries.Count; i++)
                {
                    var entryA = entries[i];

                    for (int j = i + 1; j < entries.Count; j++)
                    {
                        var entryB = entries[j];

                        if (entryA.altTrip.StopTimes[entryA.srcIndex].DepartureTime < entryB.altTrip.StopTimes[entryB.srcIndex].DepartureTime &&
                            entryA.altTrip.StopTimes[entryA.destIndex].ArrivalTime >= entryB.altTrip.StopTimes[entryB.destIndex].ArrivalTime)
                        {
                            sortedTrips.Remove(entryA);
                        }
                    }
                }
            }

            List<AlternativeEntry> GetBestConnectingTrips()
            {
                List<AlternativeEntry> resultTrips = new();
                if (previous)
                {
                    foreach (var item in sortedTrips.Skip(sortedTrips.Count - count))
                    {
                        resultTrips.Add(item);
                    }
                }
                else
                {
                    foreach (var item in sortedTrips.Take(count))
                    {
                        resultTrips.Add(item);
                    }
                }

                return resultTrips;
            }

            List<SearchResult.UsedTrip> GetUsedTripsFromAlternativeEntries()
            {
                List<SearchResult.UsedTrip> result = new();
                foreach (AlternativeEntry entry in resultTrips)
                {
                    var trip = entry.altTrip;
                    var tripDate = entry.tripDate;
                    var srcIndex = entry.srcIndex;
                    var destIndex = entry.destIndex;
                    var hasDelayData = entry.hasSrcDelayData;
                    var srcDepDelay = entry.srcDepartureDelay;
                    var currDelay = entry.currTripDelay;
                    DateTime arrivalTime = entry.dateTime;

                    List<SearchResult.StopPass> stopsPasses =
                        SearchResult.GetStopPassesList(trip.Route.RouteStops, trip.StopTimes, tripDate);

                    SearchResult.UsedTrip usedTrip = new SearchResult.UsedTrip(stopsPasses, srcIndex, destIndex,
                        trip.Route.ShortName, trip.Route.Color, trip.Route.Type, hasDelayData, srcDepDelay, currDelay, trip.Id);
                    result.Add(usedTrip);
                }

                return result;
            }
        }
    }
}

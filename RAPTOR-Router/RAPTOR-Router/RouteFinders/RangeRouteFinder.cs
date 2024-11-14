//#define SEQUENTIAL


using RAPTOR_Router.Extensions;
using RAPTOR_Router.Models.Dynamic;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Custom;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.Structures.Requests;
using RAPTOR_Router.Structures.Transit;



namespace RAPTOR_Router.RouteFinders
{
    public class RangeRouteFinder : IRangeRouteFinder
    {
        /// <summary>
        /// The transit model holding all the static information about the transit network
        /// </summary>
        private TransitModel transitModel;
        private BikeModel bikeModel;
        private DelayModel delayModel;


        /// <summary>
        /// A set of currently marked stops
        /// </summary>
        private HashSet<Stop> markedStops = new();
        /// <summary>
        /// A set of all currently marked bike stations
        /// </summary>
        private HashSet<BikeStation> markedBikeStations = new();
        /// <summary>
        /// A dictionary storing for every currently marked route the stop at which it first can be boarded - i.e. the first marked stop it passes through
        /// </summary>
        //private Dictionary<Route, Stop> markedRoutesWithReachedStops = new();

        private Dictionary<Route, ReachedTrip> markedRoutesWithReachedTrips = new();


        /// <summary>
        /// The settings to be used for the connection search
        /// </summary>
        private Settings settings;

        private TimeComparator timeComp;
        private IndexComparator indexComp;
        private bool forward;
        private int timeMpl;


        /// <summary>
        /// Creates a new BasicRouter object
        /// </summary>
        /// <param name="forward">Whether the search is run forward or backward in time</param>
        /// <param name="settings">The settings to be used for the connection search</param>
        /// <param name="transitModel">The transit model holding all the static information about the transit network</param>
        /// <param name="delayModel">The delay model holding the current delay information</param>
        internal RangeRouteFinder(bool forward, Settings settings, TransitModel transitModel, BikeModel bikeModel, DelayModel delayModel)
        {
            this.settings = settings;
            this.transitModel = transitModel;
            this.bikeModel = bikeModel;
            this.forward = forward;
            this.timeComp = new TimeComparator(forward);
            this.indexComp = new IndexComparator(forward);
            this.timeMpl = forward ? 1 : -1;
            this.delayModel = delayModel;
        }

        /*public async Task<List<SearchResult>> FindConnectionsAsync(
            RouteFinderBuilder builder,
            bool forward,
            Settings settings,
            DateTime searchBeginRangeStart,
            DateTime searchBeginRangeEnd,
            string srcStopName,
            string destStopName)
        {
            // First, collect all times at which a trip departs from the source stop/arrives at the destination stop
            // Second, add the times specified in the first step plus/minus 1 minute -> this is to ensure there are alternatives to connections that begin with a trip
            // Third, if there are any large gaps between the times, add additional times to fill the gaps
            // Finally, run the search for all the times above
            //
            // This approach is not complete, as it does not ensure all possible connections within the time frame (i.e. connection sets where 
            // for every connection A and B, departure(A) < departure(B) ==> arrival(A) < arrival(B)) are found. However, we cannot directly
            // use the approach for the rRAPTOR algorithm specified in the RAPTOR documentation, as we also want to allow connections beginning with
            // a transfer or a bike ride, which this approach excludes. The rRAPTOR approach is to find all departures/arrivals within the time frame
            // and then run the search for all of them as search start times, which means that connections beginning/ending with a trip are prioritised over
            // connections beginning/ending with a transfer, which can be more efficient when the departure/arrival time of the search is within the gaps between
            // the times found in the first step.
            //
            // For example, let's say we are looking for a connection from stop A to stop B, and there are trips departing from A at times 05 and 15. The
            // rRAPTOR approach would mean only running the search with those two times as search start times, while there may exist a connection from
            // a stop C that is reachable from A by a transfer even after the trip at 05 departs, while arriving at B before the connection beginning
            // at stop A at time 15 arrives there.

            // To fully resolve this issue, there are essentially 2 options - either run the search for each minute within the timeframe (which 
            // is a fine enough granularity for Prague to not cause many issues with some connections being missed), which would be very inefficient,
            // or to first properly find all stops we can transfer and bike to from the search begin stop, then find all of their departure/arrival
            // times within the timeframe, shifted by the time it takes to get there, and then run the search for all those times, which now would
            // include departure/arrival times of trips, transfers and bike rides. This is the only way to ensure that all possible connections are found,
            // however in practice, this leads to a similar, if not worse efficiency situation as the first option, as the number of stops we can transfer
            // and bike to (and trip departure/arrival times at them) typically is comparable or sometimes even much larger than the number of minutes within 
            // the typical timeframe (which is expected to be around 15-30 minutes).
            //
            // Therefore, the approach we use here is a compromise between efficiency and result completeness. It is expected to work well for the typical
            // use case of finding connections within a timeframe of 15-30 minutes, while still allowing for connections that begin with a transfer or a bike ride.

            // In this approach, we also collect all the trip departure/arrival times at the search start stops, but we also add times that are 1 minute
            // later/earlier than these times (based on the search direction). This ensures, that if there is a connection that begins with a transfer or a bike ride that reaches the search
            // end stop at a better time than the next trip-beginning connection, it will be found. Note that this approach is not perfect, as further connections
            // beginning with a transfer may be shadowed by this first one, but it is a compromise that allows for a good balance between efficiency and result
            // completeness. To further ensure that the least possible important connections within the resulting gaps are missed, we also add additional times to fill the gaps
            // if the gaps are larger than a certain threshold.

            List<SearchResult> results = new();
            List<Stop> srcStops = transitModel.GetStopsByName(srcStopName);

            HashSet<DateTime> departureTimes = new();
            foreach (Stop stop in srcStops)
            {
                foreach (Route route in stop.StopRoutes)
                {
                    var routeDepartureTimes = route.GetTripTimesAtStopWithinRange(stop, searchBeginRangeStart,
                        searchBeginRangeEnd, delayModel, forward);
                    if (routeDepartureTimes is not null)
                    {
                        foreach (var depTime in routeDepartureTimes)
                        {
                            var newDepTime = depTime.AddSeconds(-depTime.Second);
                            departureTimes.Add(newDepTime);
                        }
                    }
                }
            }

            var orderedDepTimes = departureTimes.OrderBy(t => t).ToList();




            var tasks = new List<Task>();
            int numberOfCalls = orderedDepTimes.Count;
#if SEQUENTIAL
            for (int i = 0; i < numberOfCalls; i++)
            {
                var departureTime = orderedDepTimes[i];

                IRouteFinder router = builder.CreateUniversalRouteFinder(forward, settings);
                var searchResults = router.FindConnectionWithAlternatives(srcStopName, destStopName, departureTime);

                lock (results)
                {
                    foreach (var result in searchResults)
                    {
                        if (result is not null)
                        {
                            results.Add(result);
                        }
                    }
                }
            }
#else
            for (int i = 0; i < numberOfCalls; i++)
            {
                var departureTime = orderedDepTimes[i];//searchBeginRangeStart.AddMinutes(i * minutesDifference);

                tasks.Add(Task.Run(() =>
                {
                    ISimpleRoutingProvider router = builder.CreateRoutingProvider(forward, settings);

                    var searchResults = router.FindConnection(srcStopName, destStopName, departureTime, true);

                    if (searchResults is not null)
                    {
                        lock (results)
                        {
                            foreach (var result in searchResults)
                            {
                                // TODO: shouldnt this not be necessary?
                                if (result is not null)
                                {
                                    results.Add(result);
                                }
                            }
                            //results.Add(searchResult);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
#endif


            // sort by arrival time for cleaning up nonoptimal results
            results = results.OrderBy(r => r.ArrivalDateTime).ThenBy(r => r.DepartureDateTime).ToList();

            for (int i = 0; i < results.Count - 1; i++)
            {
                SearchResult res1 = results[i];
                SearchResult res2 = results[i + 1];

                if (res1.ArrivalDateTime >= res2.ArrivalDateTime)
                {
                    results.RemoveAt(i);
                    i--;
                }
            }

            // sort by departure time for final result list
            results = results.OrderBy(r => r.DepartureDateTime).ToList();

            return results;
        }*/

        

        public async Task<CompleteSearchResult> FindConnectionsAsync(
            ConnectionRequest request
        )
        {
            CompleteSearchResult result = new();

            var error = request.Validate(transitModel, bikeModel);//ValidateRequest(request);

            if(error != ConnectionSearchError.NoError)
            {
                result.Error = error;
                return result;
            }

            DateTime dateTime = request.dateTime!.Value;

            DateTime searchBeginRangeStart = request.byEarliestDeparture ? dateTime : dateTime.AddMinutes(-request.rangeLength);
            DateTime searchBeginRangeEnd = request.byEarliestDeparture ? dateTime.AddMinutes(request.rangeLength) : dateTime;

            List<SearchResult> results = new();

            List<Stop> searchBeginStops;
            if (request.byEarliestDeparture)
            {
                if (request.srcByCoords)
                {
                    searchBeginStops = transitModel.GetStopsByLocation(request.srcLat, request.srcLon, settings.GetMaxTransferDistance());
                }
                else
                {
                    searchBeginStops = transitModel.GetStopsByName(request.srcStopName!);
                }
            }
            else
            {
                if (request.destByCoords)
                {
                    searchBeginStops = transitModel.GetStopsByLocation(request.destLat, request.destLon, settings.GetMaxTransferDistance());
                }
                else
                {
                    searchBeginStops = transitModel.GetStopsByName(request.destStopName!);
                }
            }


            HashSet<DateTime> tripTimes = new();
            foreach (Stop stop in searchBeginStops)
            {
                foreach (Route route in stop.StopRoutes)
                {
                    var routeTripTimes = route.GetTripTimesAtStopWithinRange(stop, searchBeginRangeStart,
                        searchBeginRangeEnd, delayModel, forward);
                    if (routeTripTimes is not null)
                    {
                        foreach (var tripTime in routeTripTimes)
                        {
                            // Round the time down (earliest departure) or up (latest arrival) to the nearest minute
                            // This is to partly reduce the number of calls to the search algorithm
                            int secondsToAdd = request.byEarliestDeparture ? (60 - tripTime.Second) : -tripTime.Second;
                            var newTripTime = tripTime.AddSeconds(secondsToAdd);
                            tripTimes.Add(newTripTime);
                        }
                    }
                }
            }

            var orderedTripTimes = tripTimes.OrderBy(t => t).ToList();

            var tasks = new List<Task>();
            int numberOfCalls = orderedTripTimes.Count;

#if SEQUENTIAL
            for (int i = 0; i < numberOfCalls; i++)
            {
                var departureTime = orderedTripTimes[i];

                ISimpleRoutingProvider router = RouteFinderBuilder.CreateRoutingProvider(forward, settings);
                List<SearchResult>? searchResults;

                if (request.srcByCoords)
                {
                    Coordinates srcCoords = new Coordinates(request.srcLat, request.srcLon);
                    if (request.destByCoords)
                    {
                        Coordinates destCoords = new Coordinates(request.destLat, request.destLon);
                        searchResults = router.FindConnection(srcCoords, destCoords, departureTime, true);
                    }
                    else
                    {
                        searchResults = router.FindConnection(srcCoords, request.destStopName!, departureTime, true);
                    }
                }
                else
                {
                    if (request.destByCoords)
                    {
                        Coordinates destCoords = new Coordinates(request.destLat, request.destLon);
                        searchResults = router.FindConnection(request.srcStopName!, destCoords, departureTime, true);
                    }
                    else
                    {
                        searchResults = router.FindConnection(request.srcStopName!, request.destStopName!, departureTime, true);
                    }
                }

                if (searchResults is not null)
                {
                    lock (results)
                    {
                        foreach (var searchResult in searchResults)
                        {
                            // TODO: shouldnt this not be necessary?
                            if (searchResult is not null)
                            {
                                results.Add(searchResult);
                            }
                        }
                    }
                }
            }
#else
            for (int i = 0; i < numberOfCalls; i++)
            {
                var departureTime = orderedTripTimes[i];

                tasks.Add(Task.Run(() =>
                {
                    ISimpleRoutingProvider router = RouteFinderBuilder.CreateRoutingProvider(forward, settings);

                    List<SearchResult>? searchResults;

                    if (request.srcByCoords)
                    {
                        Coordinates srcCoords = new Coordinates(request.srcLat, request.srcLon);
                        if (request.destByCoords)
                        {
                            Coordinates destCoords = new Coordinates(request.destLat, request.destLon);
                            searchResults = router.FindConnection(srcCoords, destCoords, departureTime, true);
                        }
                        else
                        {
                            searchResults = router.FindConnection(srcCoords, request.destStopName!, departureTime, true);
                        }
                    }
                    else
                    {
                        if (request.destByCoords)
                        {
                            Coordinates destCoords = new Coordinates(request.destLat, request.destLon);
                            searchResults = router.FindConnection(request.srcStopName!, destCoords, departureTime, true);
                        }
                        else
                        {
                            searchResults = router.FindConnection(request.srcStopName!, request.destStopName!, departureTime, true);
                        }
                    }

                    //var searchResults = router.FindConnectionWithAlternatives(srcStopName, destStopName, departureTime);

                    if (searchResults is not null)
                    {
                        lock (results)
                        {
                            foreach (var result in searchResults)
                            {
                                // TODO: shouldnt this not be necessary?
                                if (result is not null)
                                {
                                    results.Add(result);
                                }
                            }
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
#endif

            results = results.OrderBy(r => r.ArrivalDateTime).ThenBy(r => r.DepartureDateTime).ToList();

            for (int i = 0; i < results.Count - 1; i++)
            {
                SearchResult res1 = results[i];
                SearchResult res2 = results[i + 1];

                if (res1.ArrivalDateTime >= res2.ArrivalDateTime)
                {
                    results.RemoveAt(i);
                    i--;
                }
            }

            // sort by departure time for final result list
            results = results.OrderBy(r => r.DepartureDateTime).ToList();

            if (results is not null && results.Count > 0)
            {
                result.Error = ConnectionSearchError.NoError;
                result.Results = results;
            }
            else
            {
                result.Error = ConnectionSearchError.NoConnectionFound;
            }

            return result;
        }
    }

    
}

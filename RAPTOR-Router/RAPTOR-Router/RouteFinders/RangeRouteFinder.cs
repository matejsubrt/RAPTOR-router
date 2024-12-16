//#define SEQUENTIAL


using RAPTOR_Router.Extensions;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Requests;
using RAPTOR_Router.Structures.Transit;





namespace RAPTOR_Router.RouteFinders
{
    /// <summary>
    /// A route finder that finds a set of the best connections within a given time range
    /// </summary>
    public class RangeRouteFinder : IRangeRouteFinder
    {
        const int startTimeCount = 5;


        /// <summary>
        /// The transit model holding all the static information about the transit network
        /// </summary>
        private TransitModel transitModel;
        /// <summary>
        /// The bike model holding the bike data
        /// </summary>
        private BikeModel bikeModel;
        /// <summary>
        /// The delay model holding the current delay information
        /// </summary>
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
        /// A dictionary holding for each marked route the reached trip on it
        /// </summary>
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
        /// <param name="bikeModel">The bike model holding the bike data</param>
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

        // Gets the stops that are reachable from the source/destination together with their walking distance
        private Dictionary<Stop, int> GetSearchBeginStopsWithTransferTimes(ConnectionRequest request)
        {
            Dictionary<Stop, int> searchBeginStopsWithTransferTimes = new();
            if (request.byEarliestDeparture)
            {
                if (request.srcByCoords)
                {
                    Coordinates srcCoords = new Coordinates(request.srcLat, request.srcLon);
                    searchBeginStopsWithTransferTimes = transitModel.GetStopsWithDistancesByLocation(srcCoords, settings.GetMaxTransferDistance());
                }
                else
                {
                    var stopsByName = transitModel.GetStopsByName(request.srcStopName!);

                    foreach (var stop in stopsByName)
                    {
                        if (searchBeginStopsWithTransferTimes.ContainsKey(stop))
                        {
                            searchBeginStopsWithTransferTimes[stop] = 0;
                        }
                        else
                        {
                            searchBeginStopsWithTransferTimes.Add(stop, 0);
                        }


                        foreach (var transfer in stop.Transfers)
                        {
                            var transferStop = transfer.To;
                            if (!searchBeginStopsWithTransferTimes.ContainsKey(transferStop))
                            {
                                searchBeginStopsWithTransferTimes.Add(transferStop, settings.GetAdjustedWalkingTransferTime(transfer.Distance));
                            }
                            else if (searchBeginStopsWithTransferTimes[transferStop] > settings.GetAdjustedWalkingTransferTime(transfer.Distance))
                            {
                                searchBeginStopsWithTransferTimes[transferStop] = settings.GetAdjustedWalkingTransferTime(transfer.Distance);
                            }
                        }
                    }
                }
            }
            else
            {
                if (request.destByCoords)
                {
                    Coordinates destCoords = new Coordinates(request.destLat, request.destLon);
                    searchBeginStopsWithTransferTimes = transitModel.GetStopsWithDistancesByLocation(destCoords, settings.GetMaxTransferDistance());
                }
                else
                {
                    var stopsByName = transitModel.GetStopsByName(request.destStopName!);

                    foreach (var stop in stopsByName)
                    {
                        if (searchBeginStopsWithTransferTimes.ContainsKey(stop))
                        {
                            searchBeginStopsWithTransferTimes[stop] = 0;
                        }
                        else
                        {
                            searchBeginStopsWithTransferTimes.Add(stop, 0);
                        }

                        foreach (var transfer in stop.Transfers)
                        {
                            var transferStop = transfer.To;
                            if (!searchBeginStopsWithTransferTimes.ContainsKey(transferStop))
                            {
                                searchBeginStopsWithTransferTimes.Add(transferStop, settings.GetAdjustedWalkingTransferTime(transfer.Distance));
                            }
                            else if (searchBeginStopsWithTransferTimes[transferStop] > settings.GetAdjustedWalkingTransferTime(transfer.Distance))
                            {
                                searchBeginStopsWithTransferTimes[transferStop] = settings.GetAdjustedWalkingTransferTime(transfer.Distance);
                            }
                        }
                    }
                }
            }

            return searchBeginStopsWithTransferTimes;
        }

        // Gets the first N ordered trip times at the source stop
        private List<DateTime> GetNOrderedTripTimes(Dictionary<Stop, int> searchBeginStopsWithTransferTimes, DateTime dateTime, bool byEarliestDeparture)
        {
            HashSet<DateTime> tripTimes = new();
            foreach (KeyValuePair<Stop, int> stopWithTransferDistance in searchBeginStopsWithTransferTimes)
            {
                var stop = stopWithTransferDistance.Key;
                var transferTime = stopWithTransferDistance.Value;

                foreach (Route route in stop.StopRoutes)
                {
                    var routeTripTimes = route.GetFirstNTripTimesAtStop(stop, dateTime, startTimeCount, forward, transferTime);
                    if (routeTripTimes is not null)
                    {
                        foreach (var tripTime in routeTripTimes)
                        {
                            var adjustedTripTime = tripTime;
                            // Round the time down (earliest departure) or up (latest arrival) to the nearest minute
                            // This is to partly reduce the number of calls to the search algorithm
                            int secondsToAdd = byEarliestDeparture ? (-adjustedTripTime.Second) : (60 - adjustedTripTime.Second);
                            var newTripTime = adjustedTripTime.AddSeconds(secondsToAdd);
                            tripTimes.Add(newTripTime);
                        }
                    }
                }
            }


            List<DateTime> orderedTripTimes;
            if (forward)
            {
                orderedTripTimes = tripTimes.OrderBy(t => t).Take(startTimeCount).ToList();
            }
            else
            {
                orderedTripTimes = tripTimes.OrderByDescending(t => t).Take(startTimeCount).ToList();
            }

            return orderedTripTimes;
        }

        // Removes dominated results
        private List<SearchResult> CleanupResults(List<SearchResult> results)
        {
            var newResults = new List<SearchResult>(results);
            if (forward)
            {
                newResults = newResults.OrderBy(r => r.ArrivalDateTime).ThenBy(r => r.DepartureDateTime).ToList();

                for (int i = 0; i < newResults.Count - 1; i++)
                {
                    SearchResult res1 = newResults[i];
                    SearchResult res2 = newResults[i + 1];

                    // As the results are ordered first by arrival and then by departure, if 2 results have the
                    // same arrival time, the one with the earlier departure time is removed. This ensures that
                    // dominated trips are removed, while still keeping intentionally included dominated trips.
                    // 
                    // -> If running search forward, for each start time, we may get multiple different results
                    // with a different number of trips if they are comparable in terms of arrival time.
                    // Obviously, one of these is the "best" one, while the other ones are dominated by it.
                    // However, in this case, we want to keep the dominated ones, as they are structurally
                    // different from the best one and provide alternatives. These dominated ones will have the
                    // same departure time as the best one, but a later arrival time.

                    // As opposed to this, we will also get results with certain departure time, which may
                    // have the same arrival time as another result with a later departure time. In this case,
                    // we want to remove the one with the earlier departure time, as it is dominated by the
                    // other one and does NOT necessarily provide a reasonable alternative

                    // This means, that we want to erase results dominated due to having later departure but
                    // same arrival and do NOT want to erase results dominated due to having same departure but
                    // later arrival.
                    if (res1.ArrivalDateTime == res2.ArrivalDateTime)
                    {
                        newResults.RemoveAt(i);
                        i--;
                    }
                }
            }
            else
            {
                newResults = newResults.OrderBy(r => r.DepartureDateTime).ThenBy(r => r.ArrivalDateTime).ToList();

                for (int i = 0; i < newResults.Count - 1; i++)
                {
                    SearchResult res1 = newResults[i];
                    SearchResult res2 = newResults[i + 1];


                    // The above comment applies here as well, but in the opposite way. We want to erase the
                    // results dominated due to having later arrival but same departure and do NOT want to erase
                    // results dominated due to having same arrival but earlier departure.
                    if (res1.DepartureDateTime == res2.DepartureDateTime)
                    {
                        newResults.RemoveAt(i + 1);
                        i--;
                    }
                }
            }

            return newResults;
        }

        // Gets the best reach time from the results
        private DateTime GetBestReachTime(List<SearchResult> results)
        {
            DateTime bestEndReachTime = forward ? DateTime.MaxValue : DateTime.MinValue;
            foreach (var result in results)
            {
                if (forward)
                {
                    if (result.ArrivalDateTime < bestEndReachTime)
                    {
                        bestEndReachTime = result.ArrivalDateTime;
                    }
                }
                else
                {
                    if (result.DepartureDateTime > bestEndReachTime)
                    {
                        bestEndReachTime = result.DepartureDateTime;
                    }
                }
            }

            return bestEndReachTime;
        }


        // Creates the router and dispatches the search to the appropriate search method
        private List<SearchResult>? FindConnections(
            bool srcByCoords, bool destByCoords,
            double srcLat, double srcLon, double destLat, double destLon,
            string srcStopName, string destStopName,
            DateTime dateTime, bool reverseDirection = false
            )
        {
            bool runForward = reverseDirection ? !forward : forward;
            ISimpleRoutingProvider router = RouteFinderBuilder.CreateRoutingProvider(runForward, settings);

            List<SearchResult>? searchResults;

            if (srcByCoords)
            {
                Coordinates srcCoords = new Coordinates(srcLat, srcLon);
                if (destByCoords)
                {
                    Coordinates destCoords = new Coordinates(srcLat, destLon);
                    searchResults = router.FindConnection(srcCoords, destCoords, dateTime, true);
                }
                else
                {
                    searchResults = router.FindConnection(srcCoords, destStopName!, dateTime, true);
                }
            }
            else
            {
                if (destByCoords)
                {
                    Coordinates destCoords = new Coordinates(srcLat, destLon);
                    searchResults = router.FindConnection(srcStopName!, destCoords, dateTime, true);
                }
                else
                {
                    searchResults = router.FindConnection(srcStopName!, destStopName!, dateTime, true);
                }
            }

            return searchResults;
        }

        
        /// <summary>
        /// Finds the best connections within a given time range
        /// </summary>
        /// <param name="request">The connection request object</param>
        /// <returns>The connection request response, including the error data if there was an error</returns>
        public async Task<ConnectionApiResponseResult> FindConnectionsAsync(
            ConnectionRequest request
        )
        {
            ConnectionApiResponseResult apiResponseResult = new();

            var error = request.Validate(transitModel, bikeModel);
            if(error != ConnectionSearchError.NoError)
            {
                apiResponseResult.Error = error;
                return apiResponseResult;
            }
            DateTime dateTime = request.dateTime!.Value;
            



            // Get the stops that are reachable from the source/destination with their walking distance
            Dictionary<Stop, int> searchBeginStopsWithTransferTimes = GetSearchBeginStopsWithTransferTimes(request);

            // Get the first N dateTimes (rounded to the lower whole minute) at which one can leave the source stop to get to a trip departing
            List<DateTime> orderedTripTimes = GetNOrderedTripTimes(searchBeginStopsWithTransferTimes, dateTime, request.byEarliestDeparture);



            List<SearchResult> results = new();
            var tasks = new List<Task>();
            int numberOfCalls = orderedTripTimes.Count;


            // For each of the N departure times, find the best connections
#if SEQUENTIAL
// This is the sequential version of the code. It is used for debugging and testing purposes.
            for (int i = 0; i < numberOfCalls; i++)
            {
                var departureTime = orderedTripTimes[i];

                List<SearchResult>? searchResults = FindConnections(
                    request.srcByCoords, request.destByCoords,
                    request.srcLat, request.srcLon, request.destLat, request.destLon,
                    request.srcStopName!, request.destStopName!,
                    departureTime
                    );

                if (searchResults is not null)
                {
                    foreach (var searchResult in searchResults)
                    {
                        if (searchResult is not null)
                        {
                            results.Add(searchResult);
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
                    List<SearchResult>? searchResults = FindConnections(
                        request.srcByCoords, request.destByCoords,
                        request.srcLat, request.srcLon, request.destLat, request.destLon,
                        request.srcStopName!, request.destStopName!,
                        departureTime
                        );

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


            // Remove dominated results
            results = CleanupResults(results);

            // Get the best reach time (there may be multiple results with different number of trips)
            DateTime bestEndReachTime = GetBestReachTime(results);
            

            // Remove results with long waiting times - they are not useful.
            List<SearchResult> removedResults = new();
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].HasLongWaiting())
                {
                    removedResults.Add(results[i]);
                    results.RemoveAt(i);
                    i--;
                }
            }


            // In case we removed all the connections due to long waiting times, we try to find new connections in the opposite direction
            // -> For that we use the best end reach time as the new start time -> we find the shortest connection, not just those that arrive
            // at the destination earliest, as there may be many of such connections
            if (removedResults.Count != 0 && removedResults.Count >= results.Count)
            {
                List<SearchResult>? newResults = FindConnections(
                    request.srcByCoords, request.destByCoords,
                    request.srcLat, request.srcLon, request.destLat, request.destLon,
                    request.srcStopName!, request.destStopName!,
                    bestEndReachTime, true
                    );

                if(newResults is not null)
                {
                    foreach (var newResult in newResults)
                    {
                        if (newResult is not null && newResult.DepartureDateTime > dateTime)
                        {
                            results.Add(newResult);
                        }
                    }
                }
            }
            

            // sort by departure time for final result list
            results = results.OrderBy(r => r.DepartureDateTime).ToList();

            if (results is not null && results.Count > 0)
            {
                apiResponseResult.Error = ConnectionSearchError.NoError;
                apiResponseResult.Results = results;
            }
            else
            {
                apiResponseResult.Error = ConnectionSearchError.NoConnectionFound;
            }

            return apiResponseResult;
        }
    }

    
}

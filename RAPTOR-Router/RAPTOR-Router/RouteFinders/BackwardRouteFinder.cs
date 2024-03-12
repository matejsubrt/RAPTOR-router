using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Interfaces;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Models.Dynamic;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.Structures.Custom;
using RAPTOR_Router.Structures.Generic;

namespace RAPTOR_Router.RouteFinders
{
    /// <summary>
    /// Basic router used for finding the best connection from a source stop to a destination stop using only public transit. It only takes the arrival time into account, i.e. is to be used in situations, where the arrival time is the only important factor (doesn't take into account comfort/transfers/...)
    /// </summary>
    public class BackwardRouteFinder : IBikeRouteFinder
    {
        private TransitModel raptorModel;
        /// <summary>
        /// The search model, that the router will use for the connection searching algorithm
        /// </summary>
        private BackwardSearchModel searchModel;

        private BikeModel bikeModel;
        /// <summary>
        /// A set of currently marked stops
        /// </summary>
        private HashSet<Stop> markedStops = new();

        private HashSet<BikeStation> markedBikeStations = new();
        /// <summary>
        /// A dictionary storing for every currently marked route the stop at which it will be exited - i.e. the last marked stop it passes through
        /// </summary>
        private Dictionary<Route, Stop> markedRoutesWithGetOffStops = new();
        /// <summary>
        /// The settings to be used for the connection search
        /// </summary>
        private Settings settings;

        /// <summary>
        /// The current round of the RAPTOR algorithm
        /// </summary>
        private int round = 0;

        /// <summary>
        /// Creates a new BasicRouter object
        /// </summary>
        /// <param name="settings">The settings to be used for the connection search</param>
        internal BackwardRouteFinder(Settings settings, TransitModel raptorModel, BikeModel bikeModel)
        {
            this.settings = settings;
            this.raptorModel = raptorModel;
            this.bikeModel = bikeModel;
        }



        /// <summary>
        /// Initiates the search by setting earliest arrival for source stops, marks them and improves arrival times for their neighbors in round 0
        /// </summary>
        /// <remarks>To be used for searches by stop name</remarks>
        private void InitiateSearchFromStops(bool useSharedBikes)
        {
            searchModel.SetDestStopsLatestArrival();
            if (useSharedBikes)
            {
                searchModel.SetDestBikeStationsLatestArrival();
            }

            MarkDestStops();
            if (useSharedBikes)
            {
                MarkDestBikeStations();
            }

            //TODO: check this after implementing search from coordinates
            ImproveByTransfers(useSharedBikes, true); // only from stops -> in 0th round, only transfers from source stops are considered


            void MarkDestStops()
            {
                foreach (Stop destStop in searchModel.destinationStops)
                {
                    markedStops.Add(destStop);
                }
            }
            void MarkDestBikeStations()
            {
                foreach (BikeStation destBikeStation in searchModel.destinationBikeStations)
                {
                    markedBikeStations.Add(destBikeStation);
                }
            }
        }

        /// <summary>
        /// Initiates the search by setting earliest arrival for all stops that can be reached from the custom source route point, marks them and improves arrival times in round 0
        /// </summary>
        /// <remarks>To be used for searches by coordinates</remarks>
        private void InitiateSearchFromCustomRoutePoint(CustomRoutePoint customDestRP, bool useSharedBikes)
        {
            foreach (ITransfer transfer in customDestRP.possibleTransfers)
            {
                IRoutePoint rp = transfer.GetSrcRoutePoint();
                if (transfer.Distance > settings.GetMaxTransferDistance())
                {
                    continue;
                }
                if (rp is Stop)
                {
                    DateTime departureTime = searchModel.GetArrivalTime().AddSeconds(-transfer.GetTransferTime(settings.WalkingPace));
                    searchModel.SetLatestDeparture(rp, departureTime);
                    searchModel.SetTransferDepartureInRound(rp, transfer, departureTime, round);
                    markedStops.Add((Stop)rp);
                }
                else if (useSharedBikes && rp is BikeStation)
                {
                    DateTime departureTime = searchModel.GetArrivalTime().AddSeconds(-transfer.GetTransferTime(settings.WalkingPace) + settings.BikeUnlockTime);
                    searchModel.SetLatestDeparture(rp, departureTime);
                    searchModel.SetTransferDepartureInRound(rp, transfer, departureTime, round);
                    markedBikeStations.Add((BikeStation)rp);
                }
            }
        }


        /// <summary>
        /// Accumulates all routes passing through the marked stops and finds the earliest marked stop for them
        /// </summary>
        private void AccumulateRoutes()
        {
            markedRoutesWithGetOffStops.Clear();
            foreach (Stop markedStop in markedStops)
            {
                foreach (Route route in markedStop.StopRoutes)
                {
                    int currStopIndex = route.GetLastStopIndex(markedStop);
                    if(currStopIndex != 0) // if the route is already marked and the marked stop is not the last one
                    {
                        if (markedRoutesWithGetOffStops.ContainsKey(route)) 
                        {
                            int prevGetOffStopIndex = route.GetLastStopIndex(markedRoutesWithGetOffStops[route]);

                            if (prevGetOffStopIndex < currStopIndex)
                            {
                                markedRoutesWithGetOffStops[route] = markedStop;
                            }
                        }
                        else
                        {
                            markedRoutesWithGetOffStops.Add(route, markedStop);
                        }
                    }                    
                }
                //TODO: Is this neccessary?
                markedStops.Remove(markedStop);
            }
        }
        /// <summary>
        /// Traverses all the marked routes, improving the arrival times and info for all stops where it is possible
        /// </summary>
        private void TraverseMarkedRoutes()
        {
            foreach (KeyValuePair<Route, Stop> pair in markedRoutesWithGetOffStops)
            {
                Route route = pair.Key;
                Stop getOffStop = pair.Value;
                DateOnly tripDate;

                //TODO: shouldnt this be just trip arrival?
                DateTime latestDepartureAtGetOffStopLastRound = searchModel.GetLatestDepartureInRound(getOffStop, round - 1);
                // in round 1, the arrival time is the start time -> no need for buffer
                if (/*round > 1 && */searchModel.RoutePointIsReachedByTripInRound(getOffStop, round - 1))
                {
                    latestDepartureAtGetOffStopLastRound = latestDepartureAtGetOffStopLastRound.AddSeconds(-settings.GetStationaryTransferMinimumSeconds());
                }

                if(/*getOffStop.Id == "U52Z101P" || */getOffStop.Id == "U52Z102P")
                {
                    Console.WriteLine();
                }

                Trip trip = route.GetLatestTripArrivingBeforeTimeAtStop(
                    getOffStop,
                    DateOnly.FromDateTime(latestDepartureAtGetOffStopLastRound),
                    TimeOnly.FromDateTime(latestDepartureAtGetOffStopLastRound),
                    Settings.MAX_TRIP_LENGTH_DAYS,
                    out tripDate
                );
                //TODO: TRIP CAN BE NULL?
                if(trip != null)
                {
                    if(route.ShortName == "C")
                    {
                        Console.WriteLine();
                    }
                    TraverseRoute(route, getOffStop, trip, tripDate);
                }
                //TraverseRoute(route, getOffStop, trip, tripDate);
            }

            void TraverseRoute(Route route, Stop getOffStop, in Trip trip, DateOnly tripDate)
            {
                Trip currTrip = trip;
                if(trip.Route.ShortName == "A")
                {
                    Console.WriteLine();
                }

                for (int i = route.GetLastStopIndex(getOffStop); i >= 0; i--)
                {
                    Stop currStop = route.RouteStops[i];



                    if (currTrip is not null)
                    {
                        StopTime stopTime = currTrip.StopTimes[i];

                        DateOnly realDate;
                        if (TripGoesOverMidnight(currTrip, route.GetLastStopIndex(getOffStop), i))
                            realDate = tripDate.AddDays(-1);
                        else
                            realDate = tripDate;


                        DateTime arrivalTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.ArrivalTime);
                        DateTime departureTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.DepartureTime);

                        if (DepartureTimeImprovesCurrBest(arrivalTime, currStop))
                        {

                            ImproveDepartureByTrip(currStop, arrivalTime, currTrip, getOffStop);
                            markedStops.Add(currStop);
                        }

                        if (ArrivalIsEarlierThanLastRoundDeparture(currStop, departureTime))
                        {
                            //TODO: same as above
                            DateTime latestDepartureLastRound = searchModel.GetLatestDepartureInRound(currStop, round - 1);
                            // in first round, no buffer needed
                            //TODO: check the round condition when brain functions
                            if (/*round != 1 && */searchModel.RoutePointIsReachedByTripInRound(currStop, round - 1))
                            {
                                latestDepartureLastRound = latestDepartureLastRound.AddSeconds(-settings.GetStationaryTransferMinimumSeconds());
                            }

                            if (latestDepartureLastRound < arrivalTime)
                            {
                                continue;
                            }

                            DateOnly latestDepartureLastRoundDate = DateOnly.FromDateTime(latestDepartureLastRound);
                            TimeOnly latestDepartureLastRoundTime = TimeOnly.FromDateTime(latestDepartureLastRound);



                            Trip newTrip = route.GetLatestTripArrivingBeforeTimeAtStop(
                                currStop,
                                latestDepartureLastRoundDate,
                                latestDepartureLastRoundTime,
                                Settings.MAX_TRIP_LENGTH_DAYS,
                                out tripDate);
                            if (newTrip != currTrip || searchModel.GetLatestDeparture(currStop) > searchModel.GetLatestDeparture(getOffStop))
                            {
                                currTrip = newTrip;
                                getOffStop = currStop;
                            }
                        }
                    }
                }

                bool TripGoesOverMidnight(Trip trip, int getOffStopIndex, int currStopIndex)
                {
                    return trip.StopTimes[getOffStopIndex].ArrivalTime < trip.StopTimes[currStopIndex].DepartureTime;
                }
                bool DepartureTimeImprovesCurrBest(DateTime departureTime, Stop stop)
                {
                    return departureTime > searchModel.GetLatestDeparture(stop)
                            && departureTime > searchModel.GetCurrentBestDepartureTime()
                            && departureTime >= searchModel.GetArrivalTime().AddDays(-Settings.MAX_TRIP_LENGTH_DAYS);
                }
                bool ArrivalIsEarlierThanLastRoundDeparture(Stop stop, DateTime arrivalTime)
                {
                    return searchModel.GetLatestDepartureInRound(stop, round - 1) > arrivalTime;
                }
                void ImproveDepartureByTrip(Stop stop, DateTime departureTime, Trip trip, Stop getOffStop)
                {
                    searchModel.SetTripDepartureInRound(stop, trip, getOffStop, departureTime, round);
                    //TODO: check
                    searchModel.SetLatestDeparture(stop, departureTime);

                    if (searchModel.sourceStops.Contains(stop) && departureTime > searchModel.GetCurrentBestDepartureTime())
                    {
                        searchModel.SetCurrentBestDepartureTime(departureTime);
                    }
                }
            }
        }
        void TraverseBikeRoutes()
        {
            HashSet<BikeStation> newMarkedBikeStations = new();
            foreach (BikeStation markedBikeStation in markedBikeStations)
            {
                if (searchModel.RoutePointIsReachedByBikeInRound(markedBikeStation, round - 1))
                {
                    continue;
                }
                Dictionary<BikeStation, int> distances = bikeModel.GetDistancesFromStation(markedBikeStation);
                foreach (KeyValuePair<BikeStation, int> pair in distances)
                {
                    BikeStation srcBikeStation = pair.Key;
                    int distance = pair.Value;

                    if (distance == -1)
                    {
                        continue;
                    }
                    if (settings.BikeMax15Minutes && GetCyclingTime(distance) > 15 * 60)
                    {
                        continue;
                    }
                    if(srcBikeStation.BikeCount == 0)
                    {
                        continue;
                    }


                    int cyclingTimeSeconds = GetCyclingTime(distance);
                    DateTime destStationLatestDepartureTime = searchModel.GetLatestDepartureInRound(markedBikeStation, round - 1);
                    DateTime departureUsingBicycle = destStationLatestDepartureTime.AddSeconds(-(cyclingTimeSeconds + settings.BikeUnlockTime));
                    if (DepartureTimeImprovesCurrBest(departureUsingBicycle, srcBikeStation))
                    {
                        if (srcBikeStation.Name == "P1-Quadrio - Purkynova")
                        {
                            Console.WriteLine();
                        }
                        ImproveDepartureByBikeTrip(markedBikeStation, srcBikeStation, departureUsingBicycle);
                        newMarkedBikeStations.Add(srcBikeStation);
                    }
                }
            }
            markedBikeStations = newMarkedBikeStations;
            int GetCyclingTime(int distance)
            {
                return (int)((distance / 1000.0 * settings.CyclingPace * 60) * settings.GetBikeTripLengthMultiplier());
            }
            bool DepartureTimeImprovesCurrBest(DateTime departureTime, BikeStation bikeStation)
            {
                return departureTime > searchModel.GetLatestDeparture(bikeStation)
                        && departureTime > searchModel.GetCurrentBestDepartureTime()
                        && departureTime >= searchModel.GetArrivalTime().AddDays(-Settings.MAX_TRIP_LENGTH_DAYS);
            }
            void ImproveDepartureByBikeTrip(BikeStation destBikeStation, BikeStation srcBikeStation, DateTime departureTime)
            {
                searchModel.SetBikeTripDepartureInRound(srcBikeStation, destBikeStation, departureTime, round);
                //TODO: check
                searchModel.SetLatestDeparture(srcBikeStation, departureTime);
                if (searchModel.sourceBikeStations.Contains(srcBikeStation) && departureTime > searchModel.GetCurrentBestDepartureTime())
                {
                    searchModel.SetCurrentBestDepartureTime(departureTime);
                }
            }
        }
        /// <summary>
        /// Takes all the stops that have been improved in current round and tries to improve all their neighbors by transfers
        /// </summary>
        private void ImproveByTransfers(bool useSharedBikes, bool onlyFromStops = false)
        {
            int maxTransferDistance = settings.GetMaxTransferDistance();
            HashSet<Stop> newMarkedStops = new();
            HashSet<BikeStation> newMarkedBikeStations = new();
            foreach (Stop markedStop in markedStops)
            {
                if(markedStop.Name == "Muzeum")
                {
                    Console.WriteLine();
                }
                foreach (Transfer transfer in markedStop.Transfers)
                {
                    // SWITCH -> the transfer was extracted from the destination stop as we are searching backwards. We switch to make from and to the real start and end of the transfer
                    var realSrc = transfer.To;
                    var realDest = transfer.From; // should be the same as markedStop
                    if ((transfer.Distance <= maxTransferDistance || realSrc.Name == realDest.Name) && TransferImprovesDepartureTime(transfer) && !searchModel.RoutePointIsReachedByTransferInRound(realDest, round))
                    {
                        ImproveArrivalByTransfer(transfer, false);
                        newMarkedStops.Add(realSrc);
                    }
                }
                if (useSharedBikes)
                {
                    foreach (BikeTransfer bikeTransfer in markedStop.BikeTransfers)
                    {
                        BikeStation realSrc = (BikeStation)bikeTransfer.GetDestRoutePoint();
                        Stop realDest = (Stop)bikeTransfer.GetSrcRoutePoint(); // should be the same as markedStop
                        if (bikeTransfer.Distance <= maxTransferDistance && TransferImprovesDepartureTime(bikeTransfer) && !searchModel.RoutePointIsReachedByTransferInRound(realDest, round))
                        {
                            if(realSrc.Name == "P1-Quadrio - Purkynova")
                            {
                                Console.WriteLine();
                            }
                            ImproveArrivalByTransfer(bikeTransfer, false);
                            newMarkedBikeStations.Add(realSrc);
                        }
                    }
                }
            }
            if (useSharedBikes && !onlyFromStops)
            {
                foreach (BikeStation markedBikeStation in markedBikeStations)
                {
                    foreach (BikeTransfer bikeTransfer in markedBikeStation.Transfers)
                    {
                        Stop realSrc = (Stop)bikeTransfer.GetDestRoutePoint();
                        BikeStation realDest = (BikeStation)bikeTransfer.GetSrcRoutePoint(); // should be the same as markedBikeStation
                        if (bikeTransfer.Distance <= maxTransferDistance && TransferImprovesDepartureTime(bikeTransfer) && !searchModel.RoutePointIsReachedByTransferInRound(realDest, round))
                        {
                            ImproveArrivalByTransfer(bikeTransfer, true, settings.BikeUnlockTime);
                            newMarkedStops.Add(realSrc);
                        }
                    }
                }
            }

            markedStops.UnionWith(newMarkedStops);
            if (useSharedBikes)
            {
                markedBikeStations.UnionWith(newMarkedBikeStations);
            }


            bool TransferImprovesDepartureTime(ITransfer transfer)
            {
                // SWITCH -> the transfer was extracted from the destination stop as we are searching backwards. We switch to make from and to the real start and end of the transfer
                IRoutePoint from = transfer.GetDestRoutePoint();
                IRoutePoint to = transfer.GetSrcRoutePoint();
                //DateTime currEarliestArrival = searchModel.GetEarliestArrivalInRound(transfer.To, round);
                DateTime currLatestDeparture = searchModel.GetLatestDeparture(from);
                DateTime latestDepartureWithTransfer = searchModel.GetLatestDepartureInRound(to, round);

                int stationaryTransferSeconds = settings.GetStationaryTransferMinimumSeconds();
                if (transfer.Distance == 0)
                {
                    // if transfer length is 0, the transfer is stationary -> it takes the time of the stationary transfer minimum
                    latestDepartureWithTransfer = latestDepartureWithTransfer.AddSeconds(-stationaryTransferSeconds);
                }
                else
                {
                    // if transfer length is not 0, the transfer is moving -> it takes the time of the moving transfer length, but if this is lower than the stationary transfer minimum, the stationary transfer minimum is used
                    latestDepartureWithTransfer = latestDepartureWithTransfer.AddSeconds(-Math.Max((int)(transfer.GetTransferTime(settings.WalkingPace) * settings.GetMovingTransferLengthMultiplier()), stationaryTransferSeconds));
                }

                return currLatestDeparture < latestDepartureWithTransfer;
            }
            void ImproveArrivalByTransfer(ITransfer transfer, bool toBikeStation, int extraSeconds = 0)
            {
                // SWITCH -> the transfer was extracted from the destination stop as we are searching backwards. We switch to make from and to the real start and end of the transfer
                IRoutePoint from = transfer.GetDestRoutePoint();
                IRoutePoint to = transfer.GetSrcRoutePoint();
                DateTime latestDepartureWithTransfer = searchModel.GetLatestDepartureInRound(to, round);

                int stationaryTransferSeconds = settings.GetStationaryTransferMinimumSeconds();
                if (transfer.Distance == 0)
                {
                    // if transfer length is 0, the transfer is stationary -> it takes the time of the stationary transfer minimum
                    latestDepartureWithTransfer = latestDepartureWithTransfer.AddSeconds(-(stationaryTransferSeconds + extraSeconds));
                }
                else
                {
                    // If the transfer is to a bike station, we do not need the safety buffer for transfers -> the bike is always there
                    if (toBikeStation)
                    {
                        latestDepartureWithTransfer = latestDepartureWithTransfer.AddSeconds(-((int)(transfer.GetTransferTime(settings.WalkingPace) * settings.GetMovingTransferLengthMultiplier()) + extraSeconds));
                    }
                    else
                    {
                        // if transfer length is not 0, the transfer is moving -> it takes the time of the moving transfer length, but if this is lower than the stationary transfer minimum, the stationary transfer minimum is used
                        latestDepartureWithTransfer = latestDepartureWithTransfer.AddSeconds(-(Math.Max((int)(transfer.GetTransferTime(settings.WalkingPace) * settings.GetMovingTransferLengthMultiplier()), stationaryTransferSeconds) + extraSeconds));
                    }
                }

                ////searchModel.SetEarliestArrivalInRound(transfer.To, round, earliestArrivalWithTransfer);
                searchModel.SetTransferDepartureInRound(from, transfer, latestDepartureWithTransfer, round);
                if (searchModel.GetLatestDeparture(from) < searchModel.GetLatestDepartureInRound(from, round))
                {
                    searchModel.SetLatestDeparture(from, searchModel.GetLatestDepartureInRound(from, round));
                }
            }
        }
        /// <summary>
        /// Finds the connection with the earliest arrival to one of the destinationStations in the searchModel
        /// </summary>
        /// <param name="searchModel">The search model to be used for finding the connection</param>
        /// <returns>The quickest connection from one of the sourceStops to one of the destinationStops in the searchModel</returns>
        internal SearchResult FindConnection(BackwardSearchModel searchModel)
        {
            this.searchModel = searchModel;

            InitiateSearchFromStops(settings.UseSharedBikes);
            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                ImproveByTransfers(settings.UseSharedBikes);
            }
            markedStops.Clear();
            markedRoutesWithGetOffStops.Clear();
            markedBikeStations.Clear();
            return searchModel.ExtractResult();
        }
        /// <summary>
        /// Finds the connection with the earliest arrival to a destination stop with the provided name, that departs from the source stop after the specified time.
        /// </summary>
        /// <param name="sourceStop">The exact name of the source stop</param>
        /// <param name="destStop">The exact name of the destination stop</param>
        /// <param name="departureTime">The departure date and time</param>
        /// <returns>The result of the search, null if no conection could be found.</returns>
        public SearchResult FindConnection(string sourceStop, string destStop, DateTime departureTime)
        {
            //bikeModel.UpdateStationStatus();
            if (sourceStop == destStop)
            {
                return null;
            }
            List<Stop> sourceStops = raptorModel.GetStopsByName(sourceStop);
            List<Stop> destStops = raptorModel.GetStopsByName(destStop);

            // If bikes cannot be used and either the source or the destination stop is not found, return null
            if (sourceStops.Count == 0 || destStops.Count == 0)
            {
                //Console.WriteLine("Incorrect stop name/s");
                return null;
            }
            //TODO: THIS NEEDS TO BE MODIFIED!!!!
            List<BikeStation> sourceBikeStations = bikeModel.GetNearStations(sourceStops[0], 100);
            List<BikeStation> destBikeStations = bikeModel.GetNearStations(destStops[0], 100);

            // If bikes can be used, but either the source or the destination stop or bike station is not found, return null
            if (settings.UseSharedBikes && (sourceStops.Count + sourceBikeStations.Count == 0 || destStops.Count + destBikeStations.Count == 0))
            {
                return null;
            }


            this.searchModel = new BackwardSearchModel(sourceStops, destStops, sourceBikeStations, destBikeStations, departureTime, settings);


            InitiateSearchFromStops(settings.UseSharedBikes);
            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                if (settings.UseSharedBikes)
                {
                    TraverseBikeRoutes();
                }
                ImproveByTransfers(settings.UseSharedBikes);
            }
            markedStops.Clear();
            markedRoutesWithGetOffStops.Clear();
            return searchModel.ExtractResult();
        }

        public SearchResult FindConnection(double srcLat, double srcLon, double destLat, double destLon, DateTime departureTime)
        {
            CustomRoutePoint source = new CustomRoutePoint("srcId", "Source", new Coordinates(srcLat, srcLon));
            CustomRoutePoint dest = new CustomRoutePoint("destId", "Destination", new Coordinates(destLat, destLon));



            List<Stop> sourceStops = raptorModel.GetStopsByLocation(srcLat, srcLon, settings.GetMaxTransferDistance());
            List<Stop> destStops = raptorModel.GetStopsByLocation(destLat, destLon, settings.GetMaxTransferDistance());

            List<BikeStation> sourceBikeStations = bikeModel.GetNearStations(srcLat, srcLon, settings.GetMaxTransferDistance());
            List<BikeStation> destBikeStations = bikeModel.GetNearStations(destLat, destLon, settings.GetMaxTransferDistance());

            if ((sourceStops.Count == 0 && sourceBikeStations.Count == 0) || (destStops.Count == 0 && destBikeStations.Count == 0))
            {
                //Console.WriteLine("Incorrect stop name/s");
                return null;
            }


            foreach (Stop srcStop in sourceStops)
            {
                source.AddTransferToRoutePoint(srcStop);
            }
            foreach (BikeStation srcStation in sourceBikeStations)
            {
                source.AddTransferToRoutePoint(srcStation);
            }

            foreach (Stop destStop in destStops)
            {
                dest.AddTransferFromRoutePoint(destStop);
            }
            foreach (BikeStation destStation in destBikeStations)
            {
                dest.AddTransferFromRoutePoint(destStation);
            }







            this.searchModel = new BackwardSearchModel(sourceStops, destStops, sourceBikeStations, destBikeStations, departureTime, settings);
            this.searchModel.sourceCustomRoutePoint = source;
            this.searchModel.destinationCustomRoutePoint = dest;

            InitiateSearchFromCustomRoutePoint(dest, settings.UseSharedBikes);
            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                TraverseBikeRoutes();
                ImproveByTransfers(settings.UseSharedBikes);
            }
            markedStops.Clear();
            markedRoutesWithGetOffStops.Clear();
            return searchModel.ExtractResult();
        }
    }
}

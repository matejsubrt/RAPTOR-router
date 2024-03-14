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
    /// Class used for finding the quickest connection from source to destination by latest possible arrival time
    /// </summary>
    public class BackwardRouteFinder : IRouteFinder
    {
        /// <summary>
        /// The transit model holding all the static information about the transit network
        /// </summary>
        private TransitModel transitModel;
        /// <summary>
        /// The bike model holding all the information about the shared bike systems and their stations
        /// </summary>
        private BikeModel bikeModel;
        /// <summary>
        /// The search model, that the router will use for the connection searching algorithm
        /// </summary>
        private BackwardSearchModel searchModel;
        

        /// <summary>
        /// A set of currently marked stops
        /// </summary>
        private HashSet<Stop> markedStops = new();
        /// <summary>
        /// A set of all currently marked bike stations
        /// </summary>
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
        /// <param name="transitModel">The transit model holding all the static information about the transit network</param>
        /// <param name="bikeModel">The bike model holding all the information about the shared bike systems and their stations</param>
        internal BackwardRouteFinder(Settings settings, TransitModel transitModel, BikeModel bikeModel)
        {
            this.settings = settings;
            this.transitModel = transitModel;
            this.bikeModel = bikeModel;
        }



        /// <summary>
        /// Initiates the search by setting latest arrival for destination stops, marks them and improves departure times for their neighbors in round 0
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
        /// Initiates the search by setting latest departure for all stops from which the custom destination route point can be reached, marks them and improves departure times in round 0
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
        /// Accumulates all routes passing through the marked stops and finds the latest marked stop for them
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
        /// Traverses all the marked routes, improving the departure times and info for all stops where it is possible
        /// </summary>
        private void TraverseMarkedRoutes()
        {
            foreach (KeyValuePair<Route, Stop> pair in markedRoutesWithGetOffStops)
            {
                Route route = pair.Key;
                Stop getOffStop = pair.Value;
                DateOnly tripDate;

                DateTime latestDepartureAtGetOffStopLastRound = searchModel.GetLatestDepartureInRound(getOffStop, round - 1);
                if (searchModel.RoutePointIsReachedByTripInRound(getOffStop, round - 1))
                {
                    latestDepartureAtGetOffStopLastRound = latestDepartureAtGetOffStopLastRound.AddSeconds(-settings.GetStationaryTransferMinimumSeconds());
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
                    TraverseRoute(route, getOffStop, trip, tripDate);
                }
                //TraverseRoute(route, getOffStop, trip, tripDate);
            }

            void TraverseRoute(Route route, Stop getOffStop, in Trip trip, DateOnly tripDate)
            {
                Trip currTrip = trip;

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
        
        /// <summary>
        /// For all marked bike stations, traverses all the possible bike trips to them and improves the departure times for all stops where it is possible
        /// </summary>
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
        /// <param name="useSharedBikes">Whether to use shared bikes in the search</param>
        /// <param name="onlyFromStops">Whether to improve only from stops, not from bike stations</param>
        /// <param name="DoNotImproveToRoutePoint">A functor that returns true if the route point should not be improved to - used in coords to coords searches where we shouldn't transfer to RoutePoints from which we can transfer to the destination</param>
        private void ImproveByTransfers(bool useSharedBikes, bool onlyFromStops = false, Func<IRoutePoint, bool> DoNotImproveToRoutePoint = null)
        {
            int maxTransferDistance = settings.GetMaxTransferDistance();
            HashSet<Stop> newMarkedStops = new();
            HashSet<BikeStation> newMarkedBikeStations = new();
            foreach (Stop markedStop in markedStops)
            {
                foreach (Transfer transfer in markedStop.Transfers)
                {
                    // SWITCH -> the transfer was extracted from the destination stop as we are searching backwards. We switch to the opposite transfer, which has the same semantics as the real-life connection direction
                    Transfer realTransfer = transfer.OppositeTransfer;
                    if ((transfer.Distance <= maxTransferDistance || realTransfer.From.Name == realTransfer.To.Name) && TransferImprovesDepartureTime(realTransfer) && !searchModel.RoutePointIsReachedByTransferInRound(realTransfer.To, round))
                    {
                        // the functor is used to not improve to certain stops/stations. This is typically used in coords to coords searches, where we do not want to improve by transfer to the stops from which we can transfer to the coord point.
                        if(DoNotImproveToRoutePoint is null || !DoNotImproveToRoutePoint(realTransfer.From))
                        {
                            ImproveArrivalByTransfer(realTransfer, false);
                            newMarkedStops.Add(realTransfer.From);
                        }                        
                    }
                }
                if (useSharedBikes)
                {
                    foreach (BikeTransfer bikeTransfer in markedStop.BikeTransfers)
                    {
                        BikeTransfer realTransfer = bikeTransfer.OppositeTransfer;
                        BikeStation realSrc = (BikeStation)realTransfer.GetSrcRoutePoint();
                        Stop realDest = (Stop)realTransfer.GetDestRoutePoint();
                        if (realTransfer.Distance <= maxTransferDistance && TransferImprovesDepartureTime(realTransfer) && !searchModel.RoutePointIsReachedByTransferInRound(realDest, round))
                        {
                            if (DoNotImproveToRoutePoint is null || !DoNotImproveToRoutePoint(realSrc))
                            {
                                ImproveArrivalByTransfer(realTransfer, false);
                                newMarkedBikeStations.Add(realSrc);
                            }
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
                        BikeTransfer realTransfer = bikeTransfer.OppositeTransfer;
                        Stop realSrc = (Stop)realTransfer.GetSrcRoutePoint();
                        BikeStation realDest = (BikeStation)realTransfer.GetDestRoutePoint();
                        if (realTransfer.Distance <= maxTransferDistance && TransferImprovesDepartureTime(realTransfer) && !searchModel.RoutePointIsReachedByTransferInRound(realDest, round))
                        {
                            if (DoNotImproveToRoutePoint is null || !DoNotImproveToRoutePoint(realSrc))
                            {
                                ImproveArrivalByTransfer(realTransfer, true, settings.BikeUnlockTime);
                                newMarkedStops.Add(realSrc);
                            }
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
                IRoutePoint from = transfer.GetSrcRoutePoint();
                IRoutePoint to = transfer.GetDestRoutePoint();

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
                IRoutePoint from = transfer.GetSrcRoutePoint();
                IRoutePoint to = transfer.GetDestRoutePoint();
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
        /// Finds the connection with the latest departure from one of the source RoutePoints in the searchModel
        /// </summary>
        /// <param name="searchModel">The search model to be used for finding the connection</param>
        /// <returns>The quickest connection from one of the sourceStops to one of the destinationStops in the searchModel arriving before latest arrival time</returns>
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
        /// Finds the connection with the latest departure from a source stop with the provided name, that arrives at the destination stop before the specified time.
        /// </summary>
        /// <param name="sourceStop">The exact name of the source stop</param>
        /// <param name="destStop">The exact name of the destination stop</param>
        /// <param name="arrivalTime">The arrival date and time</param>
        /// <returns>The result of the search, null if no conection could be found.</returns>
        public SearchResult FindConnection(string sourceStop, string destStop, DateTime arrivalTime)
        {
            //bikeModel.UpdateStationStatus();
            if (sourceStop == destStop)
            {
                return null;
            }
            List<Stop> sourceStops = transitModel.GetStopsByName(sourceStop);
            List<Stop> destStops = transitModel.GetStopsByName(destStop);

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


            this.searchModel = new BackwardSearchModel(sourceStops, destStops, sourceBikeStations, destBikeStations, arrivalTime, settings);


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

        /// <summary>
        /// Finds the connection with the latest departure from the source custom route point, that arrives at the destination custom route point before the specified time.
        /// </summary>
        /// <remarks>Used for searches by coordinates</remarks>
        /// <param name="srcLat">The latitude of the source point</param>
        /// <param name="srcLon">The longitude of the source point</param>
        /// <param name="destLat">The latitude of the destination point</param>
        /// <param name="destLon">The longitude of the destination point</param>
        /// <param name="arrivalTime">The arrival date and time</param>
        /// <returns>The result of the search, null if no conection could be found.</returns>
        public SearchResult FindConnection(double srcLat, double srcLon, double destLat, double destLon, DateTime arrivalTime)
        {
            CustomRoutePoint source = new CustomRoutePoint("srcId", "Source", new Coordinates(srcLat, srcLon));
            CustomRoutePoint dest = new CustomRoutePoint("destId", "Destination", new Coordinates(destLat, destLon));



            List<Stop> sourceStops = transitModel.GetStopsByLocation(srcLat, srcLon, settings.GetMaxTransferDistance());
            List<Stop> destStops = transitModel.GetStopsByLocation(destLat, destLon, settings.GetMaxTransferDistance());

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


            HashSet<IRoutePoint> sourceRoutePoints = new HashSet<IRoutePoint>();
            sourceRoutePoints.UnionWith(sourceStops);
            sourceRoutePoints.UnionWith(sourceBikeStations);




            this.searchModel = new BackwardSearchModel(sourceStops, destStops, sourceBikeStations, destBikeStations, arrivalTime, settings);
            this.searchModel.sourceCustomRoutePoint = source;
            this.searchModel.destinationCustomRoutePoint = dest;

            InitiateSearchFromCustomRoutePoint(dest, settings.UseSharedBikes);
            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                TraverseBikeRoutes();
                ImproveByTransfers(settings.UseSharedBikes, false, (x) => sourceRoutePoints.Contains(x));
            }
            markedStops.Clear();
            markedRoutesWithGetOffStops.Clear();
            return searchModel.ExtractResult();
        }
    }
}

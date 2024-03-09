using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Interfaces;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Models.Dynamic;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Extensions;

namespace RAPTOR_Router.RouteFinders
{
    /// <summary>
    /// Basic router used for finding the best connection from a source stop to a destination stop using only public transit. It only takes the arrival time into account, i.e. is to be used in situations, where the arrival time is the only important factor (doesn't take into account comfort/transfers/...)
    /// </summary>
    public class ForwardRouteFinder : IBikeRouteFinder
    {
        private RAPTORModel raptorModel;
        /// <summary>
        /// The search model, that the router will use for the connection searching algorithm
        /// </summary>
        private ForwardSearchModel searchModel;

        private BikeModel bikeModel;
        /// <summary>
        /// A set of currently marked stops
        /// </summary>
        private HashSet<Stop> markedStops = new();

        private HashSet<BikeStation> markedBikeStations = new();
        /// <summary>
        /// A dictionary storing for every currently marked route the stop at which it first can be boarded - i.e. the first marked stop it passes through
        /// </summary>
        private Dictionary<Route, Stop> markedRoutesWithGetOnStops = new();
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
        internal ForwardRouteFinder(Settings settings, RAPTORModel raptorModel, BikeModel bikeModel)
        {
            this.settings = settings;
            this.raptorModel = raptorModel;
            this.bikeModel = bikeModel;
        }

        

        /// <summary>
        /// Initiates the search by setting earliest arrival for source stops, marks them and improves arrival times for their neighbors in round 0
        /// </summary>
        private void InitiateSearch(bool useSharedBikes)
        {
            searchModel.SetSourceStopsEarliestArrival();
            if(useSharedBikes)
            {
                searchModel.SetSourceBikeStationsEarliestArrival();
            }

            MarkSourceStops();
            if(useSharedBikes)
            {
                MarkSourceBikeStations();
            }

            //TODO: check this after implementing search from coordinates
            ImproveByTransfers(useSharedBikes, true); // only from stops -> in 0th round, only transfers from source stops are considered
            

            void MarkSourceStops()
            {
                foreach (Stop sourceStop in searchModel.sourceStops)
                {
                    markedStops.Add(sourceStop);
                }
            }
            void MarkSourceBikeStations()
            {
                foreach(BikeStation sourceBikeStation in searchModel.sourceBikeStations)
                {
                    markedBikeStations.Add(sourceBikeStation);
                }
            }
        }
        /// <summary>
        /// Accumulates all routes passing through the marked stops and finds the earliest marked stop for them
        /// </summary>
        private void AccumulateRoutes()
        {
            markedRoutesWithGetOnStops.Clear();
            foreach (Stop markedStop in markedStops)
            {
                foreach (Route route in markedStop.StopRoutes)
                {
                    if (markedRoutesWithGetOnStops.ContainsKey(route))
                    {
                        if (route.GetStopIndex(markedRoutesWithGetOnStops[route]) > route.GetStopIndex(markedStop))
                        {
                            markedRoutesWithGetOnStops[route] = markedStop;
                        }
                    }
                    else
                    {
                        markedRoutesWithGetOnStops.Add(route, markedStop);
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
            foreach (KeyValuePair<Route, Stop> pair in markedRoutesWithGetOnStops)
            {
                Route route = pair.Key;
                Stop getOnStop = pair.Value;
                DateOnly tripDate;

                //TODO: shouldnt this be just trip arrival?
                DateTime earliestArrivalAtGetOnStopLastRound = searchModel.GetEarliestArrivalInRound(getOnStop, round - 1);
                // in round 1, the arrival time is the start time -> no need for buffer
                if (round > 1)
                {
                    earliestArrivalAtGetOnStopLastRound = earliestArrivalAtGetOnStopLastRound.AddSeconds(settings.GetStationaryTransferMinimumSeconds());
                }

                Trip trip = route.GetEarliestTripAtStop(
                    getOnStop,
                    DateOnly.FromDateTime(earliestArrivalAtGetOnStopLastRound),
                    TimeOnly.FromDateTime(earliestArrivalAtGetOnStopLastRound),
                    Settings.MAX_TRIP_LENGTH_DAYS,
                    out tripDate
                );

                TraverseRoute(route, getOnStop, trip, tripDate);
            }

            void TraverseRoute(Route route, Stop getOnStop, in Trip trip, DateOnly tripDate)
            {
                Trip currTrip = trip;
                
                for (int i = route.GetStopIndex(getOnStop); i < route.RouteStops.Count; i++)
                {
                    Stop currStop = route.RouteStops[i];

                    

                    if (currTrip is not null)
                    {
                        StopTime stopTime = currTrip.StopTimes[i];                        

                        DateOnly realDate;
                        if (TripGoesOverMidnight(currTrip, route.GetStopIndex(getOnStop), i))
                            realDate = tripDate.AddDays(1);
                        else
                            realDate = tripDate;


                        DateTime arrivalTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.ArrivalTime);
                        DateTime departureTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.DepartureTime);

                        if (ArrivalTimeImprovesCurrBest(arrivalTime, currStop))
                        {
                            
                            ImproveArrivalByTrip(currStop, arrivalTime, currTrip, getOnStop);
                            markedStops.Add(currStop);
                        }

                        if (DepartureIsLaterThanLastRoundArrival(currStop, departureTime))
                        {
                            //TODO: same as above
                            DateTime earliestArrivalLastRound = searchModel.GetEarliestArrivalInRound(currStop, round - 1);
                            // in first round, no buffer needed
                            if (round != 1)
                            {
                                if(searchModel.ArrivalInRoundIsByTrip(currStop, round - 1))
                                {
                                    earliestArrivalLastRound = earliestArrivalLastRound.AddSeconds(settings.GetStationaryTransferMinimumSeconds());
                                }                                
                            }

                            if(earliestArrivalLastRound > departureTime)
                            {
                                continue;
                            }

                            DateOnly earliestArrivalLastRoundDate = DateOnly.FromDateTime(earliestArrivalLastRound);
                            TimeOnly earliestArrivalLastRoundTime = TimeOnly.FromDateTime(earliestArrivalLastRound);



                            Trip newTrip = route.GetEarliestTripAtStop(
                                currStop,
                                earliestArrivalLastRoundDate,
                                earliestArrivalLastRoundTime,
                                Settings.MAX_TRIP_LENGTH_DAYS,
                                out tripDate);
                            if (newTrip != currTrip || searchModel.GetEarliestArrival(currStop) < searchModel.GetEarliestArrival(getOnStop))
                            {
                                currTrip = newTrip;
                                getOnStop = currStop;
                            }
                        }
                    }
                }

                bool TripGoesOverMidnight(Trip trip, int getOnStopIndex, int currStopIndex)
                {
                    return trip.StopTimes[getOnStopIndex].DepartureTime > trip.StopTimes[currStopIndex].ArrivalTime;
                }
                bool ArrivalTimeImprovesCurrBest(DateTime arrivalTime, Stop stop)
                {
                    return arrivalTime < searchModel.GetEarliestArrival(stop)
                            && arrivalTime < searchModel.GetCurrentBestArrivalTime()
                            && arrivalTime <= searchModel.GetDepartureTime().AddDays(Settings.MAX_TRIP_LENGTH_DAYS);
                }
                bool DepartureIsLaterThanLastRoundArrival(Stop stop, DateTime departureTime)
                {
                    return searchModel.GetEarliestArrivalInRound(stop, round - 1) < departureTime;
                }
                void ImproveArrivalByTrip(Stop stop, DateTime arrivalTime, Trip trip, Stop getOnStop)
                {
                    searchModel.SetTripArrivalInRound(stop, trip, getOnStop, arrivalTime, round);
                    //TODO: check
                    searchModel.SetEarliestArrival(stop, arrivalTime);

                    if (searchModel.destinationStops.Contains(stop) && arrivalTime < searchModel.GetCurrentBestArrivalTime())
                    {
                        searchModel.SetCurrentBestArrivalTime(arrivalTime);
                    }
                }
            }
        }
        void TraverseBikeRoutes()
        {
            HashSet<BikeStation> newMarkedBikeStations = new();
            foreach(BikeStation markedBikeStation in markedBikeStations)
            {
                if(markedBikeStation.BikeCount == 0)
                {
                    continue;
                }
                if (searchModel.RoutePointIsReachedByBikeInRound(markedBikeStation, round - 1))
                {
                    continue;
                }
                Dictionary<BikeStation, int> distances = bikeModel.GetDistancesFromStation(markedBikeStation);
                foreach(KeyValuePair<BikeStation, int> pair in distances)
                {
                    BikeStation destBikeStation = pair.Key;
                    int distance = pair.Value;

                    if(distance == -1)
                    {
                        continue;
                    }
                    if(settings.BikeMax15Minutes && GetCyclingTime(distance) > 15 * 60)
                    {
                        continue;
                    }
                    

                    int cyclingTimeSeconds = GetCyclingTime(distance);
                    DateTime srcStopArrivalTime = searchModel.GetEarliestArrivalInRound(markedBikeStation, round - 1);
                    DateTime arrivalUsingBicycle = srcStopArrivalTime.AddSeconds(cyclingTimeSeconds + settings.BikeUnlockTime);
                    if(ArrivalTimeImprovesCurrBest(arrivalUsingBicycle, destBikeStation))
                    {
                        ImproveArrivalByBikeTrip(markedBikeStation, destBikeStation, arrivalUsingBicycle);
                        newMarkedBikeStations.Add(destBikeStation);
                    }
                }
            }
            markedBikeStations = newMarkedBikeStations;
            int GetCyclingTime(int distance)
            {
                return (int)((distance / 1000.0 * settings.CyclingPace * 60) * settings.GetBikeTripLengthMultiplier());
            }
            bool ArrivalTimeImprovesCurrBest(DateTime arrivalTime, BikeStation bikeStation)
            {
                return arrivalTime < searchModel.GetEarliestArrival(bikeStation)
                        && arrivalTime < searchModel.GetCurrentBestArrivalTime()
                        && arrivalTime <= searchModel.GetDepartureTime().AddDays(Settings.MAX_TRIP_LENGTH_DAYS);
            }
            void ImproveArrivalByBikeTrip(BikeStation fromBikeStation, BikeStation toBikeStation, DateTime arrivalTime)
            {
                searchModel.SetBikeTripArrivalInRound(fromBikeStation, toBikeStation, arrivalTime, round);
                //TODO: check
                searchModel.SetEarliestArrival(toBikeStation, arrivalTime);
                if(searchModel.destinationBikeStations.Contains(toBikeStation) && arrivalTime < searchModel.GetCurrentBestArrivalTime())
                {
                    searchModel.SetCurrentBestArrivalTime(arrivalTime);
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
                foreach (Transfer transfer in markedStop.Transfers)
                {
                    if ((transfer.Distance <= maxTransferDistance || transfer.From.Name == transfer.To.Name) && TransferImprovesArrivalTime(transfer) && !searchModel.RoutePointIsReachedByTransferInRound(transfer.From, round))
                    {
                        ImproveArrivalByTransfer(transfer, false);
                        newMarkedStops.Add(transfer.To);
                    }
                }
                if (useSharedBikes)
                {
                    foreach (BikeTransfer bikeTransfer in markedStop.BikeTransfers)
                    {
                        if (bikeTransfer.Distance <= maxTransferDistance && TransferImprovesArrivalTime(bikeTransfer) && !searchModel.RoutePointIsReachedByTransferInRound(bikeTransfer.GetSrcRoutePoint(), round))
                        {
                            ImproveArrivalByTransfer(bikeTransfer, true, settings.BikeUnlockTime);
                            newMarkedBikeStations.Add((BikeStation)bikeTransfer.GetDestRoutePoint());
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
                        if (bikeTransfer.Distance <= maxTransferDistance && TransferImprovesArrivalTime(bikeTransfer) && !searchModel.RoutePointIsReachedByTransferInRound(bikeTransfer.GetSrcRoutePoint(), round))
                        {
                            ImproveArrivalByTransfer(bikeTransfer, false);
                            newMarkedStops.Add((Stop)bikeTransfer.GetDestRoutePoint());
                        }
                    }
                }
            }
            
            markedStops.UnionWith(newMarkedStops);
            if (useSharedBikes)
            {
                markedBikeStations.UnionWith(newMarkedBikeStations);
            }


            bool TransferImprovesArrivalTime(ITransfer transfer)
            {
                IRoutePoint from = transfer.GetSrcRoutePoint();
                IRoutePoint to = transfer.GetDestRoutePoint();
                //DateTime currEarliestArrival = searchModel.GetEarliestArrivalInRound(transfer.To, round);
                DateTime currEarliestArrival = searchModel.GetEarliestArrival(to);
                DateTime earliestArrivalWithTransfer = searchModel.GetEarliestArrivalInRound(from, round);

                int stationaryTransferSeconds = settings.GetStationaryTransferMinimumSeconds();
                if (transfer.Distance == 0)
                {
                    // if transfer length is 0, the transfer is stationary -> it takes the time of the stationary transfer minimum
                    earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds(stationaryTransferSeconds);
                }
                else
                {
                    // if transfer length is not 0, the transfer is moving -> it takes the time of the moving transfer length, but if this is lower than the stationary transfer minimum, the stationary transfer minimum is used
                    earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds(Math.Max((int)(transfer.GetTransferTime(settings.WalkingPace) * settings.GetMovingTransferLengthMultiplier()), stationaryTransferSeconds));
                }

                return currEarliestArrival > earliestArrivalWithTransfer;
            }
            void ImproveArrivalByTransfer(ITransfer transfer, bool toBikeStation, int extraSeconds = 0)
            {
                IRoutePoint from = transfer.GetSrcRoutePoint();
                IRoutePoint to = transfer.GetDestRoutePoint();
                DateTime earliestArrivalWithTransfer = searchModel.GetEarliestArrivalInRound(from, round);

                int stationaryTransferSeconds = settings.GetStationaryTransferMinimumSeconds();
                if (transfer.Distance == 0)
                {
                    // if transfer length is 0, the transfer is stationary -> it takes the time of the stationary transfer minimum
                    earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds(stationaryTransferSeconds + extraSeconds);
                }
                else
                {
                    // If the transfer is to a bike station, we do not need the safety buffer for transfers -> the bike is always there
                    if(toBikeStation)
                    {
                        earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds((int)(transfer.GetTransferTime(settings.WalkingPace) * settings.GetMovingTransferLengthMultiplier()) + extraSeconds);
                    }
                    else
                    {
                        // if transfer length is not 0, the transfer is moving -> it takes the time of the moving transfer length, but if this is lower than the stationary transfer minimum, the stationary transfer minimum is used
                        earliestArrivalWithTransfer = earliestArrivalWithTransfer.AddSeconds(Math.Max((int)(transfer.GetTransferTime(settings.WalkingPace) * settings.GetMovingTransferLengthMultiplier()), stationaryTransferSeconds) + extraSeconds);
                    }
                }

                //searchModel.SetEarliestArrivalInRound(transfer.To, round, earliestArrivalWithTransfer);
                searchModel.SetTransferArrivalInRound(to, transfer, earliestArrivalWithTransfer, round);
                if (searchModel.GetEarliestArrival(to) > searchModel.GetEarliestArrivalInRound(to, round))
                {
                    searchModel.SetEarliestArrival(to, searchModel.GetEarliestArrivalInRound(to, round));
                }
            }
        }
        /// <summary>
        /// Finds the connection with the earliest arrival to one of the destinationStations in the searchModel
        /// </summary>
        /// <param name="searchModel">The search model to be used for finding the connection</param>
        /// <returns>The quickest connection from one of the sourceStops to one of the destinationStops in the searchModel</returns>
        internal SearchResult FindConnection(ForwardSearchModel searchModel)
        {
            this.searchModel = searchModel;

            InitiateSearch(settings.UseSharedBikes);
            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                ImproveByTransfers(settings.UseSharedBikes);
            }
            markedStops.Clear();
            markedRoutesWithGetOnStops.Clear();
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

            if (sourceStops.Count == 0 || destStops.Count == 0)
            {
                //Console.WriteLine("Incorrect stop name/s");
                return null;
            }
            //TODO: THIS NEEDS TO BE MODIFIED!!!!
            List<BikeStation> sourceBikeStations = bikeModel.GetNearStations(sourceStops[0], 100);
            List<BikeStation> destBikeStations = bikeModel.GetNearStations(destStops[0], 100);
            
            this.searchModel = new ForwardSearchModel(sourceStops, destStops, sourceBikeStations, destBikeStations, departureTime, settings);


            InitiateSearch(settings.UseSharedBikes);
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
            markedRoutesWithGetOnStops.Clear();
            return searchModel.ExtractResult();
        }

        public SearchResult FindConnection(double srcLat, double srcLon, double destLat, double destLon, DateTime departureTime)
        {
            List<Stop> sourceStops = raptorModel.GetStopsByLocation(srcLat, srcLon, 100);
            List<Stop> destStops = raptorModel.GetStopsByLocation(destLat, destLon, 100);

            List<BikeStation> sourceBikeStations = bikeModel.GetNearStations(srcLat, srcLon, 100);
            List<BikeStation> destBikeStations = bikeModel.GetNearStations(destLat, destLon, 100);
            if ((sourceStops.Count == 0 && sourceBikeStations.Count == 0) || (destStops.Count == 0 && destBikeStations.Count == 0))
            {
                //Console.WriteLine("Incorrect stop name/s");
                return null;
            }
            this.searchModel = new ForwardSearchModel(sourceStops, destStops, sourceBikeStations, destBikeStations, departureTime, settings);

            InitiateSearch(settings.UseSharedBikes);
            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                TraverseBikeRoutes();
                ImproveByTransfers(settings.UseSharedBikes);
            }
            markedStops.Clear();
            markedRoutesWithGetOnStops.Clear();
            return searchModel.ExtractResult();
        }
    }
}

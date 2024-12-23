using System.Diagnostics;
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
using RAPTOR_Router.Structures.Requests;

namespace RAPTOR_Router.RouteFinders
{
    /// <summary>
    /// Class representing a trip that was reached during the algorithm together with the data about the stop it was reached on
    /// </summary>
    class ReachedTrip
    {
        /// <summary>
        /// The trip that was reached
        /// </summary>
        public Trip trip { get; }
        /// <summary>
        /// The stop at which it was reached
        /// </summary>
        public Stop reachedOnStop { get; }
        /// <summary>
        /// The start date of the trip
        /// </summary>
        public DateOnly tripDate { get; }
        /// <summary>
        /// The index of the stop in the route where the trip was reached
        /// </summary>
        public int stopIndex { get; }

        /// <summary>
        /// Creates a new ReachedTrip object
        /// </summary>
        /// <param name="trip">The trip</param>
        /// <param name="reachedOnStop">The stop where the trip was reached</param>
        /// <param name="tripDate">The start date of the trip</param>
        /// <param name="stopIndex">The index of the source stop</param>
        public ReachedTrip(Trip trip, Stop reachedOnStop, DateOnly tripDate, int stopIndex)
        {
            this.trip = trip;
            this.reachedOnStop = reachedOnStop;
            this.tripDate = tripDate;
            this.stopIndex = stopIndex;
        }
    }



    /// <summary>
    /// Class used for finding the quickest connection from source to destination by earliest possible departure time
    /// </summary>
    public class BasicRouteFinder : ISimpleRouteFinder, ISimpleRoutingProvider
    {
        /// <summary>
        /// The transit model holding all the static information about the transit network
        /// </summary>
        private readonly TransitModel transitModel;
        /// <summary>
        /// The bike model holding all the information about the shared bike systems and their stations
        /// </summary>
        private readonly BikeModel bikeModel;
        /// <summary>
        /// The search model, that the router will use for the connection searching algorithm
        /// </summary>
        private SearchModel? searchModel;
        /// <summary>
        /// The delay model holding all the current delay data for all active trips
        /// </summary>
        private readonly IDelayModel delayModel;


        /// <summary>
        /// A set of currently marked stops
        /// </summary>
        private HashSet<Stop> markedStops = new();
        /// <summary>
        /// A set of all currently marked bike stations
        /// </summary>
        private HashSet<BikeStation> markedBikeStations = new();
        /// <summary>
        /// A dictionary storing for every currently marked route the trip that was reached on it and the stop it was reached on
        /// </summary>
        private Dictionary<Route, ReachedTrip> markedRoutesWithReachedTrips = new();


        /// <summary>
        /// The settings to be used for the connection search
        /// </summary>
        private Settings settings;

        /// <summary>
        /// The current round of the RAPTOR algorithm
        /// </summary>
        private int round = 0;


        /// <summary>
        /// The time comparator used for comparing times during the search
        /// </summary>
        private TimeComparator timeComp;

        /// <summary>
        /// The index comparator used for comparing indices during the search
        /// </summary>
        private IndexComparator indexComp;

        /// <summary>
        /// Whether the search is forward or backward
        /// </summary>
        private bool forward;

        /// <summary>
        /// The time multiplier used for the search
        /// </summary>
        private int timeMpl;

        /// <summary>
        /// Assigns route points from a real-world transfer according to the search direction
        /// </summary>
        /// <param name="transfer">The real-world transfer (in the direction in which it would be used by the user)</param>
        /// <param name="imprFromPoint">The point from which the transfer was passed during the search</param>
        /// <param name="imprToPoint">The point to which the transfer leads during the search</param>
        private void OrderTransferPointsBySearchDirection(ITransfer transfer, out IRoutePoint imprFromPoint, out IRoutePoint imprToPoint)
        {
            if (forward)
            {
                imprFromPoint = transfer.GetSrcRoutePoint();
                imprToPoint = transfer.GetDestRoutePoint();
            }
            else
            {
                imprFromPoint = transfer.GetDestRoutePoint();
                imprToPoint = transfer.GetSrcRoutePoint();
            }
        }

        /// <summary>
        /// Creates a new BasicRouter object
        /// </summary>
        /// <param name="forward">Whether the search is forward or backward</param>
        /// <param name="settings">The settings to be used for the connection search</param>
        /// <param name="transitModel">The transit model holding all the static information about the transit network</param>
        /// <param name="bikeModel">The bike model holding all the information about the shared bike systems and their stations</param>
        /// <param name="delayModel">The delay model holding all the current delay data for all active trips</param>
        internal BasicRouteFinder(bool forward, Settings settings, TransitModel transitModel, BikeModel bikeModel, IDelayModel delayModel)
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



        /// <summary>
        /// Initiates the search by setting earliest arrival for source stops, marks them and improves arrival times for their neighbors in round 0
        /// </summary>
        /// <remarks>To be used for searches by stop name</remarks>
        private void InitiateSearchFromStops(bool useSharedBikes)
        {
            if (searchModel is null)
            {
                throw new InvalidOperationException("Search model not initialized");
            }

            searchModel.SetSearchBeginStopsReachTime();
            if (useSharedBikes)
            {
                searchModel.SetSearchBeginBikeStationsReachTime();
            }

            MarkSearchBeginStops();
            if (useSharedBikes)
            {
                MarkSearchBeginBikeStations();
            }

            //TODO: check this after implementing search from coordinates - method is correct?
            ImproveByTransfers(useSharedBikes, true); // only from stops -> in 0th round, only transfers from source stops are considered


            void MarkSearchBeginStops()
            {
                foreach (Stop searchBeginStop in searchModel.searchBeginStops)
                {
                    markedStops.Add(searchBeginStop);
                }
            }
            void MarkSearchBeginBikeStations()
            {
                foreach (BikeStation searchBeginBikeStation in searchModel.searchBeginBikeStations)
                {
                    markedBikeStations.Add(searchBeginBikeStation);
                }
            }
        }

        /// <summary>
        /// Initiates the search by setting earliest arrival for all stops that can be reached from the custom source route point, marks them and improves arrival times in round 0
        /// </summary>
        /// <remarks>To be used for searches by coordinates</remarks>
        private void InitiateSearchFromCustomRoutePoint(CustomRoutePoint customSearchBeginRP, bool useSharedBikes)
        {
            if (searchModel is null)
            {
                throw new InvalidOperationException("Search model not initialized");
            }

            foreach (ITransfer transfer in customSearchBeginRP.possibleTransfers)
            {
                IRoutePoint imprFromPoint, imprToPoint;
                OrderTransferPointsBySearchDirection(transfer, out imprFromPoint, out imprToPoint);
                if (transfer.Distance > settings.GetMaxTransferDistance())
                {
                    continue;
                }
                if (imprToPoint is Stop)
                {
                    // TODO: check - shouldnt there be some transferLengthMultiplier?
                    int transferDuration = timeMpl * settings.GetAdjustedWalkingTransferTime(transfer.Distance);
                    DateTime arrivalTime = searchModel.GetSearchBeginTime().AddSeconds(transferDuration);
                    searchModel.SetBestReachTime(imprToPoint, arrivalTime);
                    searchModel.SetTransferReachInRound(imprToPoint, transfer, arrivalTime, round);
                    markedStops.Add((Stop)imprToPoint);
                }
                else if (useSharedBikes && imprToPoint is BikeStation)
                {
                    int transferDuration = timeMpl * settings.GetAdjustedWalkingTransferTime(transfer.Distance);
                    DateTime arrivalTime = searchModel.GetSearchBeginTime().AddSeconds(transferDuration);
                    searchModel.SetBestReachTime(imprToPoint, arrivalTime);
                    searchModel.SetTransferReachInRound(imprToPoint, transfer, arrivalTime, round);
                    markedBikeStations.Add((BikeStation)imprToPoint);
                }
            }
        }


        /// <summary>
        /// Accumulates all routes passing through the marked stops and finds the earliest marked stop for them
        /// </summary>
        private void AccumulateRoutes()
        {
            if (searchModel is null)
            {
                throw new InvalidOperationException("Search model not initialized");
            }


            markedRoutesWithReachedTrips.Clear();

            foreach (Stop markedStop in markedStops)
            {
                foreach (Route route in markedStop.StopRoutes)
                {
                    int markedStopIndex = forward ? route.GetFirstStopIndex(markedStop) : route.GetLastStopIndex(markedStop);

                    if (markedRoutesWithReachedTrips.TryGetValue(route, out ReachedTrip? existingReachedTrip))
                    {
                        int existingGetOnStopIndex = existingReachedTrip.stopIndex;

                        if (indexComp.PrecedesInSearchDirection(markedStopIndex, existingGetOnStopIndex))
                        {
                            TryFindTransferableTrip(route, markedStop, markedStopIndex, true);
                        }
                    }
                    else
                    {
                        TryFindTransferableTrip(route, markedStop, markedStopIndex, false);
                    }
                }
                //TODO: Is this neccessary?
                markedStops.Remove(markedStop);
            }


            void TryFindTransferableTrip(Route route, Stop markedStop, int markedStopIndex, bool replacingOld)
            {
                DateTime bestReachTimeAtTraverseFromStopLastRound = searchModel.GetBestReachTimeInRound(markedStop, round - 1);
                // in round 1, the arrival time is the start time -> no need for buffer
                if (round > 1 && searchModel.RoutePointIsReachedByTripInRound(markedStop, round - 1))
                {
                    int stationaryTransferTime = timeMpl * settings.GetStationaryTransferMinimumSeconds();
                    bestReachTimeAtTraverseFromStopLastRound = bestReachTimeAtTraverseFromStopLastRound.AddSeconds(stationaryTransferTime);
                }

                DateOnly tripDate;

                Trip? trip = route.GetFirstTransferableTripAtStopByReachTime(
                    forward,
                    markedStop,
                    bestReachTimeAtTraverseFromStopLastRound,
                    delayModel,
                    out tripDate
                );

                if (trip is not null)
                {
                    ReachedTrip newReachedTrip = new(trip, markedStop, tripDate, markedStopIndex);
                    if (replacingOld)
                    {
                        markedRoutesWithReachedTrips[route] = newReachedTrip;
                    }
                    else
                    {
                        markedRoutesWithReachedTrips.Add(route, newReachedTrip);
                    }
                }
            }
        }

        /// <summary>
        /// Traverses all the marked routes, improving the arrival times and info for all stops where it is possible
        /// </summary>
        private void TraverseMarkedRoutes()
        {
            if (searchModel is null)
            {
                throw new InvalidOperationException("Search model not initialized");
            }


            foreach(KeyValuePair<Route, ReachedTrip> pair in markedRoutesWithReachedTrips)
            {
                Route route = pair.Key;
                ReachedTrip reachedTrip = pair.Value;

                Stop traverseFromStop = reachedTrip.reachedOnStop;
                Trip trip = reachedTrip.trip;
                DateOnly tripDate = reachedTrip.tripDate;

                if (trip is not null)
                {
                    TraverseRoute(route, traverseFromStop, trip, tripDate);
                }
            }

            void TraverseRoute(Route route, Stop traverseFromStop, Trip trip, DateOnly tripDate)
            {
                DateOnly currTripDate = tripDate;
                Trip currTrip = trip;

                bool tripHasDelayData = delayModel.TripHasDelayData(currTripDate, currTrip.Id);
                TripStopDelays? tripStopDelays = null;
                if (tripHasDelayData)
                {
                    tripStopDelays = delayModel.GetTripStopDelaysUnsafe(currTripDate, currTrip.Id);
                }

                if (forward)
                {
                    for (int i = route.GetFirstStopIndex(traverseFromStop); i < route.RouteStops.Count; i++)
                    {
                        ProcessStopAtIndex(i);
                    }
                }
                else
                {
                    for (int i = route.GetLastStopIndex(traverseFromStop); i >= 0; i--)
                    {
                        ProcessStopAtIndex(i);
                    }
                }

                void ProcessStopAtIndex(int i)
                {
                    Stop currStop = route.RouteStops[i];


                    int daysToAddWhenOverMidnight = forward ? 1 : -1;
                    int firstStopIndexInDirOfSearch = forward ? route.GetFirstStopIndex(traverseFromStop) : route.GetLastStopIndex(traverseFromStop);

                    StopTime stopTime = currTrip.StopTimes[i];


                    int arrivalDelay = 0;
                    int departureDelay = 0;
                    bool stopHasDelayData = false;
                    if (tripHasDelayData)
                    {
                        stopHasDelayData = tripStopDelays!.TryGetStopDelay(i, out arrivalDelay, out departureDelay);


                        // Sometimes there is a bug in the GTFS realtime data where not all stops have delay data, but the trip has delay data
                        // in that case, if this is a stop after the last stop with delay data, the delay data should be the same as the last stop with delay data
                        if (!stopHasDelayData && i >= tripStopDelays.Count && tripStopDelays.Count < trip.StopTimes.Count)
                        {
                            (arrivalDelay, departureDelay) = tripStopDelays.GetLastStopDelay();
                            stopHasDelayData = true;
                        }
                    }


                    //DateOnly realDate;
                    //if (TripGoesOverMidnight(currTrip, firstStopIndexInDirOfSearch, i))
                    //    realDate = currTripDate.AddDays(daysToAddWhenOverMidnight);
                    //else
                    //    realDate = currTripDate;


                    //DateTime regularArrivalTime = DateTimeExtensions.FromDateAndTime(currTripDate, stopTime.ArrivalTime);
                    //DateTime regularDepartureTime = DateTimeExtensions.FromDateAndTime(currTripDate, stopTime.DepartureTime);
                    DateTime regularArrivalTime =
                        currTrip.GetArrivalDateTime(i, currTripDate);
                    DateTime regularDepartureTime =
                        currTrip.GetDepartureDateTime(i, currTripDate);

                    DateTime actualArrivalTime = stopHasDelayData ? regularArrivalTime.AddSeconds(arrivalDelay) : regularArrivalTime;
                    DateTime actualDepartureTime = stopHasDelayData ? regularDepartureTime.AddSeconds(departureDelay) : regularDepartureTime;

                    DateTime searchReachTime = forward ? actualArrivalTime : actualDepartureTime;
                    DateTime searchLeaveTime = forward ? actualDepartureTime : actualArrivalTime;

                    bool improved = searchModel.TryImproveReachTimeByTrip(currStop, searchReachTime, currTrip, currTripDate, traverseFromStop, round);
                    if (improved)
                    {
                        markedStops.Add(currStop);
                    }

                    if (SearchLeaveTimeIsTransferableFromLastRoundReach(currStop, actualDepartureTime))
                    {
                        //TODO: same as above
                        DateTime bestReachTimeLastRound = searchModel.GetBestReachTimeInRound(currStop, round - 1);
                        // in first round, no buffer needed
                        //TODO: check if round is necessary
                        if (round != 1)
                        {
                            if (searchModel.RoutePointIsReachedByTripInRound(currStop, round - 1))
                            {
                                int stationaryTransferTime = timeMpl * settings.GetStationaryTransferMinimumSeconds();
                                bestReachTimeLastRound = bestReachTimeLastRound.AddSeconds(stationaryTransferTime);
                            }
                        }

                        if (!timeComp.ImprovesTime(bestReachTimeLastRound, searchLeaveTime))
                        {
                            return;
                        }


                        DateOnly newTripDate;
                        Trip? newTrip = route.GetFirstTransferableTripAtStopByReachTime(
                            forward,
                            currStop,
                            bestReachTimeLastRound,
                            delayModel,
                            out newTripDate);
                        if (newTrip != currTrip || timeComp.ImprovesTime(searchModel.GetBestReachTime(currStop), searchModel.GetBestReachTime(traverseFromStop)))
                        {
                            // newTrip should only be null when the route contains the same stop twice, reaches it the second time, 
                            // sees that at the second stop time, it can be reached from previous round reach, but then it cannot find a 
                            // trip that would depart from the stop after the last round reach, as this looks for the first departure and not the second one
                            // TODO: check if this is true, check if it should be changed
                            if (newTrip is not null)
                            {
                                currTrip = newTrip;
                                traverseFromStop = currStop;
                                currTripDate = newTripDate;

                                tripHasDelayData = delayModel.TripHasDelayData(currTripDate, newTrip.Id);
                                if (tripHasDelayData)
                                {
                                    tripStopDelays = delayModel.GetTripStopDelaysUnsafe(currTripDate, newTrip.Id);
                                }
                            }
                            
                        }
                    }
                }

                bool SearchLeaveTimeIsTransferableFromLastRoundReach(Stop stop, DateTime searchLeaveTime)
                {
                    return timeComp.ImprovesTime(searchModel.GetBestReachTimeInRound(stop, round - 1), searchLeaveTime);
                }
            }
        }

        /// <summary>
        /// For all marked bike stations, traverses all the possible bike trips from them and improves the arrival times for all stops where it is possible
        /// </summary>
        private void TraverseBikeRoutes()
        {
            if (searchModel is null)
            {
                throw new InvalidOperationException("Search model not initialized");
            }


            HashSet<BikeStation> newMarkedBikeStations = new();
            foreach (BikeStation markedBikeStation in markedBikeStations)
            {
                if (CannotImproveFromBikeStation(markedBikeStation))
                {
                    continue;
                }

                Dictionary<BikeStation, int> distances = bikeModel.GetDistancesFromStation(markedBikeStation);
                foreach (KeyValuePair<BikeStation, int> pair in distances)
                {
                    BikeStation reachingToBikeStation = pair.Key;
                    int distance = pair.Value;

                    if (CannotImproveToBikeStation(reachingToBikeStation, distance))
                    {
                        continue;
                    }

                    AssignBikeStationsByDirection(markedBikeStation, reachingToBikeStation, out BikeStation realSrcBikeStation, out BikeStation realDestBikeStation);
                    
                    DateTime reachTimeUsingBicycle = GetReachTimeUsingBicycle(markedBikeStation, distance);

                    bool improved = searchModel.TryImproveReachTimeByBikeTrip(realSrcBikeStation, realDestBikeStation, reachTimeUsingBicycle, round);
                    if (improved)
                    {
                        newMarkedBikeStations.Add(reachingToBikeStation);
                    }
                }
            }
            markedBikeStations = newMarkedBikeStations;

            bool CannotImproveFromBikeStation(BikeStation bikeStation)
            {
                // No bikes available (and search is forward)
                if (forward && bikeStation.BikeCount == 0)
                {
                    return true;
                }
                // The bike station was best reached by bike last time and thus we cannot continue improving by bike from it
                if (searchModel.RoutePointIsReachedByBikeInRound(bikeStation, round - 1))
                {
                    return true;
                }

                return false;
            }

            bool CannotImproveToBikeStation(BikeStation bikeStation, int distance)
            {
                // The bike station is not connected to the bike station we are improving from, or they are too far apart to be considered
                if (distance == -1)
                {
                    return true;
                }
                // The bike stations are close enough to be considered, but using the cyclingPace, the trip would take too long
                if (settings.BikeMax15Minutes && settings.GetBilledBikeTripTime(distance) > 15 * 60)
                {
                    return true;
                }
                // The bike station has no bikes available (and search is backward)
                if (!forward && bikeStation.BikeCount == 0)
                {
                    return true;
                }

                return false;
            }

            void AssignBikeStationsByDirection(BikeStation markedBikeStation, BikeStation reachingToBikeStation, out BikeStation realSrcBikeStation, out BikeStation realDestBikeStation)
            {
                if (forward)
                {
                    realSrcBikeStation = markedBikeStation;
                    realDestBikeStation = reachingToBikeStation;
                }
                else
                {
                    realSrcBikeStation = reachingToBikeStation;
                    realDestBikeStation = markedBikeStation;
                }
            }

            DateTime GetReachTimeUsingBicycle(BikeStation markedBikeStation, int distance)
            {
                int cyclingTimeSeconds = timeMpl * settings.GetAdjustedBikeTripTime(distance);
                DateTime improvingFromStopReachTime = searchModel.GetBestReachTimeInRound(markedBikeStation, round - 1);
                DateTime reachTime = improvingFromStopReachTime.AddSeconds(cyclingTimeSeconds);
                return reachTime;
            }
        }

        /// <summary>
        /// Takes all the stops that have been improved in current round and tries to improve all their neighbors by transfers
        /// </summary>
        /// <param name="useSharedBikes">Whether the search should consider shared bikes</param>
        /// <param name="onlyFromStops">Whether to only use transfers from stops</param>
        /// <param name="DoNotImproveToRoutePoint">To be used when the destination is a custom RoutePoint - in that case, its near stops (from which it can be accessed by foot) may NOT also be accessed by foot</param>
        private void ImproveByTransfers(bool useSharedBikes, bool onlyFromStops = false, Func<IRoutePoint, bool>? DoNotImproveToRoutePoint = null)
        {
            if (searchModel is null)
            {
                throw new InvalidOperationException("Search model not initialized");
            }


            HashSet<Stop> newMarkedStops = new();
            HashSet<BikeStation> newMarkedBikeStations = new();

            var orderedMarkedStops = forward ?
                markedStops.ToList().OrderBy(stop => searchModel.GetBestReachTimeInRound(stop, round)):
                markedStops.ToList().OrderByDescending(stop => searchModel.GetBestReachTimeInRound(stop, round));

            // Improve from all marked stops
            foreach (Stop markedStop in orderedMarkedStops)
            {
                TryImproveAllTransfersFromStop(markedStop);
            }

            // Improve from all marked bike stations (if permitted)
            if (useSharedBikes && !onlyFromStops)
            {
                foreach (BikeStation markedBikeStation in markedBikeStations)
                {
                    TryImproveAllTransfersFromBikeStation(markedBikeStation);
                }
            }

            // Add newly marked stops and bike stations to the marked sets
            markedStops.UnionWith(newMarkedStops);
            if (useSharedBikes)
            {
                markedBikeStations.UnionWith(newMarkedBikeStations);
            }



            void TryImproveAllTransfersFromStop(Stop stop)
            {
                
                foreach (Transfer transfer in stop.Transfers)
                {
                    if (transfer.From.Name == "Byšice" && transfer.To.Name == "Byšice")
                    {
                        Console.WriteLine();
                    }
                    Transfer realTransfer = forward ? transfer : transfer.OppositeTransfer!;

                    // Improve by Stop-to-Stop transfers
                    bool improved = searchModel.TryImproveReachTimeByTransfer(realTransfer, false, round, DoNotImproveToRoutePoint);
                    if (improved)
                    {
                        // NOT realTransfer here - we need the stop to which we are improving
                        newMarkedStops.Add(transfer.To);
                    }
                }
                if (useSharedBikes)
                {
                    foreach (BikeTransfer bikeTransfer in stop.BikeTransfers)
                    {
                        BikeTransfer realBikeTransfer = forward ? bikeTransfer : bikeTransfer.OppositeTransfer!;
                        // Improve by Stop-to-BikeStation transfers
                        bool improved = searchModel.TryImproveReachTimeByTransfer(realBikeTransfer, true, round, DoNotImproveToRoutePoint);
                        if (improved)
                        {
                            newMarkedBikeStations.Add((BikeStation)bikeTransfer.GetDestRoutePoint());
                        }
                    }
                }
            }

            void TryImproveAllTransfersFromBikeStation(BikeStation bikeStation)
            {
                foreach (BikeTransfer bikeTransfer in bikeStation.Transfers)
                {
                    BikeTransfer realBikeTransfer = forward ? bikeTransfer : bikeTransfer.OppositeTransfer!;
                    // Improve by BikeStation-to-Stop transfers
                    bool improved = searchModel.TryImproveReachTimeByTransfer(realBikeTransfer, false, round, DoNotImproveToRoutePoint);
                    if (improved)
                    {
                        newMarkedStops.Add((Stop)bikeTransfer.GetDestRoutePoint());
                    }
                }
            }
        }

        /// <summary>
        /// Runs the actual connection search algorithm
        /// </summary>
        /// <returns>Whether the search was correctly performed</returns>
        private bool RunRAPTOR(List<Stop> srcStops, List<BikeStation> srcBikeStations, List<Stop> destStops,
            List<BikeStation> destBikeStations, DateTime searchBeginTime, bool srcByCoord, bool destByCoord,
            Coordinates srcCoords = default, Coordinates destCoords = default)
        {
            // Sanity check
            if (StopListsAreIncorrect())
            {
                return false;
            }


            // Search variables setup
            List<Stop> searchBeginStops, searchEndStops;
            List<BikeStation> searchBeginBikeStations, searchEndBikeStations;


            // the parameters of this function are in real-world order, but the search may be in the opposite direction
            AssignStopsAndStationsByDirection();


            this.searchModel = new SearchModel(forward, searchBeginStops, searchEndStops, searchBeginBikeStations, searchEndBikeStations,
                searchBeginTime, settings, delayModel);


            HashSet<IRoutePoint> searchEndRoutePoints = new HashSet<IRoutePoint>();
            searchEndRoutePoints.UnionWith(searchEndStops);
            searchEndRoutePoints.UnionWith(searchEndBikeStations);



            // If source or destination are custom route points, set them up
            CustomRoutePoint realSrcRoutePoint = new CustomRoutePoint("srcId", "Source", srcCoords);
            CustomRoutePoint realDestRoutePoint = new CustomRoutePoint("destId", "Destination", destCoords);
            if (srcByCoord)
            {
                SetupCustomSrcRP();
            }
            if (destByCoord)
            {
                SetupCustomDestRP();
            }




            // Run connection search algorithm

            InitiateSearch();

            while (round < Settings.ROUNDS)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                if (settings.UseSharedBikes)
                {
                    TraverseBikeRoutes();
                }
                ImproveByTransfersComplete();
            }





            // Return the result
            markedStops.Clear();
            markedRoutesWithReachedTrips.Clear();
            return true;


            bool StopListsAreIncorrect()
            {
                if (settings.UseSharedBikes)
                {
                    if ((srcStops.Count == 0 && srcBikeStations.Count == 0) || (destStops.Count == 0 && destBikeStations.Count == 0))
                    {
                        return true;
                    }
                }
                else
                {
                    if (srcStops.Count == 0 || destStops.Count == 0)
                    {
                        return true;
                    }
                }

                return false;
            }

            void AssignStopsAndStationsByDirection()
            {
                if (forward)
                {
                    searchBeginStops = srcStops;
                    searchEndStops = destStops;
                    searchBeginBikeStations = srcBikeStations;
                    searchEndBikeStations = destBikeStations;
                }
                else
                {
                    searchBeginStops = destStops;
                    searchEndStops = srcStops;
                    searchBeginBikeStations = destBikeStations;
                    searchEndBikeStations = srcBikeStations;
                }
            }

            void SetupCustomSrcRP()
            {
                // set the starting custom route point
                foreach (Stop srcStop in srcStops)
                {
                    realSrcRoutePoint.AddTransferToRoutePoint(srcStop);
                }
                foreach (BikeStation srcStation in srcBikeStations)
                {
                    realSrcRoutePoint.AddTransferToRoutePoint(srcStation);
                }

                if (forward)
                {
                    this.searchModel.searchBeginCustomRoutePoint = realSrcRoutePoint;
                }
                else
                {
                    this.searchModel.searchEndCustomRoutePoint = realSrcRoutePoint;
                }
            }

            void SetupCustomDestRP()
            {
                // set the ending custom route point
                foreach (Stop destStop in destStops)
                {
                    realDestRoutePoint.AddTransferFromRoutePoint(destStop);
                }
                foreach (BikeStation destStation in destBikeStations)
                {
                    realDestRoutePoint.AddTransferFromRoutePoint(destStation);
                }

                if (forward)
                {
                    this.searchModel.searchEndCustomRoutePoint = realDestRoutePoint;
                }
                else
                {
                    this.searchModel.searchBeginCustomRoutePoint = realDestRoutePoint;
                }
            }

            void InitiateSearch()
            {
                if (forward && srcByCoord)
                {
                    InitiateSearchFromCustomRoutePoint(realSrcRoutePoint, settings.UseSharedBikes);
                }
                else if (!forward && destByCoord)
                {
                    InitiateSearchFromCustomRoutePoint(realDestRoutePoint, settings.UseSharedBikes);
                }
                else
                {
                    InitiateSearchFromStops(settings.UseSharedBikes);
                }
            }

            void ImproveByTransfersComplete()
            {
                if (SearchEndsByCustomRP())
                {
                    ImproveByTransfers(settings.UseSharedBikes, false, (x) => searchEndRoutePoints.Contains(x));
                }
                else
                {
                    ImproveByTransfers(settings.UseSharedBikes);
                }
            }

            bool SearchEndsByCustomRP()
            {
                return (forward && destByCoord) || (!forward && srcByCoord);
            }
        }

        private List<SearchResult>? FindConnection(
            List<Stop> srcStops, List<BikeStation> srcBikeStations, 
            List<Stop> destStops, List<BikeStation> destBikeStations,
            DateTime searchBeginTime, bool srcByCoord, bool destByCoord, 
            Coordinates srcCoords, Coordinates destCoords,
            bool allowViableAlternatives
        )
        {
            if (RunRAPTOR(srcStops, srcBikeStations, destStops, destBikeStations, searchBeginTime, srcByCoord,
                    destByCoord, srcCoords, destCoords))
            {
                if (allowViableAlternatives)
                {
                    return searchModel!.ExtractResultWithAlternatives(bikeModel);
                }
                else
                {
                    SearchResult? result = searchModel!.ExtractResult(bikeModel);
                    if (result is null)
                    {
                        return null;
                    }
                    else
                    {

                        return new List<SearchResult> { result };
                    }
                }
            }
            else
            {
                return null;
            }
        }



        /// <summary>
        /// Finds the best connection(s) between the 2 stops using their names
        /// </summary>
        /// <param name="srcStopName">The name of the source stop (exact)</param>
        /// <param name="destStopName">The name of the destination stop (exact)</param>
        /// <param name="searchBeginTime">The time at which the search starts (i.e. departure time if this is a forward search, arrival date otherwise</param>
        /// <param name="allowViableAlternatives">Whether to also include connections with different number of trips than the best one found, assuming they do not differ much in quality.</param>
        /// <returns>The list of best found connections (if allowViableAlternatives is false, only contains 0 or 1 item)</returns>
        public List<SearchResult>? FindConnection(string srcStopName, string destStopName, DateTime searchBeginTime, bool allowViableAlternatives)
        {
            if (srcStopName == destStopName)
            {
                return null;
            }
            List<Stop> srcStops = transitModel.GetStopsByName(srcStopName);
            List<Stop> destStops = transitModel.GetStopsByName(destStopName);
            List<BikeStation> srcBikeStations = new List<BikeStation>();
            List<BikeStation> destBikeStations = new List<BikeStation>();
            return FindConnection(srcStops, srcBikeStations, destStops, destBikeStations, searchBeginTime, false, false, default, default, allowViableAlternatives);
        }



        /// <summary>
        /// Finds the best connection(s) between the 2 coordinate points
        /// </summary>
        /// <param name="srcCoords">The source point coordinates</param>
        /// <param name="destCoords">The destination point coordinates</param>
        /// <param name="searchBeginTime">The time at which the search starts (i.e. departure time if this is a forward search, arrival date otherwise</param>
        /// <param name="allowViableAlternatives">Whether to also include connections with different number of trips than the best one found, assuming they do not differ much in quality.</param>
        /// <returns>The list of best found connections (if allowViableAlternatives is false, only contains 0 or 1 item)</returns>
        public List<SearchResult>? FindConnection(Coordinates srcCoords, Coordinates destCoords, DateTime searchBeginTime, bool allowViableAlternatives)
        {
            if (srcCoords == destCoords)
            {
                return null;
            }

            List<Stop> srcStops = transitModel.GetStopsByLocation(srcCoords, settings.GetMaxTransferDistance());
            List<Stop> destStops = transitModel.GetStopsByLocation(destCoords, settings.GetMaxTransferDistance());
            List<BikeStation> srcBikeStations = bikeModel.GetNearStations(srcCoords, settings.GetMaxTransferDistance());
            List<BikeStation> destBikeStations = bikeModel.GetNearStations(destCoords, settings.GetMaxTransferDistance());
            return FindConnection(srcStops, srcBikeStations, destStops, destBikeStations, searchBeginTime, true, true, srcCoords, destCoords, allowViableAlternatives);
        }

        /// <summary>
        /// Finds the best connection(s) from the source coordinates to the destination stop with the given name
        /// </summary>
        /// <param name="srcCoords">The source point coordinates</param>
        /// <param name="destStopName">The destination stop name (exact)</param>
        /// <param name="searchBeginTime">The time at which the search starts (i.e. departure time if this is a forward search, arrival date otherwise</param>
        /// <param name="allowViableAlternatives">Whether to also include connections with different number of trips than the best one found, assuming they do not differ much in quality.</param>
        /// <returns>The list of best found connections (if allowViableAlternatives is false, only contains 0 or 1 item)</returns>
        public List<SearchResult>? FindConnection(Coordinates srcCoords, string destStopName, DateTime searchBeginTime, bool allowViableAlternatives)
        {
            List<Stop> srcStops = transitModel.GetStopsByLocation(srcCoords, settings.GetMaxTransferDistance());
            List<Stop> destStops = transitModel.GetStopsByName(destStopName);
            List<BikeStation> srcBikeStations = bikeModel.GetNearStations(srcCoords, settings.GetMaxTransferDistance());
            List<BikeStation> destBikeStations = new List<BikeStation>();
            return FindConnection(srcStops, srcBikeStations, destStops, destBikeStations, searchBeginTime, true, false, srcCoords, default, allowViableAlternatives);
        }

        /// <summary>
        /// Finds the best connection(s) from the source stop with the given name to the destination coordinates
        /// </summary>
        /// <param name="srcStopName">The source stop name (exact)</param>
        /// <param name="destCoords">The destination point coordinates</param>
        /// <param name="searchBeginTime">The time at which the search starts (i.e. departure time if this is a forward search, arrival date otherwise</param>
        /// <param name="allowViableAlternatives">Whether to also include connections with different number of trips than the best one found, assuming they do not differ much in quality.</param>
        /// <returns>The list of best found connections (if allowViableAlternatives is false, only contains 0 or 1 item)</returns>
        public List<SearchResult>? FindConnection(string srcStopName, Coordinates destCoords, DateTime searchBeginTime, bool allowViableAlternatives)
        {
            List<Stop> srcStops = transitModel.GetStopsByName(srcStopName);
            List<Stop> destStops = transitModel.GetStopsByLocation(destCoords, settings.GetMaxTransferDistance());
            List<BikeStation> srcBikeStations = new List<BikeStation>();
            List<BikeStation> destBikeStations = bikeModel.GetNearStations(destCoords, settings.GetMaxTransferDistance());
            return FindConnection(srcStops, srcBikeStations, destStops, destBikeStations, searchBeginTime, false, true, default, destCoords, allowViableAlternatives);
        }


        /// <summary>
        /// Finds the best connections using the given request
        /// </summary>
        /// <param name="request">The connection request object</param>
        /// <returns>The complete connection request response object (including the error type if an error occurs)</returns>
        public ConnectionApiResponseResult FindConnection(ConnectionRequest request)
        {
            ConnectionApiResponseResult apiResponseResult = new();

            var error = request.Validate(transitModel, bikeModel);

            if (error != ConnectionSearchError.NoError)
            {
                apiResponseResult.Error = error;
                return apiResponseResult;
            }

            List<SearchResult>? resultJourneys;

            if (request.srcByCoords)
            {
                Coordinates srcCoords = new(request.srcLat, request.srcLon);
                if (request.destByCoords)
                {
                    Coordinates destCoords = new(request.destLat, request.destLon);
                    resultJourneys = FindConnection(srcCoords, destCoords, request.dateTime!.Value, false);
                }
                else
                {
                    resultJourneys = FindConnection(srcCoords, request.destStopName!, request.dateTime!.Value, false);
                }
            }
            else
            {
                if (request.destByCoords)
                {
                    Coordinates destCoords = new(request.destLat, request.destLon);
                    resultJourneys = FindConnection(request.srcStopName!, destCoords, request.dateTime!.Value, false);
                }
                else
                {
                    resultJourneys = FindConnection(request.srcStopName!, request.destStopName!, request.dateTime!.Value, false);
                }
            }

            if (resultJourneys is null)
            {
                apiResponseResult.Error = ConnectionSearchError.NoConnectionFound;
            }
            else
            {
                apiResponseResult.Error = ConnectionSearchError.NoError;
                apiResponseResult.Results = resultJourneys;
            }


            return apiResponseResult;
        }
    }
}

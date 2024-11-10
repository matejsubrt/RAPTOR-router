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

namespace RAPTOR_Router.RouteFinders
{
    class ReachedTrip
    {
        public Trip trip { get; }
        public Stop reachedOnStop { get; }
        public DateOnly tripDate { get; }
        public int stopIndex { get; }

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
    public class BasicRouteFinder : IRouteFinder
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
        private SearchModel searchModel;

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

        /// <summary>
        /// The current round of the RAPTOR algorithm
        /// </summary>
        private int round = 0;

        private TimeComparator timeComp;
        private IndexComparator indexComp;
        private bool forward;
        private int timeMpl;

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
        /// <param name="settings">The settings to be used for the connection search</param>
        /// <param name="transitModel">The transit model holding all the static information about the transit network</param>
        /// <param name="bikeModel">The bike model holding all the information about the shared bike systems and their stations</param>
        internal BasicRouteFinder(bool forward, Settings settings, TransitModel transitModel, BikeModel bikeModel, DelayModel delayModel)
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
            foreach (ITransfer transfer in customSearchBeginRP.possibleTransfers)
            {
                IRoutePoint imprFromPoint, imprToPoint;
                OrderTransferPointsBySearchDirection(transfer, out imprFromPoint, out imprToPoint);
                //IRoutePoint imprToPoint = transfer.GetDestRoutePoint();
                if (transfer.Distance > settings.GetMaxTransferDistance())
                {
                    continue;
                }
                if (imprToPoint is Stop)
                {
                    // TODO: check - shouldnt there be some transferLengthMultiplier?
                    int transferDuration = timeMpl * settings.GetAdjustedWalkingTransferTime(transfer.Distance);//transfer.GetTransferTime(settings.WalkingPace);
                    DateTime arrivalTime = searchModel.GetSearchBeginTime().AddSeconds(transferDuration);
                    searchModel.SetBestReachTime(imprToPoint, arrivalTime);
                    searchModel.SetTransferReachInRound(imprToPoint, transfer, arrivalTime, round);
                    markedStops.Add((Stop)imprToPoint);
                }
                else if (useSharedBikes && imprToPoint is BikeStation)
                {
                    int transferDuration = timeMpl * settings.GetAdjustedWalkingTransferTime(transfer.Distance);//transfer.GetTransferTime(settings.WalkingPace));// + settings.BikeUnlockTime);
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
            markedRoutesWithReachedTrips.Clear();

            foreach (Stop markedStop in markedStops)
            {
                foreach (Route route in markedStop.StopRoutes)
                {
                    int markedStopIndex = forward ? route.GetFirstStopIndex(markedStop) : route.GetLastStopIndex(markedStop);

                    if (markedRoutesWithReachedTrips.TryGetValue(route, out ReachedTrip existingReachedTrip))
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
                    /*if (forward)
                    {
                        int markedStopIndex = route.GetFirstStopIndex(markedStop);

                        if (markedRoutesWithReachedTrips.TryGetValue(route, out ReachedTrip existingReachedTrip))
                        {
                            int existingGetOnStopIndex = existingReachedTrip.stopIndex;

                            if (markedStopIndex < existingGetOnStopIndex)
                            {
                                TryFindTransferableTrip(route, markedStop, markedStopIndex, true);
                            }
                        }
                        else
                        {
                            TryFindTransferableTrip(route, markedStop, markedStopIndex, false);
                        }

                    }
                    else
                    {
                        int markedStopIndex = route.GetLastStopIndex(markedStop);


                        if (markedRoutesWithReachedTrips.TryGetValue(route, out ReachedTrip existingReachedTrip))
                        {
                            int existingGetOffStopIndex = existingReachedTrip.stopIndex;

                            if (markedStopIndex > existingGetOffStopIndex)
                            {
                                TryFindTransferableTrip(route, markedStop, markedStopIndex, true);
                            }
                        }
                        else
                        {
                            TryFindTransferableTrip(route, markedStop, markedStopIndex, false);
                        }
                    }*/
                    
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

                Trip trip = route.GetFirstTransferableTripAtStopByReachTimeBeta(
                    forward,
                    markedStop,
                    bestReachTimeAtTraverseFromStopLastRound,
                    delayModel,
                    out tripDate
                );
                //DatedTrip datedTrip = route.GetFirstTransferableTripAtStopByReachTimeBeta(forward, markedStop,
                //    bestReachTimeAtTraverseFromStopLastRound,
                //    searchModel.GetSearchBeginTime().AddDays(timeMpl * Settings.MAX_TRIP_LENGTH_DAYS), delayModel);

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

            /*void TraverseRouteBackward(Route route, Stop getOffStop, in Trip trip, DateOnly tripDate)
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

                        //if (DepartureTimeImprovesCurrBest(arrivalTime, currStop))
                        //{

                        //    ImproveDepartureByTrip(currStop, arrivalTime, currTrip, getOffStop);
                        //    markedStops.Add(currStop);
                        //}
                        bool improved = searchModel.TryImproveReachTimeByTrip(currStop, departureTime, currTrip, getOffStop, round);
                        if (improved)
                        {
                            markedStops.Add(currStop);
                        }

                        //TODO: shouldnt there be arrivalTime?
                        if (ArrivalIsEarlierThanLastRoundDeparture(currStop, arrivalTime))
                        {
                            //TODO: same as above
                            DateTime latestDepartureLastRound = searchModel.GetBestReachTimeInRound(currStop, round - 1);
                            // in first round, no buffer needed
                            //TODO: check the round condition when brain functions
                            if (searchModel.RoutePointIsReachedByTripInRound(currStop, round - 1))
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
                                searchModel.GetSearchBeginTime().AddDays(timeMpl * Settings.MAX_TRIP_LENGTH_DAYS), //Settings.MAX_TRIP_LENGTH_DAYS,
                                out tripDate);
                            if (newTrip != currTrip || searchModel.GetBestReachTime(currStop) > searchModel.GetBestReachTime(getOffStop))
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

                bool ArrivalIsEarlierThanLastRoundDeparture(Stop stop, DateTime arrivalTime)
                {
                    return searchModel.GetBestReachTimeInRound(stop, round - 1) > arrivalTime;
                }
                bool DepartureTimeImprovesCurrBest(DateTime departureTime, Stop stop)
                {
                    return departureTime > searchModel.GetLatestDeparture(stop)
                           && departureTime > searchModel.GetCurrentBestDepartureTime()
                           && departureTime >= searchModel.GetArrivalTime().AddDays(-Settings.MAX_TRIP_LENGTH_DAYS);
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

            void TraverseRouteForward(Route route, Stop getOnStop, in Trip trip, DateOnly tripDate)
            {
                Trip currTrip = trip;

                for (int i = route.GetFirstStopIndex(getOnStop); i < route.RouteStops.Count; i++)
                {
                    Stop currStop = route.RouteStops[i];



                    if (currTrip is not null)
                    {
                        StopTime stopTime = currTrip.StopTimes[i];

                        DateOnly realDate;
                        if (TripGoesOverMidnight(currTrip, route.GetFirstStopIndex(getOnStop), i))
                            realDate = tripDate.AddDays(1);
                        else
                            realDate = tripDate;


                        DateTime arrivalTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.ArrivalTime);
                        DateTime departureTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.DepartureTime);

                        bool improved = searchModel.TryImproveReachTimeByTrip(currStop, arrivalTime, currTrip, getOnStop, round);
                        if (improved)
                        {
                            markedStops.Add(currStop);
                        }

                        if (DepartureIsLaterThanLastRoundArrival(currStop, departureTime))
                        {
                            //TODO: same as above
                            DateTime earliestArrivalLastRound = searchModel.GetBestReachTimeInRound(currStop, round - 1);
                            // in first round, no buffer needed
                            if (round != 1)
                            {
                                if (searchModel.RoutePointIsReachedByTripInRound(currStop, round - 1))
                                {
                                    earliestArrivalLastRound = earliestArrivalLastRound.AddSeconds(settings.GetStationaryTransferMinimumSeconds());
                                }
                            }

                            if (earliestArrivalLastRound > departureTime)
                            {
                                continue;
                            }

                            DateOnly earliestArrivalLastRoundDate = DateOnly.FromDateTime(earliestArrivalLastRound);
                            TimeOnly earliestArrivalLastRoundTime = TimeOnly.FromDateTime(earliestArrivalLastRound);



                            Trip newTrip = route.GetEarliestTripDepartingAfterTimeAtStop(
                                currStop,
                                earliestArrivalLastRoundDate,
                                earliestArrivalLastRoundTime,
                                searchModel.GetSearchBeginTime().AddDays(timeMpl * Settings.MAX_TRIP_LENGTH_DAYS), //Settings.MAX_TRIP_LENGTH_DAYS,
                                out tripDate);
                            if (newTrip != currTrip || searchModel.GetBestReachTime(currStop) < searchModel.GetBestReachTime(getOnStop))
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

                bool DepartureIsLaterThanLastRoundArrival(Stop stop, DateTime departureTime)
                {
                    return searchModel.GetBestReachTimeInRound(stop, round - 1) < departureTime;
                }
                bool ArrivalTimeImprovesCurrBest(DateTime arrivalTime, Stop stop)
                {
                    return arrivalTime < searchModel.GetEarliestArrival(stop)
                            && arrivalTime < searchModel.GetCurrentBestArrivalTime()
                            && arrivalTime <= searchModel.GetDepartureTime().AddDays(Settings.MAX_TRIP_LENGTH_DAYS);
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
            */

            void TraverseRoute(Route route, Stop traverseFromStop, Trip trip, DateOnly tripDate)
            {
                DateOnly currTripDate = tripDate;
                Trip currTrip = trip;

                bool tripHasDelayData = delayModel.TripHasDelayData(currTripDate, currTrip.Id);
                TripStopDelays? tripStopDelays = null;
                if (tripHasDelayData)
                {
                    tripStopDelays = delayModel.GetTripStopDelays(currTripDate, currTrip.Id);
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
                        stopHasDelayData = tripStopDelays.TryGetStopDelay(i, out arrivalDelay, out departureDelay);
                    }


                    DateOnly realDate;
                    if (TripGoesOverMidnight(currTrip, firstStopIndexInDirOfSearch, i))
                        realDate = currTripDate.AddDays(daysToAddWhenOverMidnight);
                    else
                        realDate = currTripDate;


                    //DateTime arrivalTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.ArrivalTime);
                    //DateTime departureTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.DepartureTime);

                    //DateTime searchReachTime = forward ? arrivalTime : departureTime;
                    //DateTime searchLeaveTime = forward ? departureTime : arrivalTime;


                    DateTime regularArrivalTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.ArrivalTime);
                    DateTime regularDepartureTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.DepartureTime);

                    DateTime actualArrivalTime = stopHasDelayData ? regularArrivalTime.AddSeconds(arrivalDelay) : regularArrivalTime;
                    DateTime actualDepartureTime = stopHasDelayData ? regularDepartureTime.AddSeconds(departureDelay) : regularDepartureTime;

                    DateTime searchReachTime = forward ? actualArrivalTime : actualDepartureTime;
                    DateTime searchLeaveTime = forward ? actualDepartureTime : actualArrivalTime;

                    bool improved = searchModel.TryImproveReachTimeByTrip(currStop, searchReachTime, currTrip, traverseFromStop, round);
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
                            //continue;
                            return;
                        }

                        //DateOnly bestReachLastRoundDate = DateOnly.FromDateTime(bestReachTimeLastRound);
                        //TimeOnly bestReachLastRoundTime = TimeOnly.FromDateTime(bestReachTimeLastRound);


                        DateOnly newTripDate;
                        Trip newTrip = route.GetFirstTransferableTripAtStopByReachTimeBeta(
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
                                    tripStopDelays = delayModel.GetTripStopDelays(currTripDate, newTrip.Id);
                                }
                            }
                            
                        }
                    }
                }

                //TODO: check for delays
                bool TripGoesOverMidnight(Trip trip, int traverseFromStopIndex, int currStopIndex)
                {
                    return forward ?
                        trip.StopTimes[0].DepartureTime > trip.StopTimes[currStopIndex].ArrivalTime :
                        trip.StopTimes[^1].ArrivalTime < trip.StopTimes[currStopIndex].DepartureTime;
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
        /// <param name="DoNotImproveToRoutePoint">To be used when the destination is a custom RoutePoint - in that case, its near stops (from which it can be accessed by foot) may NOT also be accessed by foot</param>
        private void ImproveByTransfers(bool useSharedBikes, bool onlyFromStops = false, Func<IRoutePoint, bool> DoNotImproveToRoutePoint = null)
        {
            HashSet<Stop> newMarkedStops = new();
            HashSet<BikeStation> newMarkedBikeStations = new();

            // Improve from all marked stops
            foreach (Stop markedStop in markedStops)
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
                    Transfer realTransfer = forward ? transfer : transfer.OppositeTransfer;

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
                        BikeTransfer realBikeTransfer = forward ? bikeTransfer : bikeTransfer.OppositeTransfer;
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
                    BikeTransfer realBikeTransfer = forward ? bikeTransfer : bikeTransfer.OppositeTransfer;
                    // Improve by BikeStation-to-Stop transfers
                    bool improved = searchModel.TryImproveReachTimeByTransfer(realBikeTransfer, false, round, DoNotImproveToRoutePoint);
                    if (improved)
                    {
                        newMarkedStops.Add((Stop)bikeTransfer.GetDestRoutePoint());
                    }
                }
            }
        }


        public Tuple<List<Stop>, List<BikeStation>> GetNearRoutePoints(double lat, double lon)
        {
            List<Stop> nearStops = transitModel.GetStopsByLocation(lat, lon, settings.GetMaxTransferDistance());
            List<BikeStation> nearBikeStations = bikeModel.GetNearStations(lat, lon, settings.GetMaxTransferDistance());
            return new Tuple<List<Stop>, List<BikeStation>>(nearStops, nearBikeStations);
        }

        public Tuple<List<Stop>, List<BikeStation>> GetNearRoutePoints(string stopName)
        {
            List<Stop> stops = transitModel.GetStopsByName(stopName);
            List<BikeStation> bikeStations = new List<BikeStation>();
            return new Tuple<List<Stop>, List<BikeStation>>(stops, bikeStations);
        }

        private bool RunRAPTOR(List<Stop> srcStops, List<BikeStation> srcBikeStations, List<Stop> destStops,
            List<BikeStation> destBikeStations, DateTime searchBeginTime, bool srcByCoord, bool destByCoord,
            Coordinates srcCoords = default, Coordinates destCoords = default)
        {
            Stopwatch sw = Stopwatch.StartNew();
            // Sanity check

            if (StopListsAreIncorrect())
            {
                return false;
            }




            // Search variables setup

            List<Stop> searchBeginStops, searchEndStops;
            List<BikeStation> searchBeginBikeStations, searchEndBikeStations;


            AssignStopsAndStationsByDirection();


            this.searchModel = new SearchModel(forward, searchBeginStops, searchEndStops, searchBeginBikeStations, searchEndBikeStations,
                searchBeginTime, settings, delayModel);


            HashSet<IRoutePoint> searchEndRoutePoints = new HashSet<IRoutePoint>();
            searchEndRoutePoints.UnionWith(searchEndStops);
            searchEndRoutePoints.UnionWith(searchEndBikeStations);

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

            while (round <= Settings.ROUNDS - 1)
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

            sw.Stop();
            return true;
            //Console.WriteLine("Search took: " + sw.Elapsed);

            //return searchModel.ExtractResult(bikeModel);


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

        private SearchResult FindConnection(List<Stop> srcStops, List<BikeStation> srcBikeStations, List<Stop> destStops, List<BikeStation> destBikeStations, DateTime searchBeginTime, bool srcByCoord, bool destByCoord, Coordinates srcCoords = default, Coordinates destCoords = default)
        {
            if (RunRAPTOR(srcStops, srcBikeStations, destStops, destBikeStations, searchBeginTime, srcByCoord,
                    destByCoord, srcCoords, destCoords))
            {
                return searchModel.ExtractResult(bikeModel);
            }
            else
            {
                return null;
            }
        }

        private List<SearchResult>? FindConnectionWithAlternatives(List<Stop> srcStops,
            List<BikeStation> srcBikeStations, List<Stop> destStops, List<BikeStation> destBikeStations,
            DateTime searchBeginTime, bool srcByCoord, bool destByCoord)
        {
            if (RunRAPTOR(srcStops, srcBikeStations, destStops, destBikeStations, searchBeginTime, srcByCoord,
                    destByCoord))
            {
                return searchModel.ExtractResultWithAlternatives(bikeModel);
            }
            else
            {
                return null;
            }
        }

        public List<SearchResult>? FindConnectionWithAlternatives(string sourceStop, string destStop,
            DateTime departureTime)
        {
            if (sourceStop == destStop)
            {
                return null;
            }
            List<Stop> srcStops = transitModel.GetStopsByName(sourceStop);
            List<Stop> destStops = transitModel.GetStopsByName(destStop);
            List<BikeStation> srcBikeStations = new List<BikeStation>();
            List<BikeStation> destBikeStations = new List<BikeStation>();
            return FindConnectionWithAlternatives(srcStops, srcBikeStations, destStops, destBikeStations, departureTime, false, false);
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
            if (sourceStop == destStop)
            {
                return null;
            }
            List<Stop> srcStops = transitModel.GetStopsByName(sourceStop);
            List<Stop> destStops = transitModel.GetStopsByName(destStop);
            List<BikeStation> srcBikeStations = new List<BikeStation>();
            List<BikeStation> destBikeStations = new List<BikeStation>();
            return FindConnection(srcStops, srcBikeStations, destStops, destBikeStations, departureTime, false, false);
        }

        /// <summary>
        /// Finds the connection with the earliest arrival to the destination custom route point, that departs from the source custom route point after the specified time.
        /// </summary>
        /// <remarks>Used for searches by coordinates</remarks>
        /// <param name="srcLat">The latitude of the source point</param>
        /// <param name="srcLon">The longitude of the source point</param>
        /// <param name="destLat">The latitude of the destination point</param>
        /// <param name="destLon">The longitude of the destination point</param>
        /// <param name="departureTime">The arrival date and time</param>
        /// <returns>The result of the search, null if no conection could be found.</returns>
        public SearchResult FindConnection(double srcLat, double srcLon, double destLat, double destLon, DateTime departureTime)
        {
            var (srcStops, srcBikeStations) = GetNearRoutePoints(srcLat, srcLon);
            var (destStops, destBikeStations) = GetNearRoutePoints(destLat, destLon);
            return FindConnection(srcStops, srcBikeStations, destStops, destBikeStations, departureTime, true, true, new Coordinates(srcLat, srcLon), new Coordinates(destLat, destLon));

        }
    }
}

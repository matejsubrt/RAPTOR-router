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
    public class UniversalRouteFinder : IRouteFinder
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
        private UniversalSearchModel searchModel;


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

        private TimeComparator comp;
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
        internal UniversalRouteFinder(bool forward, Settings settings, TransitModel transitModel, BikeModel bikeModel)
        {
            this.settings = settings;
            this.transitModel = transitModel;
            this.bikeModel = bikeModel;
            this.forward = forward;
            this.comp = new TimeComparator(forward);
            this.timeMpl = forward ? 1 : -1;
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
                    int transferDuration = timeMpl * transfer.GetTransferTime(settings.WalkingPace);
                    DateTime arrivalTime = searchModel.GetSearchBeginTime().AddSeconds(transferDuration);
                    searchModel.SetBestReachTime(imprToPoint, arrivalTime);
                    searchModel.SetTransferReachInRound(imprToPoint, transfer, arrivalTime, round);
                    markedStops.Add((Stop)imprToPoint);
                }
                else if (useSharedBikes && imprToPoint is BikeStation)
                {
                    int transferDuration = timeMpl * (transfer.GetTransferTime(settings.WalkingPace) + settings.BikeUnlockTime);
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
            //markedRoutesWithReachedStops.Clear();
            markedRoutesWithReachedTrips.Clear();
            foreach (Stop markedStop in markedStops)
            {
                foreach (Route route in markedStop.StopRoutes)
                {
                    if (forward)
                    {
                        int markedStopIndex = route.GetFirstStopIndex(markedStop);

                        //if (markedRoutesWithReachedStops.ContainsKey(route))
                        //{
                        //    if (route.GetFirstStopIndex(markedRoutesWithReachedStops[route]) > route.GetFirstStopIndex(markedStop))
                        //    {
                        //        markedRoutesWithReachedStops[route] = markedStop;
                        //    }
                        //}
                        //else
                        //{
                        //    markedRoutesWithReachedStops.Add(route, markedStop);
                        //}


                        

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


                        //if (currStopIndex != 0) // if the route is already marked and the marked stop is not the last one
                        //{
                        //    if (markedRoutesWithReachedStops.ContainsKey(route))
                        //    {
                        //        int prevGetOffStopIndex = route.GetLastStopIndex(markedRoutesWithReachedStops[route]);

                        //        if (prevGetOffStopIndex < currStopIndex)
                        //        {
                        //            markedRoutesWithReachedStops[route] = markedStop;
                        //        }
                        //    }
                        //    else
                        //    {
                        //        markedRoutesWithReachedStops.Add(route, markedStop);
                        //    }
                        //}


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

                Trip trip = route.GetFirstTransferableTripAtStopByReachTime(
                    forward,
                    markedStop,
                    DateOnly.FromDateTime(bestReachTimeAtTraverseFromStopLastRound),
                    TimeOnly.FromDateTime(bestReachTimeAtTraverseFromStopLastRound),
                    Settings.MAX_TRIP_LENGTH_DAYS,
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
            //foreach (KeyValuePair<Route, Stop> pair in markedRoutesWithReachedStops)
            //{
            foreach(KeyValuePair<Route, ReachedTrip> pair in markedRoutesWithReachedTrips)
            {
                Route route = pair.Key;
                ReachedTrip reachedTrip = pair.Value;

                Stop traverseFromStop = reachedTrip.reachedOnStop;
                Trip trip = reachedTrip.trip;
                DateOnly tripDate = reachedTrip.tripDate;

                //Stop traverseFromStop = pair.Value;
                //DateOnly tripDate;

                ////TODO: shouldnt this be just trip arrival?
                //DateTime bestReachTimeAtTraverseFromStopLastRound = searchModel.GetBestReachTimeInRound(traverseFromStop, round - 1);
                //// in round 1, the arrival time is the start time -> no need for buffer
                //if (round > 1 && searchModel.RoutePointIsReachedByTripInRound(traverseFromStop, round - 1))
                //{
                //    int stationaryTransferTime = timeMpl * settings.GetStationaryTransferMinimumSeconds();
                //    bestReachTimeAtTraverseFromStopLastRound = bestReachTimeAtTraverseFromStopLastRound.AddSeconds(stationaryTransferTime);
                //}
                //if (route.ShortName == "B")
                //{
                //    Console.WriteLine();
                //}
                //if (route.LongName == "Letňany - Ládví - Háje")
                //{
                //    Console.WriteLine();
                //}
                //Trip trip = route.GetFirstTransferableTripAtStopByReachTime(
                //    forward,
                //    traverseFromStop,
                //    DateOnly.FromDateTime(bestReachTimeAtTraverseFromStopLastRound),
                //    TimeOnly.FromDateTime(bestReachTimeAtTraverseFromStopLastRound),
                //    Settings.MAX_TRIP_LENGTH_DAYS,
                //    out tripDate
                //);

                if (forward)
                {
                    TraverseRouteForward(route, traverseFromStop, trip, tripDate);
                }
                else
                {
                    TraverseRouteBackward(route, traverseFromStop, trip, tripDate);
                }

                //TraverseRoute(route, traverseFromStop, trip, tripDate);
            }

            void TraverseRouteBackward(Route route, Stop getOffStop, in Trip trip, DateOnly tripDate)
            {
                Trip currTrip = trip;

                for (int i = route.GetLastStopIndex(getOffStop); i >= 0; i--)
                {
                    Stop currStop = route.RouteStops[i];

                    

                    if (currTrip is not null)
                    {
                        StopTime stopTime = currTrip.StopTimes[i];

                        if (currStop.Name == "Muzeum" && stopTime.DepartureTime.Hour == 6 && stopTime.DepartureTime.Minute == 38)
                        {
                            Console.WriteLine();
                        }
                            
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
                /*bool DepartureTimeImprovesCurrBest(DateTime departureTime, Stop stop)
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
                }*/
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
                                Settings.MAX_TRIP_LENGTH_DAYS,
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
                /*bool ArrivalTimeImprovesCurrBest(DateTime arrivalTime, Stop stop)
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
                }*/
            }

            void TraverseRoute(Route route, Stop traverseFromStop, in Trip trip, DateOnly tripDate)
            {
                if (route.ShortName == "A")
                {
                    Console.WriteLine();
                }

                Trip currTrip = trip;

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



                    if (currTrip is not null)
                    {
                        int daysToAddWhenOverMidnight = forward ? 1 : -1;
                        int firstStopIndexInDirOfSearch = forward ? route.GetFirstStopIndex(traverseFromStop) : route.GetLastStopIndex(traverseFromStop);

                        StopTime stopTime = currTrip.StopTimes[i];


                        DateOnly realDate;
                        if (TripGoesOverMidnight(currTrip, firstStopIndexInDirOfSearch, i))
                            realDate = tripDate.AddDays(daysToAddWhenOverMidnight);
                        else
                            realDate = tripDate;


                        DateTime arrivalTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.ArrivalTime);
                        DateTime departureTime = DateTimeExtensions.FromDateAndTime(realDate, stopTime.DepartureTime);

                        DateTime searchReachTime = forward ? arrivalTime : departureTime;
                        DateTime searchLeaveTime = forward ? departureTime : arrivalTime;

                        bool improved = searchModel.TryImproveReachTimeByTrip(currStop, searchReachTime, currTrip, traverseFromStop, round);
                        if (improved)
                        {
                            markedStops.Add(currStop);
                        }

                        if (SearchLeaveTimeIsTransferableFromLastRoundArrival(currStop, departureTime))
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

                            if (!comp.ImprovesTime(bestReachTimeLastRound, searchLeaveTime))//bestReachTimeLastRound > departureTime)
                            {
                                //continue;
                                return;
                            }

                            DateOnly bestReachLastRoundDate = DateOnly.FromDateTime(bestReachTimeLastRound);
                            TimeOnly bestReachLastRoundTime = TimeOnly.FromDateTime(bestReachTimeLastRound);



                            Trip newTrip = route.GetFirstTransferableTripAtStopByReachTime(
                                forward,
                                currStop,
                                bestReachLastRoundDate,
                                bestReachLastRoundTime,
                                Settings.MAX_TRIP_LENGTH_DAYS,
                                out tripDate);
                            if (newTrip != currTrip || comp.ImprovesTime(searchModel.GetBestReachTime(currStop), searchModel.GetBestReachTime(traverseFromStop)))
                            {
                                currTrip = newTrip;
                                traverseFromStop = currStop;
                            }
                        }
                    }
                }

                bool TripGoesOverMidnight(Trip trip, int traverseFromStopIndex, int currStopIndex)
                {
                    return forward ?
                        trip.StopTimes[traverseFromStopIndex].DepartureTime > trip.StopTimes[currStopIndex].ArrivalTime :
                        trip.StopTimes[traverseFromStopIndex].ArrivalTime < trip.StopTimes[currStopIndex].DepartureTime;
                }

                bool DepartureIsLaterThanLastRoundArrival(Stop stop, DateTime departureTime)
                {
                    return searchModel.GetBestReachTimeInRound(stop, round - 1) < departureTime;
                }

                bool SearchLeaveTimeIsTransferableFromLastRoundArrival(Stop stop, DateTime searchLeaveTime)
                {
                    return comp.ImprovesTime(searchModel.GetBestReachTimeInRound(stop, round - 1), searchLeaveTime);
                    //return searchModel.GetBestReachTimeInRound(stop, round - 1) < searchLeaveTime;
                }
                /*bool ArrivalTimeImprovesCurrBest(DateTime arrivalTime, Stop stop)
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
                }*/
            }
        }

        /// <summary>
        /// For all marked bike stations, traverses all the possible bike trips from them and improves the arrival times for all stops where it is possible
        /// </summary>
        void TraverseBikeRoutes()
        {
            HashSet<BikeStation> newMarkedBikeStations = new();
            foreach (BikeStation markedBikeStation in markedBikeStations)
            {
                if (forward && markedBikeStation.BikeCount == 0)
                {
                    continue;
                }
                if (searchModel.RoutePointIsReachedByBikeInRound(markedBikeStation, round - 1))
                {
                    continue;
                }
                Dictionary<BikeStation, int> distances = bikeModel.GetDistancesFromStation(markedBikeStation);
                foreach (KeyValuePair<BikeStation, int> pair in distances)
                {
                    BikeStation reachingToBikeStation = pair.Key;
                    int distance = pair.Value;



                    if (distance == -1)
                    {
                        continue;
                    }
                    if (settings.BikeMax15Minutes && GetCyclingTime(distance) > 15 * 60)
                    {
                        continue;
                    }
                    if (!forward && reachingToBikeStation.BikeCount == 0)
                    {
                        continue;
                    }

                    BikeStation realSrcBikeStation, realDestBikeStation;
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


                    int cyclingTimeSeconds = GetCyclingTime(distance);
                    int fullCyclingTimeSeconds = timeMpl * (cyclingTimeSeconds + settings.BikeUnlockTime + settings.BikeLockTime);
                    DateTime improvingFromStopReachTime = searchModel.GetBestReachTimeInRound(markedBikeStation, round - 1);
                    DateTime reachTimeUsingBicycle = improvingFromStopReachTime.AddSeconds(fullCyclingTimeSeconds);

                    bool improved = searchModel.TryImproveReachTimeByBikeTrip(realSrcBikeStation, realDestBikeStation, reachTimeUsingBicycle, round);
                    if (improved)
                    {
                        newMarkedBikeStations.Add(reachingToBikeStation);
                    }
                }
            }
            markedBikeStations = newMarkedBikeStations;
            int GetCyclingTime(int distance)
            {
                return (int)((distance / 1000.0 * settings.CyclingPace * 60) * settings.GetBikeTripLengthMultiplier());
            }
            /*bool ArrivalTimeImprovesCurrBest(DateTime arrivalTime, BikeStation bikeStation)
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
                if (searchModel.destinationBikeStations.Contains(toBikeStation) && arrivalTime < searchModel.GetCurrentBestArrivalTime())
                {
                    searchModel.SetCurrentBestArrivalTime(arrivalTime);
                }
            }*/
        }

        /// <summary>
        /// Takes all the stops that have been improved in current round and tries to improve all their neighbors by transfers
        /// </summary>
        /// <param name="DoNotImproveToRoutePoint">To be used when the destination is a custom RoutePoint - in that case, its near stops (from which it can be accessed by foot) may NOT also be accessed by foot</param>
        private void ImproveByTransfers(bool useSharedBikes, bool onlyFromStops = false, Func<IRoutePoint, bool> DoNotImproveToRoutePoint = null)
        {
            HashSet<Stop> newMarkedStops = new();
            HashSet<BikeStation> newMarkedBikeStations = new();
            foreach (Stop markedStop in markedStops)
            {
                foreach (Transfer transfer in markedStop.Transfers)
                {
                    Transfer realTransfer = forward ? transfer : transfer.OppositeTransfer;

                    if (realTransfer.To.Name == "Muzeum" && realTransfer.From.Name == "Muzeum")
                    {
                        Console.WriteLine();
                    }
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
                    foreach (BikeTransfer bikeTransfer in markedStop.BikeTransfers)
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
            if (useSharedBikes && !onlyFromStops)
            {
                foreach (BikeStation markedBikeStation in markedBikeStations)
                {
                    foreach (BikeTransfer bikeTransfer in markedBikeStation.Transfers)
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

            markedStops.UnionWith(newMarkedStops);
            if (useSharedBikes)
            {
                markedBikeStations.UnionWith(newMarkedBikeStations);
            }
            /*bool TransferImprovesArrivalTime(ITransfer transfer)
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
            }*/
        }

        /// <summary>
        /// Finds the connection with the earliest arrival to one of the destinationStations in the searchModel
        /// </summary>
        /// <param name="searchModel">The search model to be used for finding the connection</param>
        /// <returns>The quickest connection from one of the sourceStops to one of the destinationStops in the searchModel</returns>
        /*internal SearchResult FindConnection(ForwardSearchModel searchModel)
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
            markedRoutesWithGetOnStops.Clear();
            markedBikeStations.Clear();
            return searchModel.ExtractResult(bikeModel);
        }*/

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



        private SearchResult FindConnection(List<Stop> srcStops, List<BikeStation> srcBikeStations, List<Stop> destStops, List<BikeStation> destBikeStations, DateTime searchBeginTime, bool srcByCoord, bool destByCoord, Coordinates srcCoords = default, Coordinates destCoords = default)
        {
            if (settings.UseSharedBikes)
            {
                if ((srcStops.Count == 0 && srcBikeStations.Count == 0) || (destStops.Count == 0 && destBikeStations.Count == 0))
                {
                    return null;
                }
            }
            else
            {
                if (srcStops.Count == 0 || destStops.Count == 0)
                {
                    return null;
                }
            }


            List<Stop> searchBeginStops, searchEndStops;
            List<BikeStation> searchBeginBikeStations, searchEndBikeStations;

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


            //this.searchModel = new ForwardSearchModel(srcStops, destStops, srcBikeStations, destBikeStations, departureTime, settings);
            this.searchModel = new UniversalSearchModel(forward, searchBeginStops, searchEndStops, searchBeginBikeStations, searchEndBikeStations,
                searchBeginTime, settings);

            //HashSet<IRoutePoint> destRoutePoints = new HashSet<IRoutePoint>();
            //destRoutePoints.UnionWith(destStops);
            //destRoutePoints.UnionWith(destBikeStations);

            HashSet<IRoutePoint> searchEndRoutePoints = new HashSet<IRoutePoint>();
            searchEndRoutePoints.UnionWith(searchEndStops);
            searchEndRoutePoints.UnionWith(searchEndBikeStations);

            CustomRoutePoint realSrcRoutePoint = new CustomRoutePoint("srcId", "Source", srcCoords);
            CustomRoutePoint realDestRoutePoint = new CustomRoutePoint("destId", "Destination", destCoords);


            if (srcByCoord)
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


            if (destByCoord)
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

            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                if (settings.UseSharedBikes)
                {
                    TraverseBikeRoutes();
                }

                if (forward && destByCoord)
                {
                    ImproveByTransfers(settings.UseSharedBikes, false, (x) => searchEndRoutePoints.Contains(x));
                }
                else if (!forward && srcByCoord)
                {
                    ImproveByTransfers(settings.UseSharedBikes, false, (x) => searchEndRoutePoints.Contains(x));
                }
                else
                {
                    ImproveByTransfers(settings.UseSharedBikes);
                }
                //if (destByCoord)
                //{
                //    ImproveByTransfers(settings.UseSharedBikes, false, (x) => destRoutePoints.Contains(x));
                //}
                //else
                //{
                //    ImproveByTransfers(settings.UseSharedBikes);
                //}
            }


            markedStops.Clear();
            //markedRoutesWithReachedStops.Clear();
            markedRoutesWithReachedTrips.Clear();
            return searchModel.ExtractResult(bikeModel);
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
            /*//bikeModel.UpdateStationStatus();
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


            this.searchModel = new ForwardSearchModel(sourceStops, destStops, sourceBikeStations, destBikeStations, departureTime, settings);


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
            markedRoutesWithGetOnStops.Clear();
            return searchModel.ExtractResult(bikeModel);*/

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
            /*CustomRoutePoint source = new CustomRoutePoint("srcId", "Source", new Coordinates(srcLat, srcLon));
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


            HashSet<IRoutePoint> destRoutePoints = new HashSet<IRoutePoint>();
            destRoutePoints.UnionWith(destStops);
            destRoutePoints.UnionWith(destBikeStations);




            this.searchModel = new ForwardSearchModel(sourceStops, destStops, sourceBikeStations, destBikeStations, departureTime, settings);
            this.searchModel.sourceCustomRoutePoint = source;
            this.searchModel.destinationCustomRoutePoint = dest;

            InitiateSearchFromCustomRoutePoint(source, settings.UseSharedBikes);
            while (round <= Settings.ROUNDS - 1)
            {
                round++;
                AccumulateRoutes();
                TraverseMarkedRoutes();
                TraverseBikeRoutes();
                ImproveByTransfers(settings.UseSharedBikes, false, (x) => destRoutePoints.Contains(x));
            }
            markedStops.Clear();
            markedRoutesWithGetOnStops.Clear();
            return searchModel.ExtractResult(bikeModel);*/

            var (srcStops, srcBikeStations) = GetNearRoutePoints(srcLat, srcLon);
            var (destStops, destBikeStations) = GetNearRoutePoints(destLat, destLon);
            return FindConnection(srcStops, srcBikeStations, destStops, destBikeStations, departureTime, true, true, new Coordinates(srcLat, srcLon), new Coordinates(destLat, destLon));

        }
    }
}

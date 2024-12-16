using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Interfaces;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Custom;
using Itinero;
using RAPTOR_Router.RouteFinders;
using RAPTOR_Router.Structures.Requests;

namespace RAPTOR_Router.Models.Dynamic
{

    /// <summary>
    /// A class holding all the dynamic data of a single connection search. The BasicRouteFinder uses this class to store and access the data of a single search.
    /// </summary>
    /// <remarks>Can be used for both search directions.</remarks>
    internal class SearchModel
    {
        /// <summary>
        /// The time at which the search begins - earliest departure time for forward search, latest arrival time for backward search
        /// </summary>
        private readonly DateTime searchBeginTime;
        /// <summary>
        /// The worst possible bound for the search - the latest possible arrival time for a forward search and the earliest possible departure time for a backward search
        /// </summary>
        private readonly DateTime worstBound;
        /// <summary>
        /// Whether the search is done in the forward direction (by earliest departure time) or in the backward direction (by latest arrival time)
        /// </summary>
        private readonly bool forward;

        

        /// <summary>
        /// List of all the public transit stops considered as the source
        /// </summary>
        public List<Stop> searchBeginStops { get; }
        /// <summary>
        /// List of all the public transit stops considered as the destination
        /// </summary>
        public List<Stop> searchEndStops { get; }
        /// <summary>
        /// List of all the bike stations considered as the source
        /// </summary>
        public List<BikeStation> searchBeginBikeStations { get; }
        /// <summary>
        /// List of all the bike stations considered as the destination
        /// </summary>
        public List<BikeStation> searchEndBikeStations { get; }
        /// <summary>
        /// The custom route point from which the search is started (used for searching from coordinates instead of from stop names)
        /// </summary>
        public CustomRoutePoint? searchBeginCustomRoutePoint { get; set; }
        /// <summary>
        /// The custom route point to which the search is done (used for searching to coordinates instead of to stop names)
        /// </summary>
        public CustomRoutePoint? searchEndCustomRoutePoint { get; set; }



        /// <summary>
        /// The settings being used for the search
        /// </summary>
        private Settings settingsUsed;
        

        /// <summary>
        /// The delay model holding current delay information about the trips
        /// </summary>
        private readonly DelayModel delayModel;


        /// <summary>
        /// The time comparator used for comparing two times
        /// </summary>
        private readonly TimeComparator comp;

        /// <summary>
        /// The time multiplier for the search direction -> 1 for forward search, -1 for backward search
        /// </summary>
        private readonly int timeMpl;


        /// <summary>
        /// Dictionary indexed by the RoutePoints, holding the current routing information about each RoutePoint
        /// </summary>
        private readonly Dictionary<IRoutePoint, StopRoutingInfo> routingInfo = new();
        
        
        /// <summary>
        /// The current best found arrival time (at the destination for forward search, at the source for backward search)
        /// </summary>
        private DateTime bestCurrentSearchEndTime;



        /// <summary>
        /// Creates a new SearchModel object
        /// </summary>
        /// <param name="forward">Whether the search is run in the forward direction</param>
        /// <param name="searchBeginStops">The list of stops considered as the source stops - typically stops from one node sharing the same name</param>
        /// <param name="searchEndStops">The list of stops considered as the destination stops - typically stops from one node sharing the same name</param>
        /// <param name="searchBeginBikeStations">The list of bikeStations considered as the source stations</param>
        /// <param name="searchEndBikeStations">The list of bikeStations considered as the destination stations</param>
        /// <param name="searchBeginTime">The start time of the search (earliest departure for forward searches, latest arrival for backward searches)</param>
        /// <param name="settingsUsed">The settings used for the search</param>
        /// <param name="delayModel"></param>
        public SearchModel(bool forward, List<Stop> searchBeginStops, List<Stop> searchEndStops, List<BikeStation> searchBeginBikeStations, List<BikeStation> searchEndBikeStations, DateTime searchBeginTime, Settings settingsUsed, DelayModel delayModel)
        {
            this.searchBeginStops = searchBeginStops;
            this.searchEndStops = searchEndStops;
            this.searchBeginBikeStations = searchBeginBikeStations;
            this.searchEndBikeStations = searchEndBikeStations;
            this.settingsUsed = settingsUsed;
            this.searchBeginTime = searchBeginTime;

            this.forward = forward;
            this.comp = new TimeComparator(forward);
            this.worstBound = forward ? DateTime.MaxValue : DateTime.MinValue;
            this.timeMpl = forward ? 1 : -1;
            this.bestCurrentSearchEndTime = worstBound;

            this.delayModel = delayModel;
        }

        /// <summary>
        /// For a given stop and round, creates the best possible SearchResult that reaches the stop in the round (i.e. with exactly so many trips)
        /// </summary>
        /// <param name="stop">The stop</param>
        /// <param name="round">The round</param>
        /// <param name="bikeModel">The bike model of the search</param>
        /// <returns>The best search result that reaches the stop in round</returns>
        /// <exception cref="ApplicationException"></exception>
        private SearchResult? CreateResultFromStopInRound(Stop? stop, int round, BikeModel bikeModel)
        {
            if (stop is null)
            {
                return null;
            }
            SearchResult result = new(settingsUsed);
            StopRoutingInfo currStopInfo = routingInfo[stop];

            if (searchEndCustomRoutePoint is not null)
            {
                CustomTransfer transfer = searchEndCustomRoutePoint.GetTransferWithNormalRP(stop);
                //int transferTime = timeMpl * settingsUsed.GetAdjustedWalkingTransferTime(transfer.Distance);
                //DateTime arrivalTimeAtDestCustomRP = currStopInfo.Reaches[round]!.Time.AddSeconds(transferTime);
                result.AddUsedTransfer(transfer, !forward);
            }

            IRoutePoint nextRoundStartStop = stop;
            int currRound = round;
            while (currRound > 0)
            {
                IRoutePoint currStop;

                var reach = currStopInfo.Reaches[currRound];
                if (reach is StopRoutingInfo.TransferReach transferReach)
                {
                    result.AddUsedTransfer(transferReach.Transfer, !forward);
                    currStop = forward ? transferReach.Transfer.From : transferReach.Transfer.To;
                }
                else if (reach is StopRoutingInfo.BikeTransferReach bikeTransferReach)
                {
                    result.AddUsedTransfer(bikeTransferReach.Transfer, !forward);
                    currStop = forward ? bikeTransferReach.Transfer.GetSrcRoutePoint() : bikeTransferReach.Transfer.GetDestRoutePoint();
                }


                // In current round, no transfer has been used, i.e. we are continuing from the exact same stop -> we add a new 0 length transfer
                else
                {
                    currStop = nextRoundStartStop;
                    if (currStop is Stop s)
                    {
                        // in last round, we do not add a transfer
                        if (currRound != round)
                        {
                            result.AddUsedTransfer(new Transfer(s, s, 0), !forward);
                        }
                    }
                }

                currStopInfo = routingInfo[currStop];
                reach = currStopInfo.Reaches[currRound];

                if (reach is StopRoutingInfo.TripReach tripReach)
                {
                    Stop realGetOnStop, realGetOffStop;

                    if (forward)
                    {
                        realGetOnStop = tripReach.ReachedFromStop;
                        realGetOffStop = (Stop)currStop;
                    }
                    else
                    {
                        realGetOnStop = (Stop)currStop;
                        realGetOffStop = tripReach.ReachedFromStop;
                    }

                    Trip tripToReachStop = tripReach.Trip;
                    if (tripToReachStop is null || tripReach.ReachedFromStop is null)
                    {
                        throw new ApplicationException("Trip and getOnStop cannot be null in an used round");
                    }

                    DateOnly tripStartDate = tripReach.TripStartDate;

                    bool tripHasDelayData = delayModel.TripHasDelayData(tripStartDate, tripToReachStop.Id);
                    bool getOnStopHasDelayData;
                    int getOnStopDepartureDelay, currentTripDelay;
                    if (tripHasDelayData)
                    {
                        getOnStopHasDelayData = delayModel.TryGetDelay(tripStartDate, tripToReachStop.Id, tripToReachStop.Route.GetFirstStopIndex(realGetOnStop), out int arrivalDelay, out int departureDelay);
                        if (!getOnStopHasDelayData)
                        {
                            getOnStopDepartureDelay = 0;
                            currentTripDelay = 0;
                        }
                        else
                        {
                            getOnStopDepartureDelay = departureDelay;
                            currentTripDelay = GetCurrentTripDelay(tripToReachStop, tripStartDate);
                        }
                    }
                    else
                    {
                        getOnStopHasDelayData = false;
                        getOnStopDepartureDelay = 0;
                        currentTripDelay = 0;
                    }

                    result.AddUsedTrip(tripToReachStop, tripStartDate, realGetOnStop, realGetOffStop, getOnStopHasDelayData, getOnStopDepartureDelay, currentTripDelay, !forward);

                    currStop = tripReach.ReachedFromStop;
                }
                else if (reach is StopRoutingInfo.BikeTripReach bikeTripReach)
                {
                    //TODO: Check use of bike model - shouldnt it be somewhere else?

                    BikeStation realSrcBikeStation, realDestBikeStation;

                    if (forward)
                    {
                        realSrcBikeStation = bikeTripReach.From;
                        realDestBikeStation = bikeTripReach.To;
                    }
                    else
                    {
                        realDestBikeStation = bikeTripReach.From;
                        realSrcBikeStation = bikeTripReach.To;
                    }


                    result.AddUsedBikeTrip(realSrcBikeStation, realDestBikeStation, bikeModel.GetDistanceBetweenStations(bikeTripReach.From, bikeTripReach.To), !forward);
                    currStop = bikeTripReach.From;
                }



                currStopInfo = routingInfo[currStop];
                nextRoundStartStop = currStop;
                currRound--;
            }

            //TODO: check
            // Add the first transfer to the result -> that would be in round 0 and thus not added in the loop above
            var firstReach = currStopInfo.Reaches[0];
            if (firstReach is StopRoutingInfo.TransferReach transferReach1)
            {
                result.AddUsedTransfer(transferReach1.Transfer, !forward);
            }
            else if (firstReach is StopRoutingInfo.BikeTransferReach bikeTransferReach)
            {
                result.AddUsedTransfer(bikeTransferReach.Transfer, !forward);
            }
            else if (firstReach is StopRoutingInfo.CustomTransferReach customTransferReach)
            {
                result.AddUsedTransfer(customTransferReach.Transfer, !forward);
            }

            if(forward)
            {
                result.SetDepartureAndArrivalTimesByEarliestDeparture(searchBeginTime);
            }
            else
            {
                result.SetDepartureAndArrivalTimesByLatestArrival(searchBeginTime);
            }


            //if (forward && result.UsedTrips.Count >= 2)
            //{
            //    var lastTrip = result.UsedTrips[^1];
            //    DateTime followingTripDepartureTime = lastTrip.stopPasses[lastTrip.getOnStopIndex].DepartureTime
            //        .AddSeconds(lastTrip.delayWhenBoarded);
            //    var altRouteFinder = RouteFinderBuilder.CreateDirectRouteFinder();

            //    var lastTripSegmentIndex = result.UsedSegmentTypes.FindLastIndex(type => type == SearchResult.SegmentType.Trip);
            //    var secondToLastTripSegmentIndex = result.UsedSegmentTypes.FindLastIndex(lastTripSegmentIndex - 1, type => type == SearchResult.SegmentType.Trip);

            //    //int currIndex = secondToLastTripSegmentIndex;
            //    int currIndex = result.UsedSegmentTypes.Count - 1;
            //    int currTripIndex = result.UsedTrips.Count - 1;
            //    int currTransferIndex = result.UsedTransfers.Count - 1;
            //    int currBikeTripIndex = result.UsedBikeTrips.Count - 1;
            //    int totalTimeBeforeNextTrip = 0;
            //    bool last = true;
            //    while (currIndex >= 0)
            //    {
            //        var segmentType = result.UsedSegmentTypes[currIndex];

            //        switch (segmentType)
            //        {
            //            case SearchResult.SegmentType.Transfer:
            //                totalTimeBeforeNextTrip += result.UsedTransfers[currTransferIndex].time;
            //                currTransferIndex--;
            //                break;
            //            case SearchResult.SegmentType.Bike:
            //                totalTimeBeforeNextTrip += result.UsedBikeTrips[currBikeTripIndex].time;
            //                currBikeTripIndex--;
            //                break;
            //            case SearchResult.SegmentType.Trip:
            //                var usedTrip = result.UsedTrips[currTripIndex];
            //                if (last)
            //                {
            //                    last = false;
            //                }
            //                else
            //                {
            //                    var srcStopEntry = usedTrip.stopPasses[usedTrip.getOnStopIndex];
            //                    var destStopEntry = usedTrip.stopPasses[usedTrip.getOffStopIndex];
            //                    var srcStopId = srcStopEntry.Id;
            //                    var destStopId = destStopEntry.Id;
            //                    var getOnTime = srcStopEntry.DepartureTime.AddSeconds(usedTrip.delayWhenBoarded);

            //                    var latestTimeToGetToNextTrip = followingTripDepartureTime.AddSeconds(-totalTimeBeforeNextTrip);

            //                    var altTripsRequest = new AlternativeTripsRequest
            //                    {
            //                        srcStopId = srcStopId,
            //                        destStopId = destStopId,
            //                        dateTime = getOnTime,
            //                        count = 10,
            //                        previous = false,
            //                        tripId = usedTrip.tripId
            //                    };

            //                    var altTripsResponse = altRouteFinder.GetAlternativeTrips(altTripsRequest);

            //                    if (altTripsResponse.Error == AlternativesSearchError.NoError)
            //                    {
            //                        for (int i = altTripsResponse.Alternatives.Count - 1; i >= 0; i--)
            //                        {
            //                            var altTrip = altTripsResponse.Alternatives[i];
            //                            var altTripArrivalTime = altTrip.stopPasses[altTrip.getOffStopIndex].ArrivalTime;

            //                            if (altTripArrivalTime < latestTimeToGetToNextTrip)
            //                            {
            //                                result.UsedTrips[currTripIndex] = altTrip;
            //                                break;
            //                            }
            //                        }
            //                    }
            //                }
            //                followingTripDepartureTime = usedTrip.stopPasses[usedTrip.getOnStopIndex].DepartureTime.AddSeconds(usedTrip.delayWhenBoarded);
            //                totalTimeBeforeNextTrip = 0;
            //                currTripIndex--;
            //                break;
            //            default:
            //                break;
            //        }

            //        currIndex--;
            //    }
            //} else if (!forward && result.UsedTrips.Count >= 2)
            //{
            //    var firstTrip = result.UsedTrips[0];
            //    DateTime previousTripArrivalTime = firstTrip.stopPasses[firstTrip.getOffStopIndex].ArrivalTime
            //        .AddSeconds(firstTrip.delayWhenBoarded);
            //    var altRouteFinder = RouteFinderBuilder.CreateDirectRouteFinder();

            //    var firstTripSegmentIndex = result.UsedSegmentTypes.FindIndex(type => type == SearchResult.SegmentType.Trip);
            //    var secondTripSegmentIndex = result.UsedSegmentTypes.FindIndex(firstTripSegmentIndex + 1, type => type == SearchResult.SegmentType.Trip);

            //    int currIndex = 0;
            //    int currTripIndex = 0;
            //    int currTransferIndex = 0;
            //    int currBikeTripIndex = 0;
            //    int totalTimeAfterPreviousTrip = 0;
            //    bool first = true;
            //    while (currIndex < result.UsedSegmentTypes.Count)
            //    {
            //        var segmentType = result.UsedSegmentTypes[currIndex];

            //        switch (segmentType)
            //        {
            //            case SearchResult.SegmentType.Transfer:
            //                totalTimeAfterPreviousTrip += result.UsedTransfers[currTransferIndex].time;
            //                currTransferIndex++;
            //                break;
            //            case SearchResult.SegmentType.Bike:
            //                totalTimeAfterPreviousTrip += result.UsedBikeTrips[currBikeTripIndex].time;
            //                currBikeTripIndex++;
            //                break;
            //            case SearchResult.SegmentType.Trip:
            //                var usedTrip = result.UsedTrips[currTripIndex];
            //                if (first)
            //                {
            //                    first = false;
            //                }
            //                else
            //                {
            //                    var srcStopEntry = usedTrip.stopPasses[usedTrip.getOnStopIndex];
            //                    var destStopEntry = usedTrip.stopPasses[usedTrip.getOffStopIndex];
            //                    var srcStopId = srcStopEntry.Id;
            //                    var destStopId = destStopEntry.Id;
            //                    var getOnTime = srcStopEntry.DepartureTime.AddSeconds(usedTrip.delayWhenBoarded);

            //                    var earliestTimeToGetFromPreviousTrip = previousTripArrivalTime.AddSeconds(totalTimeAfterPreviousTrip);

            //                    var altTripsRequest = new AlternativeTripsRequest
            //                    {
            //                        srcStopId = srcStopId,
            //                        destStopId = destStopId,
            //                        dateTime = getOnTime,
            //                        count = 10,
            //                        previous = true,
            //                        tripId = usedTrip.tripId
            //                    };

            //                    var altTripsResponse = altRouteFinder.GetAlternativeTrips(altTripsRequest);

            //                    if (altTripsResponse.Error == AlternativesSearchError.NoError)
            //                    {
            //                        for (int i = 0; i < altTripsResponse.Alternatives.Count; i++)
            //                        {
            //                            var altTrip = altTripsResponse.Alternatives[i];
            //                            var altTripDepartureTime = altTrip.stopPasses[altTrip.getOnStopIndex].DepartureTime;

            //                            if (altTripDepartureTime > earliestTimeToGetFromPreviousTrip)
            //                            {
            //                                result.UsedTrips[currTripIndex] = altTrip;
            //                                break;
            //                            }
            //                        }
            //                    }
            //                }
            //                previousTripArrivalTime = usedTrip.stopPasses[usedTrip.getOffStopIndex].ArrivalTime.AddSeconds(usedTrip.delayWhenBoarded);
            //                totalTimeAfterPreviousTrip = 0;
            //                currTripIndex++;
            //                break;
            //        }

            //        currIndex++;
            //    }
            //}





            //if (result.UsedTrips.Count >= 2)
            //{
            //    var altRouteFinder = RouteFinderBuilder.CreateDirectRouteFinder();
            //    DateTime tripTime = forward
            //        ? result.UsedTrips[^1].stopPasses[result.UsedTrips[^1].getOnStopIndex].DepartureTime
            //            .AddSeconds(result.UsedTrips[^1].delayWhenBoarded)
            //        : result.UsedTrips[0].stopPasses[result.UsedTrips[0].getOffStopIndex].ArrivalTime
            //            .AddSeconds(result.UsedTrips[0].delayWhenBoarded);

            //    int currIndex = forward ? result.UsedSegmentTypes.Count - 1 : 0;
            //    int tripIndex = forward ? result.UsedTrips.Count - 1 : 0;
            //    int transferIndex = forward ? result.UsedTransfers.Count - 1 : 0;
            //    int bikeIndex = forward ? result.UsedBikeTrips.Count - 1 : 0;

            //    int timeBuffer = 0;
            //    bool firstOrLast = true;

            //    while (forward ? currIndex >= 0 : currIndex < result.UsedSegmentTypes.Count)
            //    {
            //        var segmentType = result.UsedSegmentTypes[currIndex];

            //        switch (segmentType)
            //        {
            //            case SearchResult.SegmentType.Transfer:
            //                timeBuffer += result.UsedTransfers[transferIndex].time;
            //                transferIndex += forward ? -1 : 1;
            //                break;
            //            case SearchResult.SegmentType.Bike:
            //                timeBuffer += result.UsedBikeTrips[bikeIndex].time;
            //                bikeIndex += forward ? -1 : 1;
            //                break;
            //            case SearchResult.SegmentType.Trip:
            //                var usedTrip = result.UsedTrips[tripIndex];
            //                if (firstOrLast)
            //                {
            //                    firstOrLast = false;
            //                }
            //                else
            //                {
            //                    var srcStop = usedTrip.stopPasses[usedTrip.getOnStopIndex];
            //                    var destStop = usedTrip.stopPasses[usedTrip.getOffStopIndex];
            //                    var tripBoundaryTime = forward
            //                        ? tripTime.AddSeconds(-timeBuffer)
            //                        : tripTime.AddSeconds(timeBuffer);

            //                    var altTripsRequest = new AlternativeTripsRequest
            //                    {
            //                        srcStopId = srcStop.Id,
            //                        destStopId = destStop.Id,
            //                        dateTime = srcStop.DepartureTime.AddSeconds(usedTrip.delayWhenBoarded),
            //                        count = 10,
            //                        previous = !forward,
            //                        tripId = usedTrip.tripId
            //                    };

            //                    var altTripsResponse = altRouteFinder.GetAlternativeTrips(altTripsRequest);

            //                    if (altTripsResponse.Error == AlternativesSearchError.NoError)
            //                    {
            //                        var alternatives = altTripsResponse.Alternatives;
            //                        for (int i = forward ? alternatives.Count - 1 : 0;
            //                            forward ? i >= 0 : i < alternatives.Count;
            //                            i += forward ? -1 : 1)
            //                        {
            //                            var altTrip = alternatives[i];
            //                            var altTripTime = forward
            //                                ? altTrip.stopPasses[altTrip.getOffStopIndex].ArrivalTime
            //                                : altTrip.stopPasses[altTrip.getOnStopIndex].DepartureTime;

            //                            if ((forward && altTripTime < tripBoundaryTime) ||
            //                                (!forward && altTripTime > tripBoundaryTime))
            //                            {
            //                                result.UsedTrips[tripIndex] = altTrip;
            //                                break;
            //                            }
            //                        }
            //                    }
            //                }

            //                tripTime = forward
            //                    ? usedTrip.stopPasses[usedTrip.getOnStopIndex].DepartureTime.AddSeconds(usedTrip.delayWhenBoarded)
            //                    : usedTrip.stopPasses[usedTrip.getOffStopIndex].ArrivalTime.AddSeconds(usedTrip.delayWhenBoarded);

            //                timeBuffer = 0;
            //                tripIndex += forward ? -1 : 1;
            //                break;
            //        }

            //        currIndex += forward ? -1 : 1;
            //    }
            //}



            result.InitializeAlternatives();

            return result;


            int GetCurrentTripDelay(Trip trip, DateOnly tripStartDate)
            {
                TripStopDelays stopDelays = delayModel.GetTripStopDelaysUnsafe(tripStartDate, trip.Id);
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
        }

        /// <summary>
        /// For a given round, gets the stop with the best reach time in the round
        /// </summary>
        /// <param name="round">The round</param>
        /// <returns>The stop with the best reach time in the round</returns>
        private Stop? GetSearchEndStopWithBestReachTimeInRound(int round)
        {
            Stop? stopWithBestReachTime = null;
            DateTime bestReachTime = worstBound;


            foreach (Stop stop in searchEndStops)
            {
                DateTime reachTime;
                if (searchEndCustomRoutePoint is not null)
                {
                    CustomTransfer transfer = searchEndCustomRoutePoint.GetTransferWithNormalRP(stop);
                    int transferTime = timeMpl * settingsUsed.GetAdjustedWalkingTransferTime(transfer.Distance);
                    DateTime stopReachTime = GetBestReachTimeInRound(stop, round);
                    if (stopReachTime == worstBound)
                    {
                        reachTime = stopReachTime;
                    }
                    else
                    {
                        reachTime = stopReachTime.AddSeconds(transferTime);
                    }
                }
                else
                {
                    reachTime = GetBestReachTimeInRound(stop, round);
                }

                // Reach is better than best we found so far AND it is better than in last round - otherwise we do not process this round
                if (
                    routingInfo.ContainsKey(stop)
                    && comp.ImprovesTime(reachTime, bestReachTime)
                    && (round == 0 || ReachTimeAtStopInRoundIsBetterThanAllEarlierRounds(stop, round))
                ) {
                    stopWithBestReachTime = stop;
                    bestReachTime = reachTime;
                }
            }
            return stopWithBestReachTime;
        }

        /// <summary>
        /// Gets the best search end stops with the best reach times for each round
        /// </summary>
        /// <returns>The search end stops with the best reach times for each round</returns>
        private Stop?[] GetSearchEndStopsWithBestReachTimesByRounds()
        {
            Stop?[] bestSearchEndStops = new Stop[Settings.ROUNDS + 1];
            for (int round = 0; round <= Settings.ROUNDS; round++)
            {
                bestSearchEndStops[round] = GetSearchEndStopWithBestReachTimeInRound(round);
            }

            return bestSearchEndStops;
        }

        /// <summary>
        /// Finds out whether the reach time at the specified stop in the specified round is better than the best reach time in previous rounds at that point
        /// </summary>
        /// <param name="stop">The stop</param>
        /// <param name="round">The round</param>
        /// <returns>Whether the reach time is better than all earlier ones</returns>
        private bool ReachTimeAtStopInRoundIsBetterThanAllEarlierRounds(Stop stop, int round)
        {
            DateTime bestEarlierArrival = worstBound;
            for (int i = 0; i < round; i++)
            {
                DateTime reachInRoundI = GetBestReachTimeInRound(stop, i);
                if (comp.ImprovesTime(reachInRoundI, bestEarlierArrival))
                {
                    bestEarlierArrival = reachInRoundI;
                }
            }

            return comp.ImprovesTime(GetBestReachTimeInRound(stop, round), bestEarlierArrival);
        }

        /// <summary>
        /// Gets the search result with the best reach time for every round
        /// </summary>
        /// <param name="bestSearchEndStops">The stops with best reach time in each round</param>
        /// <param name="bikeModel">The bike model</param>
        /// <returns>Search results with the best reach time for every round</returns>
        private SearchResult?[] CreateResultsFromBestStops(Stop?[] bestSearchEndStops, BikeModel bikeModel)
        {
            SearchResult?[] resultsRounds = new SearchResult[Settings.ROUNDS + 1];
            for (int round = 0; round < Settings.ROUNDS + 1; round++)
            {
                resultsRounds[round] = CreateResultFromStopInRound(bestSearchEndStops[round], round, bikeModel);
            }

            return resultsRounds;
        }

        /// <summary>
        /// Extracts the results of the search from the current state of the search model and returns them
        /// </summary>
        /// <param name="bikeModel">The bike model</param>
        /// <returns>The results of a search</returns>
        /// <remarks>As opposed to the ExtractResult function, if results for multiple rounds
        /// are close enough in time and comfort, returns them all</remarks>
        public List<SearchResult>? ExtractResultWithAlternatives(BikeModel bikeModel)
        {
            // For each round, get the stop with the earliest arrival
            Stop?[] bestSearchEndStops = GetSearchEndStopsWithBestReachTimesByRounds();

            SearchResult?[] resultsRounds = CreateResultsFromBestStops(bestSearchEndStops, bikeModel);

            return GetBestResults(resultsRounds, bestSearchEndStops);


            List<SearchResult>? GetBestResults(SearchResult?[] results, Stop?[] earliestDestStops)
            {
                int bestRound = -1;
                DateTime bestArrivalTime = worstBound;
                DateTime[] adjustedArrivalTimes = new DateTime[Settings.ROUNDS + 1];
                for (int round = 0; round < results.Length; round++)
                {
                    if (earliestDestStops[round] is not null)
                    {
                        var usedSegmentTypes = results[round]!.UsedSegmentTypes;

                        bool startsWithTransfer = usedSegmentTypes[0] == SearchResult.SegmentType.Transfer;
                        bool endsWithTransfer = usedSegmentTypes[^1] == SearchResult.SegmentType.Transfer;

                        int penaltySecondsPerTransfer = settingsUsed.GetTransferPenaltySeconds();

                        int transferCount = round == 0 ? round : round - 1;
                        if (startsWithTransfer)
                        {
                            transferCount++;
                        }
                        if (endsWithTransfer)
                        {
                            transferCount++;
                        }

                        int totalTransferTime = timeMpl * transferCount * penaltySecondsPerTransfer;

                        DateTime adjustedArrivalTime = GetBestReachTimeInRound(earliestDestStops[round]!, round).AddSeconds(totalTransferTime);
                        adjustedArrivalTimes[round] = adjustedArrivalTime;

                        if (comp.ImprovesTime(adjustedArrivalTime, bestArrivalTime))
                        {
                            bestArrivalTime = adjustedArrivalTime;
                            bestRound = round;
                        }
                    }
                }

                if (bestRound == -1)
                {
                    return null;
                }

                List<SearchResult> bestResults = new();

                for(int i = 0; i < Settings.ROUNDS + 1; i++)
                {
                    DateTime adjustedArrivalTime = adjustedArrivalTimes[i];
                    if (comp.ImprovesTime(adjustedArrivalTime, bestArrivalTime.AddMinutes(timeMpl * 5)))//adjustedArrivalTime <= bestArrivalTime.AddMinutes(5))
                    {
                        bestResults.Add(results[i]!);
                    }
                }

                return bestResults;
            }
        }


        /// <summary>
        /// Extracts the result of the search from the current state of the search model and returns it
        /// </summary>
        /// <returns>The best result found in the search</returns>
        /// <remarks>As opposed to the ExtractResultWithAlternatives function, only extracts the single best result</remarks>
        public SearchResult? ExtractResult(BikeModel bikeModel)
        {
            // For each round, get the stop with the earliest arrival
            Stop?[] bestSearchEndStops = GetSearchEndStopsWithBestReachTimesByRounds();

            SearchResult?[] resultsRounds = CreateResultsFromBestStops(bestSearchEndStops, bikeModel);

            return GetBestResult(resultsRounds, bestSearchEndStops);


            SearchResult? GetBestResult(SearchResult?[] results, Stop?[] earliestDestStops)
            {
                int bestRound = -1;
                DateTime bestArrivalTime = worstBound;
                for (int round = 0; round < results.Length; round++)
                {
                    if (earliestDestStops[round] is not null)
                    {
                        var usedSegmentTypes = results[round]!.UsedSegmentTypes;

                        bool startsWithTransfer = usedSegmentTypes[0] == SearchResult.SegmentType.Transfer;
                        bool endsWithTransfer = usedSegmentTypes[^1] == SearchResult.SegmentType.Transfer;

                        int penaltySecondsPerTransfer = settingsUsed.GetTransferPenaltySeconds();

                        int transferCount = round == 0 ? round : round - 1;
                        if (startsWithTransfer)
                        {
                            transferCount++;
                        }
                        if (endsWithTransfer)
                        {
                            transferCount++;
                        }

                        int totalTransferTime = timeMpl * transferCount * penaltySecondsPerTransfer;

                        DateTime adjustedArrivalTime = GetBestReachTimeInRound(earliestDestStops[round]!, round).AddSeconds(totalTransferTime);

                        if (comp.ImprovesTime(adjustedArrivalTime, bestArrivalTime))
                        {
                            bestArrivalTime = adjustedArrivalTime;
                            bestRound = round;
                        }
                    }
                }

                if (bestRound == -1)
                {
                    return null;
                }
                return results[bestRound];
            }

            
        }


        /// <summary>
        /// Gets the search begin time
        /// </summary>
        /// <returns>The search begin time</returns>
        public DateTime GetSearchBeginTime()
        {
            return searchBeginTime;
        }
        /// <summary>
        /// Gets the current best search end reach time
        /// </summary>
        /// <returns>The best current search end reach time</returns>
        public DateTime GetCurrentBestSearchEndTime()
        {
            return bestCurrentSearchEndTime;
        }
        /// <summary>
        /// Gets the best currently possible reach time to the specified RoutePoint
        /// </summary>
        /// <param name="rp">The RoutePoint to get the best arrival time to</param>
        /// <returns>The best possible arrival time to the RoutePoint</returns>
        public DateTime GetBestReachTime(IRoutePoint rp)
        {
            return GetRoutingInfo(rp).BestReachTime;
        }
        /// <summary>
        /// Gets the best currently possible reach time from the search begin RoutePoint to the specified RoutePoint in the specified round (i.e. with exactly so many trips)
        /// </summary>
        /// <param name="rp">The RoutePoint to get the best reach time to</param>
        /// <param name="round">The round to get the information in</param>
        /// <returns>The best possible reach time at the RoutePoint in the specified round</returns>
        public DateTime GetBestReachTimeInRound(IRoutePoint rp, int round)
        {
            var reach = GetRoutingInfo(rp).Reaches[round];
            if (reach is null)
            {
                return worstBound;
            }
            else
            {
                return reach.Time;
            }
        }
        /// <summary>
        /// Finds out whether the reachTime at the specified RoutePoint in the specified round is better than the best reach time so far at that point
        /// </summary>
        /// <param name="reachTime">The reach time at the route point</param>
        /// <param name="rp">The route point</param>
        /// <returns>Whether the reach time improves the current best reach time at the route point</returns>
        private bool ReachTimeImprovesCurrBest(DateTime reachTime, IRoutePoint rp)
        {
            int maxTripLengthDays = timeMpl * Settings.MAX_TRIP_LENGTH_DAYS;

            bool betterThanCurrent = comp.ImprovesTime(reachTime, GetBestReachTime(rp));
            bool betterThanCurrentBestSearchEndTime = comp.ImprovesTime(reachTime, GetCurrentBestSearchEndTime());
            bool notExceedsMaxTripLength = comp.ImprovesOrEqualsTime(reachTime, GetSearchBeginTime().AddDays(maxTripLengthDays));

            return betterThanCurrent && betterThanCurrentBestSearchEndTime && notExceedsMaxTripLength;
        }

        /// <summary>
        /// Using the trip, tries to improve the reach time at the specified stop in the specified round
        /// </summary>
        /// <remarks>If the reach time cannot be improved (i.e. isn't better than the current), nothing is changed and the function returns false</remarks>
        /// <param name="stop">The stop to try to improve reach time at</param>
        /// <param name="reachTime">The reach time at the stop using the trip</param>
        /// <param name="trip">The trip to try improving with</param>
        /// <param name="tripDate">The start date of the trip</param>
        /// <param name="reachedFromStop">The stop from which the trip was taken (i.e. where it is boarded for forward or exited for backward search)</param>
        /// <param name="round">The round in which we are improving</param>
        /// <returns>Whether the reach time was improved</returns>
        public bool TryImproveReachTimeByTrip(Stop stop, DateTime reachTime, Trip trip, DateOnly tripDate, Stop reachedFromStop, int round)
        {
            if (stop.Id == "U9367Z301")
            {
                Console.WriteLine();
            }
            bool improves = ReachTimeImprovesCurrBest(reachTime, stop);

            if (improves)
            {
                SetTripReachInRound(stop, trip, tripDate, reachedFromStop, reachTime, round);
                SetBestReachTime(stop, reachTime);

                // Check if it is best arrival so far. Only check if the destination is NOT a custom route point
                if (searchEndCustomRoutePoint is null)
                {
                    if (searchEndStops.Contains(stop) && comp.ImprovesTime(reachTime, GetCurrentBestSearchEndTime()))
                    {
                        SetCurrentBestSearchEndTime(reachTime);
                    }
                }
                else
                {
                    if (searchEndCustomRoutePoint.transferDistances.TryGetValue(stop, out var distance))
                    {
                        int transferDuration = settingsUsed.GetAdjustedWalkingTransferTime(distance);
                        DateTime arrivalAtCustomRP = reachTime.AddSeconds(transferDuration);
                        if (comp.ImprovesTime(arrivalAtCustomRP, GetCurrentBestSearchEndTime()))
                        {
                            SetCurrentBestSearchEndTime(arrivalAtCustomRP);
                        }
                    }
                }
            }

            return improves;
        }

        /// <summary>
        /// Using the bike trip, tries to improve the reach time at the specified bike station in the specified round
        /// </summary>
        /// <remarks>If the reach time cannot be improved (i.e. isn't better than the current), nothing is changed and the function returns false</remarks>
        /// <param name="realSrcBikeStation">The real life source station (i.e. the one where the actual bike trip starts)</param>
        /// <param name="realDestBikeStation">The real life destination station (i.e. the one where the actual bike trip ends)</param>
        /// <param name="reachTime">The time at which the newly reached bike station was reached</param>
        /// <param name="round">The round in which we are trying to improve</param>
        /// <returns>Whether the reach time at the bike station was improved.</returns>

        // TODO: Change from learLifeSrc to searchSrc??
        public bool TryImproveReachTimeByBikeTrip(BikeStation realSrcBikeStation, BikeStation realDestBikeStation,
            DateTime reachTime, int round)
        {
            BikeStation newlyReachedBikeStation = forward ? realDestBikeStation : realSrcBikeStation;
            BikeStation reachedFromBikeStation = forward ? realSrcBikeStation : realDestBikeStation;

            bool improves = ReachTimeImprovesCurrBest(reachTime, newlyReachedBikeStation);

            if (improves)
            {
                SetBikeTripReachInRound(reachedFromBikeStation, newlyReachedBikeStation, reachTime, round);
                SetBestReachTime(newlyReachedBikeStation, reachTime);
                //TODO: CHECK!!!

                // Check if it is best arrival so far. Only check if the destination is NOT a custom route point
                if (searchEndCustomRoutePoint is null)
                {
                    if (searchEndBikeStations.Contains(realDestBikeStation) && comp.ImprovesTime(reachTime, GetCurrentBestSearchEndTime()))
                    {
                        SetCurrentBestSearchEndTime(reachTime);
                    }
                }
                else
                {
                    if (searchEndCustomRoutePoint.transferDistances.TryGetValue(realDestBikeStation, out var distance))
                    {
                        int transferDuration = timeMpl * settingsUsed.GetAdjustedWalkingTransferTime(distance);
                        DateTime reachAtCustomRP = reachTime.AddSeconds(transferDuration);
                        if (comp.ImprovesTime(reachAtCustomRP, GetCurrentBestSearchEndTime()))
                        {
                            SetCurrentBestSearchEndTime(reachAtCustomRP);
                        }
                    }
                }

            }

            return improves;
        }

        /// <summary>
        /// Gets the reach time to the routing point where the transfer is heading to
        /// </summary>
        /// <param name="transfer">The real life transfer to get the reach time for. </param>
        /// <param name="round">The round for which to get the information</param>
        /// <param name="toBikeStation">Whether the transfer leads to a bike station</param>
        /// <returns>The time at which the transfer is completed</returns>
        private DateTime GetReachTimeUsingTransfer(ITransfer transfer, int round, bool toBikeStation)
        {
            IRoutePoint improvingFromPoint, improvingToPoint;
            if (forward)
            {
                improvingFromPoint = transfer.GetSrcRoutePoint();
                improvingToPoint = transfer.GetDestRoutePoint();
            }
            else
            {
                improvingFromPoint = transfer.GetDestRoutePoint();
                improvingToPoint = transfer.GetSrcRoutePoint();
            }
            //IRoutePoint src = transfer.GetSrcRoutePoint();
            DateTime bestReachTimeToImprovingFromPoint = GetBestReachTimeInRound(improvingFromPoint, round);


            int transferTime = timeMpl * settingsUsed.GetAdjustedWalkingTransferTime(transfer.Distance);


            int stationaryTransferTime = timeMpl * settingsUsed.GetStationaryTransferMinimumSeconds();

            int movingStopToStopTransferTime = forward
                ? Math.Max(transferTime, stationaryTransferTime)
                : Math.Min(transferTime, stationaryTransferTime);

            DateTime bestReachUsingTransfer;
            if (transfer.Distance == 0)
            {
                // if transfer length is 0, the transfer is stationary -> it takes the time of the stationary transfer minimum
                bestReachUsingTransfer = bestReachTimeToImprovingFromPoint.AddSeconds(stationaryTransferTime);
            }
            else
            {
                // if transfer length is not 0, the transfer is moving -> it takes the time of the moving transfer length
                if (toBikeStation)
                {
                    // The transfer time does NOT need to respect the stationary transfer minimum -> the bike is always there
                    bestReachUsingTransfer = bestReachTimeToImprovingFromPoint.AddSeconds(transferTime);// + bikeUnlockTime);
                }
                else
                {
                    // The bike unlock time does NOT need to be added, the transfer time DOES NEED to respect the stationary transfer minimum
                    bestReachUsingTransfer = bestReachTimeToImprovingFromPoint.AddSeconds(movingStopToStopTransferTime);
                }
            }

            return bestReachUsingTransfer;
        }

        /// <summary>
        /// Using the transfer, tries to improve the reach time at the route point it leads to in the specified round
        /// </summary>
        /// <remarks>If the reach time cannot be improved (i.e. isn't better than the current), nothing is changed and the function returns false</remarks>
        /// <param name="realTransfer">The real life transfer (independent on search direction)</param>
        /// <param name="toBikeStation">Whether the transfer leads to a bike station</param>
        /// <param name="round">The round in which we are trying to improve</param>
        /// <param name="DoNotImproveToRoutePoint">A functor specifying for any route point whether we can improve reach time there by a transfer</param>
        /// <returns>Whether the reach time at the route point was improved.</returns>
        public bool TryImproveReachTimeByTransfer(ITransfer realTransfer, bool toBikeStation, int round, Func<IRoutePoint, bool>? DoNotImproveToRoutePoint = null)
        {
            IRoutePoint realSrc = realTransfer.GetSrcRoutePoint();
            IRoutePoint realDest = realTransfer.GetDestRoutePoint();

            IRoutePoint improvingFromPoint, improvingToPoint;
            if (forward)
            {
                improvingFromPoint = realSrc;
                improvingToPoint = realDest;
            }
            else
            {
                improvingFromPoint = realDest;
                improvingToPoint = realSrc;
            }
            int maxTransferDistance = settingsUsed.GetMaxTransferDistance();

            bool canBeUsed = realTransfer.Distance <= maxTransferDistance || realSrc.Name == realDest.Name;
            bool transferForbiddenToImprToPoint = DoNotImproveToRoutePoint is not null && DoNotImproveToRoutePoint(improvingToPoint);
            bool imprFromPointReachedByTransferInRound = RoutePointIsReachedByTransferInRound(improvingFromPoint, round);
            if (!canBeUsed || transferForbiddenToImprToPoint || imprFromPointReachedByTransferInRound) return false;



            DateTime reachTimeWithTransfer = GetReachTimeUsingTransfer(realTransfer, round, toBikeStation);
            DateTime currEarliestReach = GetBestReachTime(improvingToPoint);

            bool wouldImprove = comp.ImprovesTime(reachTimeWithTransfer, currEarliestReach);

            bool improves = wouldImprove;

            if (improves)
            {
                SetTransferReachInRound(improvingToPoint, realTransfer, reachTimeWithTransfer, round);
                SetBestReachTime(improvingToPoint, reachTimeWithTransfer);

                // TODO: CHECK FOR GLOBAL BEST TIME
                if (searchEndCustomRoutePoint is null)
                {
                    if (toBikeStation)
                    {
                        if (searchEndBikeStations.Contains(improvingToPoint) && comp.ImprovesTime(reachTimeWithTransfer, GetCurrentBestSearchEndTime()))
                        {
                            SetCurrentBestSearchEndTime(reachTimeWithTransfer);
                        }
                    }
                    else
                    {
                        if (searchEndStops.Contains(improvingToPoint) && comp.ImprovesTime(reachTimeWithTransfer, GetCurrentBestSearchEndTime()))
                        {
                            SetCurrentBestSearchEndTime(reachTimeWithTransfer);
                        }
                    }
                }
                else
                {
                    // Improvement to one of the destRoutePoints cannot happen -> Custom RP is not a part of the search algorithm, and the stops before it need to be reached by a trip or bike trip
                }
            }

            return improves;
        }


        /// <summary>
        /// Sets the current overall best global search end time (to any of the search end stops)
        /// </summary>
        /// <param name="searchEndTime">The best search end time to set</param>
        public void SetCurrentBestSearchEndTime(DateTime searchEndTime)
        {
            bestCurrentSearchEndTime = searchEndTime;
        }
        /// <summary>
        /// Sets the current overall best reach time to the specified route point
        /// </summary>
        /// <param name="rp">The stop to set the best reach time for</param>
        /// <param name="reachTime">The best reach time to set</param>
        public void SetBestReachTime(IRoutePoint rp, DateTime reachTime)
        {
            // TODO: Check if it is enough to set the bestCurrentSearchEndTime here. If yes, add check for searchEndBikeStations
            GetRoutingInfo(rp).BestReachTime = reachTime;
            if (searchEndStops.Contains(rp) && comp.ImprovesTime(reachTime, bestCurrentSearchEndTime))
            {
                bestCurrentSearchEndTime = reachTime;
            }
        }

        /// <summary>
        /// Sets a trip reach to the specified stop in the specified round.
        /// </summary>
        /// <param name="stop">The stop to which the reach is being set</param>
        /// <param name="trip">The trip to be taken to the stop</param>
        /// <param name="tripDate">The start date of the trip</param>
        /// <param name="otherEndStop">The stop at which the trip was reached during the search (get on/off stop)</param>
        /// <param name="reachTime">The time at which the trip reaches the stop</param>
        /// <param name="round">The round in which to set the reach</param>
        public void SetTripReachInRound(Stop stop, Trip trip, DateOnly tripDate, Stop otherEndStop, DateTime reachTime, int round)
        {
            StopRoutingInfo.TripReach tripReach = new StopRoutingInfo.TripReach(trip, otherEndStop, reachTime, tripDate);
            if (round == 6)
            {
                Console.WriteLine();
            }
            GetRoutingInfo(stop).Reaches[round] = tripReach;
        }

        /// <summary>
        /// Sets a transfer reach to the specified RoutePoint in the specified round
        /// </summary>
        /// <param name="rp">The RoutePoint to which the reach is being set</param>
        /// <param name="transfer">The transfer to use</param>
        /// <param name="reachTime">The time at which the RoutePoint is reached by the transfer</param>
        /// <param name="round">The round in which to set the reach</param>
        /// <exception cref="NotImplementedException">Thrown if the arrival transfer is not between 2 stops, stop and bike station or custom route point and normal route point</exception>
        public void SetTransferReachInRound(IRoutePoint rp, ITransfer transfer, DateTime reachTime, int round)
        {
            if (transfer is Transfer t)
            {
                StopRoutingInfo.TransferReach transferReach = new StopRoutingInfo.TransferReach(t, reachTime);
                GetRoutingInfo(rp).Reaches[round] = transferReach;
            }
            else if (transfer is BikeTransfer bt)
            {
                StopRoutingInfo.BikeTransferReach bikeTransferReach = new StopRoutingInfo.BikeTransferReach(bt, reachTime);
                GetRoutingInfo(rp).Reaches[round] = bikeTransferReach;
            }
            else if (transfer is CustomTransfer ct)
            {
                StopRoutingInfo.CustomTransferReach customTransferReach = new StopRoutingInfo.CustomTransferReach(ct, reachTime);
                GetRoutingInfo(rp).Reaches[round] = customTransferReach;
            }
            else
            {
                throw new NotImplementedException();
            }

        }


        /// <summary>
        /// Sets a bike trip reach (from the other station) to the specified station in the specified round.
        /// </summary>
        /// <param name="reachedFrom">The bike station from which we reached the station</param>
        /// <param name="reachedTo">The station to which the reach is being set</param>
        /// <param name="reachTime">The time at which the station is reached</param>
        /// <param name="round">The round in which to set the reach</param>
        public void SetBikeTripReachInRound(BikeStation reachedFrom, BikeStation reachedTo, DateTime reachTime, int round)
        {
            StopRoutingInfo.BikeTripReach bikeTripReach = new StopRoutingInfo.BikeTripReach(reachedFrom, reachedTo, reachTime);
            GetRoutingInfo(reachedTo).Reaches[round] = bikeTripReach;
        }

        /// <summary>
        /// Initiates the search by setting the reach times to all the search begin stops as the search begin time
        /// </summary>
        public void SetSearchBeginStopsReachTime()
        {
            foreach (Stop sourceStop in searchBeginStops)
            {
                StopRoutingInfo stopRoutingInfo = GetRoutingInfo(sourceStop);
                stopRoutingInfo.BestReachTime = searchBeginTime;
                stopRoutingInfo.Reaches[0] = new StopRoutingInfo.ImplicitSearchStartReach(searchBeginTime);
            }
        }

        /// <summary>
        /// Initiates the search by setting the reach times to all the search begin bike stations as the search begin time
        /// </summary>
        public void SetSearchBeginBikeStationsReachTime()
        {
            foreach (BikeStation sourceBikeStation in searchBeginBikeStations)
            {
                StopRoutingInfo stopRoutingInfo = GetRoutingInfo(sourceBikeStation);
                stopRoutingInfo.BestReachTime = searchBeginTime;
                stopRoutingInfo.Reaches[0] = new StopRoutingInfo.ImplicitSearchStartReach(searchBeginTime);
            }
        }

        /// <summary>
        /// Finds out whether the specified RoutePoint was reached by a transfer in the specified round
        /// </summary>
        /// <remarks>Typically used to ensure two transfers are not performed after one another</remarks>
        /// <param name="rp">The RoutePoint</param>
        /// <param name="round">The round</param>
        /// <returns>Bool, specifying whether the RoutePoint was reached by a transfer in the round</returns>
        public bool RoutePointIsReachedByTransferInRound(IRoutePoint rp, int round)
        {
            StopRoutingInfo.IRoutingEntry? arrival = GetRoutingInfo(rp).Reaches[round];
            return arrival is StopRoutingInfo.TransferReach || arrival is StopRoutingInfo.BikeTransferReach || arrival is StopRoutingInfo.CustomTransferReach;
        }

        /// <summary>
        /// Finds out whether the specified RoutePoint was reached by a bike trip in the specified round
        /// </summary>
        /// <remarks>Typically used to ensure two bike trips are not performed after one another</remarks>
        /// <param name="rp">The RoutePoint</param>
        /// <param name="round">The round</param>
        /// <returns>Bool, specifying whether the RoutePoint was reached by a bike trip in the round</returns>
        public bool RoutePointIsReachedByBikeInRound(IRoutePoint rp, int round)
        {
            //var ri = GetRoutingInfo(rp);
            StopRoutingInfo.IRoutingEntry? arrival = GetRoutingInfo(rp).Reaches[round];
            return arrival is StopRoutingInfo.BikeTripReach;
        }

        /// <summary>
        /// Finds out whether the specified RoutePoint was reached by a public transit trip in the specified round
        /// </summary>
        /// <param name="rp">The RoutePoint</param>
        /// <param name="round">The round</param>
        /// <returns>Bool, specifying whether the RoutePoint was reached by a public transit trip in the round</returns>
        public bool RoutePointIsReachedByTripInRound(IRoutePoint rp, int round)
        {
            return GetRoutingInfo(rp).Reaches[round] is StopRoutingInfo.TripReach;
        }
        /// <summary>
        /// Gets the routing info for the specified stop if it exists. If not, creates a new one, adds it to the routingInfo and returns it
        /// </summary>
        /// <param name="rp">The RoutePoint for which to get the routing info</param>
        /// <returns>The routing info of the specified RoutePoint</returns>
        private StopRoutingInfo GetRoutingInfo(IRoutePoint rp)
        {
            if (routingInfo.ContainsKey(rp))
            {
                return routingInfo[rp];
            }
            else
            {
                StopRoutingInfo stopRoutingInfo = new StopRoutingInfo(forward);
                routingInfo.Add(rp, stopRoutingInfo);
                return stopRoutingInfo;
            }
        }
    }
}

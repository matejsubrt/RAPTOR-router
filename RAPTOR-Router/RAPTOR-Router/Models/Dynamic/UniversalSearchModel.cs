using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Interfaces;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Custom;

namespace RAPTOR_Router.Models.Dynamic
{

    /// <summary>
    /// A class holding all the dynamic data of a single forward connection search. The ForwardRouteFinder uses this class to store the data of the search.
    /// </summary>
    /// <remarks>Is used for searches, where the earliest possible departure time is known, and we need to calculate the earliest possible arrival time to the destination.</remarks>
    internal class UniversalSearchModel
    {
        /// <summary>
        /// List of all the public transit stops considered as the source
        /// </summary>
        public List<Stop> searchBeginStops { get; set; }
        /// <summary>
        /// List of all the public transit stops considered as the destination
        /// </summary>
        public List<Stop> searchEndStops { get; set; }
        /// <summary>
        /// List of all the bike stations considered as the source
        /// </summary>
        public List<BikeStation> searchBeginBikeStations { get; set; }
        /// <summary>
        /// List of all the bike stations considered as the destination
        /// </summary>
        public List<BikeStation> searchEndBikeStations { get; set; }
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
        protected Settings settingsUsed;

        private bool forward;
        private DateTime worstBound;

        private DelayModel delayModel;




        /// <summary>
        /// Dictionary indexed by the RoutePoints, holding the current routing information about each RoutePoint
        /// </summary>
        private readonly Dictionary<IRoutePoint, UniversalStopRoutingInfo> routingInfo = new();

        private readonly DateTime searchBeginTime;
        private DateTime bestCurrentSearchEndTime;

        private TimeComparator comp;

        private int timeMpl;

        /// <summary>
        /// Creates a new ForwardSearchModel object
        /// </summary>
        /// <param name="searchBeginStops">The list of stops considered as the source stops - typically stops from one node sharing the same name</param>
        /// <param name="searchEndStops">The list of stops considered as the destination stops - typically stops from one node sharing the same name</param>
        /// <param name="searchBeginBikeStations">The list of bikeStations considered as the source stations</param>
        /// <param name="searchEndBikeStations">The list of bikeStations considered as the destination stations</param>
        /// <param name="searchBeginTime">The earliest possible departure time of the found connection</param>
        /// <param name="settingsUsed">The settings used for the search</param>
        public UniversalSearchModel(bool forward, List<Stop> searchBeginStops, List<Stop> searchEndStops, List<BikeStation> searchBeginBikeStations, List<BikeStation> searchEndBikeStations, DateTime searchBeginTime, Settings settingsUsed, DelayModel delayModel)
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
        /// Extracts the result of the search from the current state of the search model and returns it
        /// </summary>
        /// <returns>The best result found in the search</returns>
        /// <exception cref="ApplicationException">Thrown if the extraction fails, meaning the search model was in an invalid state.</exception>
        public SearchResult? ExtractResult(BikeModel bikeModel)
        {
            // For each round, get the stop with the earliest arrival
            Stop?[] searchEndStopsWithBestReachTimesRounds = new Stop[Settings.ROUNDS];
            for (int round = 0; round < Settings.ROUNDS; round++)
            {
                searchEndStopsWithBestReachTimesRounds[round] = GetSearchEndStopWithBestReachTimeInRound(round);
            }

            SearchResult?[] resultsRounds = new SearchResult[Settings.ROUNDS];
            for (int round = 0; round < Settings.ROUNDS; round++)
            {
                resultsRounds[round] = CreateResultFromStopInRound(searchEndStopsWithBestReachTimesRounds[round], round);
            }

            return GetBestResult(resultsRounds, searchEndStopsWithBestReachTimesRounds);


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

            SearchResult? CreateResultFromStopInRound(Stop? stop, int round)
            {
                if (stop is null)
                {
                    return null;
                }
                SearchResult result = new(settingsUsed);
                UniversalStopRoutingInfo currStopInfo = routingInfo[stop];

                // TODO: This should be done when finding the best dest stop, not after!!!
                if (searchEndCustomRoutePoint is not null)
                {
                    CustomTransfer transfer = searchEndCustomRoutePoint.GetTransferWithNormalRP(stop);
                    DateTime arrivalTimeAtDestCustomRP = currStopInfo.Reaches[round].Time.AddSeconds(timeMpl * transfer.GetTransferTime(settingsUsed.WalkingPace));
                    result.AddUsedTransfer(transfer, arrivalTimeAtDestCustomRP, !forward);
                }

                IRoutePoint nextRoundStartStop = stop;
                int currRound = round;
                while (currRound > 0)
                {
                    IRoutePoint currStop;

                    var reach = currStopInfo.Reaches[currRound];
                    if (reach is StopRoutingInfoBase.TransferReach transferReach)
                    {
                        result.AddUsedTransfer(transferReach.Transfer, transferReach.Time, !forward);
                        currStop = forward ? transferReach.Transfer.From : transferReach.Transfer.To;
                    }
                    else if (reach is StopRoutingInfoBase.BikeTransferReach bikeTransferReach)
                    {
                        result.AddUsedTransfer(bikeTransferReach.Transfer, bikeTransferReach.Time, !forward);
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
                                result.AddUsedTransfer(new Transfer(s, s, 0), currStopInfo.Reaches[currRound].Time.AddSeconds(timeMpl * settingsUsed.GetStationaryTransferMinimumSeconds()), !forward);
                            }
                        }
                    }

                    currStopInfo = routingInfo[currStop];
                    reach = currStopInfo.Reaches[currRound];

                    if (reach is StopRoutingInfoBase.TripReach tripReach)
                    {
                        Stop realGetOnStop, realGetOffStop;

                        if (forward)
                        {
                            realGetOnStop = tripReach.OtherEndStop;
                            realGetOffStop = (Stop)currStop;
                        }
                        else
                        {
                            realGetOnStop = (Stop)currStop;
                            realGetOffStop = tripReach.OtherEndStop;
                        }

                        Trip tripToReachStop = tripReach.Trip;
                        if (tripToReachStop is null || tripReach.OtherEndStop is null)
                        {
                            throw new ApplicationException("Trip and getOnStop cannot be null in an used round");
                        }

                        bool tripStartedDayBefore = tripToReachStop.StopTimes[0].DepartureTime > TimeOnly.FromDateTime(tripReach.Time);
                        DateOnly tripStartDate = tripStartedDayBefore ? DateOnly.FromDateTime(tripReach.Time.AddDays(-1)) : DateOnly.FromDateTime(tripReach.Time.Date);

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


                        


                        result.AddUsedTrip(tripToReachStop, realGetOnStop, realGetOffStop, tripReach.Time, getOnStopHasDelayData, getOnStopDepartureDelay, currentTripDelay, !forward);



                        //result.AddUsedTrip(tripToReachStop, realGetOnStop, realGetOffStop, tripReach.Time, !forward);
                        currStop = tripReach.OtherEndStop;
                    }
                    else if (reach is StopRoutingInfoBase.BikeTripReach bikeTripReach)
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
                if (firstReach is StopRoutingInfoBase.TransferReach transferReach1)
                {
                    result.AddUsedTransfer(transferReach1.Transfer, firstReach.Time, !forward);
                }
                else if (firstReach is StopRoutingInfoBase.BikeTransferReach bikeTransferReach)
                {
                    result.AddUsedTransfer(bikeTransferReach.Transfer, firstReach.Time, !forward);
                }
                else if (firstReach is StopRoutingInfoBase.CustomTransferReach customTransferReach)
                {
                    result.AddUsedTransfer(customTransferReach.Transfer, customTransferReach.Time, !forward);
                }

                //TODO: implement other direction
                result.SetDepartureAndArrivalTimesByEarliestDeparture(searchBeginTime);

                return result;


                int GetCurrentTripDelay(Trip trip, DateOnly tripStartDate)
                {
                    TripStopDelays stopDelays = delayModel.GetTripStopDelays(tripStartDate, trip.Id);
                    List<StopTime> stopTimes = trip.StopTimes;

                    TimeOnly currTime = TimeOnly.FromDateTime(DateTime.Now);

                    
                    //bool haveLastStopDelay = stopDelays.TryGetStopDelay(0, out int lastReachedStopArrivalDelay, out int lastReachedStopDepartureDelay);
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

            Stop? GetSearchEndStopWithBestReachTimeInRound(int round)
            {
                Stop? stopWithBestReachTime = null;
                DateTime bestReachTime = worstBound;
                foreach (Stop stop in searchEndStops)
                {
                    //arrival is earlier than best we found so far AND it is better than in last round - otherwise we do not process this round
                    if (
                        routingInfo.ContainsKey(stop)
                        && comp.ImprovesTime(GetBestReachTimeInRound(stop, round), bestReachTime)//GetBestReachTimeInRound(stop, round) < bestReachTime
                        && (round == 0 || ArrivalAtStopInRoundIsBetterThanAllEarlierRounds(stop, round))
                    )
                    {
                        stopWithBestReachTime = stop;
                        bestReachTime = GetBestReachTimeInRound(stop, round);
                    }
                }
                return stopWithBestReachTime;
            }

            bool ArrivalAtStopInRoundIsBetterThanAllEarlierRounds(Stop stop, int round)
            {
                DateTime bestEarlierArrival = worstBound;//DateTime.MaxValue;
                for (int i = 0; i < round; i++)
                {
                    DateTime reachInRoundI = GetBestReachTimeInRound(stop, i);
                    if (comp.ImprovesTime(reachInRoundI, bestEarlierArrival))//reachInRoundI < bestEarlierArrival)
                    {
                        bestEarlierArrival = reachInRoundI;
                    }
                }

                return comp.ImprovesTime(GetBestReachTimeInRound(stop, round), bestEarlierArrival); //bestEarlierArrival > GetBestReachTimeInRound(stop, round);
            }
        }

        /// <summary>
        /// Gets the earliest possible departure time of the search
        /// </summary>
        /// <returns>The departure time</returns>
        public DateTime GetSearchBeginTime()
        {
            return searchBeginTime;
        }
        /// <summary>
        /// Gets the current best/earliest arrival time at the destination
        /// </summary>
        /// <returns>The best current arrival time</returns>
        public DateTime GetCurrentBestSearchEndTime()
        {
            return bestCurrentSearchEndTime;
        }
        /// <summary>
        /// Gets the earliest currently possible arrival time to the specified RoutePoint
        /// </summary>
        /// <param name="rp">The RoutePoint to get the earliest arrival to</param>
        /// <returns>The earliest possible arrival time to the RoutePoint</returns>
        public DateTime GetBestReachTime(IRoutePoint rp)
        {
            return GetRoutingInfo(rp).BestReachTime;
        }







        /// <summary>
        /// Gets the earliest currently possible arrival time from the source RoutePoint to the specified RoutePoint in the specified round (i.e. with exactly so many trips)
        /// </summary>
        /// <param name="rp">The RoutePoint to get the earliest arrival to</param>
        /// <param name="round">The round to get the information in</param>
        /// <returns>The earliest possible arrival time to the RoutePoint in the specified round</returns>
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

        private bool ReachTimeImprovesCurrBest(DateTime reachTime, IRoutePoint rp)
        {
            int maxTripLengthDays = timeMpl * Settings.MAX_TRIP_LENGTH_DAYS;

            bool betterThanCurrent = comp.ImprovesTime(reachTime, GetBestReachTime(rp));
            bool betterThanCurrentBestSearchEndTime = comp.ImprovesTime(reachTime, GetCurrentBestSearchEndTime());
            bool notExceedsMaxTripLength = comp.ImprovesOrEqualsTime(reachTime, GetSearchBeginTime().AddDays(maxTripLengthDays));

            return betterThanCurrent && betterThanCurrentBestSearchEndTime && notExceedsMaxTripLength;
        }

        public bool TryImproveReachTimeByTrip(Stop stop, DateTime reachTime, Trip trip, Stop getOnStop, int round)
        {
            bool improves = ReachTimeImprovesCurrBest(reachTime, stop);

            if (improves)
            {
                SetTripReachInRound(stop, trip, getOnStop, reachTime, round);
                SetBestReachTime(stop, reachTime);

                // Check if it is best arrival so far. Only check if the destination is NOT a custom route point
                if (searchEndCustomRoutePoint is null)
                {
                    if (searchEndStops.Contains(stop) && comp.ImprovesTime(reachTime, GetCurrentBestSearchEndTime()))     // arrivalTime < GetCurrentBestSearchEndTime())
                    {
                        SetCurrentBestSearchEndTime(reachTime);
                    }
                }
                else
                {
                    if (searchEndCustomRoutePoint.transferDistances.TryGetValue(stop, out var distance))
                    {
                        int transferDuration = timeMpl * (int)(distance * settingsUsed.WalkingPace * settingsUsed.GetMovingTransferLengthMultiplier());
                        DateTime arrivalAtCustomRP = reachTime.AddSeconds(transferDuration);
                        if (comp.ImprovesTime(arrivalAtCustomRP, GetCurrentBestSearchEndTime()))// arrivalAtCustomRP < GetCurrentBestSearchEndTime())
                        {
                            SetCurrentBestSearchEndTime(arrivalAtCustomRP);
                        }
                    }
                }
            }

            return improves;
        }

        public bool TryImproveReachTimeByBikeTrip(BikeStation realSrcBikeStation, BikeStation realDestBikeStation,
            DateTime reachTime, int round)
        {
            BikeStation newlyReachedBikeStation = forward ? realDestBikeStation : realSrcBikeStation;
            BikeStation reachedFromBikeStation = forward ? realSrcBikeStation : realDestBikeStation;

            if (newlyReachedBikeStation.Name == "P5 - Nemocnice Motol výstup z metra E6")
            {
                Console.WriteLine();
            }

            bool improves = ReachTimeImprovesCurrBest(reachTime, newlyReachedBikeStation);

            if (improves)
            {
                SetBikeTripReachInRound(reachedFromBikeStation, newlyReachedBikeStation, reachTime, round);
                SetBestReachTime(newlyReachedBikeStation, reachTime);
                //TODO: CHECK!!!

                // Check if it is best arrival so far. Only check if the destination is NOT a custom route point
                if (searchEndCustomRoutePoint is null)
                {
                    if (searchEndBikeStations.Contains(realDestBikeStation) && comp.ImprovesTime(reachTime, GetCurrentBestSearchEndTime()))     //reachTime < GetCurrentBestSearchEndTime())
                    {
                        SetCurrentBestSearchEndTime(reachTime);
                    }
                }
                else
                {
                    if (searchEndCustomRoutePoint.transferDistances.TryGetValue(realDestBikeStation, out var distance))
                    {
                        int transferDuration = timeMpl * (int)(distance * settingsUsed.WalkingPace * settingsUsed.GetMovingTransferLengthMultiplier());// + settingsUsed.BikeLockTime;
                        DateTime reachAtCustomRP = reachTime.AddSeconds(transferDuration);
                        if (comp.ImprovesTime(reachAtCustomRP, GetCurrentBestSearchEndTime()))    //arrivalAtCustomRP < GetCurrentBestSearchEndTime())
                        {
                            SetCurrentBestSearchEndTime(reachAtCustomRP);
                        }
                    }
                }

            }

            return improves;
        }

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

            //int transferTimeBase = transfer.GetTransferTime(settingsUsed.WalkingPace);
            //double movingTransferMultiplier = settingsUsed.GetMovingTransferLengthMultiplier();
            //int transferTimeAdjusted = timeMpl * (int)(transferTimeBase * movingTransferMultiplier);
            //int bikeUnlockTime = timeMpl * settingsUsed.BikeUnlockTime;

            int stationaryTransferTime = timeMpl * settingsUsed.GetStationaryTransferMinimumSeconds();

            //int movingStopToStopTransferTime = forward
            //    ? Math.Max(transferTimeAdjusted, stationaryTransferSeconds)
            //    : Math.Min(transferTimeAdjusted, stationaryTransferSeconds);

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


        public bool TryImproveReachTimeByTransfer(ITransfer realTransfer, bool toBikeStation, int round, Func<IRoutePoint, bool> DoNotImproveToRoutePoint = null)
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

            //bool wouldImprove = TransferImprovesArrivalTime(transfer);
            bool wouldImprove = comp.ImprovesTime(reachTimeWithTransfer, currEarliestReach);// reachWithTransfer < currEarliestReach;

            bool improves = wouldImprove; // and implicitly canBeUsed && !transferForbiddenToDest && !srcReachedByTransferInRound

            if (improves)
            {
                SetTransferReachInRound(improvingToPoint, realTransfer, reachTimeWithTransfer, round);
                SetBestReachTime(improvingToPoint, reachTimeWithTransfer);

                // TODO: CHECK FOR GLOBAL BEST TIME
                if (searchEndCustomRoutePoint is null)
                {
                    if (toBikeStation)
                    {
                        if (searchEndBikeStations.Contains(improvingToPoint) && comp.ImprovesTime(reachTimeWithTransfer, GetCurrentBestSearchEndTime()))// reachTimeWithTransfer < GetCurrentBestSearchEndTime())
                        {
                            SetCurrentBestSearchEndTime(reachTimeWithTransfer);
                        }
                    }
                    else
                    {
                        if (searchEndStops.Contains(improvingToPoint) && comp.ImprovesTime(reachTimeWithTransfer, GetCurrentBestSearchEndTime()))// reachTimeWithTransfer < GetCurrentBestSearchEndTime())
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
        /// Sets the current overall best arrival time to any of the destination stops
        /// </summary>
        /// <param name="searchEndTime">The best arrival time to set</param>
        public void SetCurrentBestSearchEndTime(DateTime searchEndTime)
        {
            bestCurrentSearchEndTime = searchEndTime;
        }
        /// <summary>
        /// Sets the current overall best arrival time to the specified stop
        /// </summary>
        /// <param name="rp">The stop to set the earliest arrival for</param>
        /// <param name="reachTime">The earliest arrival time to set</param>
        public void SetBestReachTime(IRoutePoint rp, DateTime reachTime)
        {
            // TODO: Check if it is enough to set the bestCurrentSearchEndTime here. If yes, add check for searchEndBikeStations
            GetRoutingInfo(rp).BestReachTime = reachTime;
            if (searchEndStops.Contains(rp) && comp.ImprovesTime(reachTime, bestCurrentSearchEndTime))// reachTime < bestCurrentSearchEndTime)
            {
                bestCurrentSearchEndTime = reachTime;
            }
        }

        /// <summary>
        /// Sets an arrival by trip to the specified stop in the specified round.
        /// </summary>
        /// <param name="stop">The stop to which the arrival is being set</param>
        /// <param name="trip">The trip to be taken to the stop</param>
        /// <param name="otherEndStop">The stop at which the trip was boarded</param>
        /// <param name="reachTime">The time at which the trip arrives at the stop</param>
        /// <param name="round">The round in which to set the arrival</param>
        public void SetTripReachInRound(Stop stop, Trip trip, Stop otherEndStop, DateTime reachTime, int round)
        {
            StopRoutingInfoBase.TripReach tripReach = new StopRoutingInfoBase.TripReach(trip, otherEndStop, reachTime);
            GetRoutingInfo(stop).Reaches[round] = tripReach;
        }

        /// <summary>
        /// Sets an arrival by transfer to the specified RoutePoint in the specified round
        /// </summary>
        /// <param name="rp">The RoutePoint to which the arrival is being set</param>
        /// <param name="transfer">The transfer to use</param>
        /// <param name="reachTime">The time at which the RoutePoint sis reached by the transfer</param>
        /// <param name="round">The round in which to set the arrival</param>
        /// <exception cref="NotImplementedException">Thrown if the arrival transfer is not between 2 stops, stop and bike station or custom route point and normal route point</exception>
        public void SetTransferReachInRound(IRoutePoint rp, ITransfer transfer, DateTime reachTime, int round)
        {
            if (transfer is Transfer t)
            {
                StopRoutingInfoBase.TransferReach transferReach = new StopRoutingInfoBase.TransferReach(t, reachTime);
                GetRoutingInfo(rp).Reaches[round] = transferReach;
            }
            else if (transfer is BikeTransfer bt)
            {
                StopRoutingInfoBase.BikeTransferReach bikeTransferReach = new StopRoutingInfoBase.BikeTransferReach(bt, reachTime);
                GetRoutingInfo(rp).Reaches[round] = bikeTransferReach;
            }
            else if (transfer is CustomTransfer ct)
            {
                StopRoutingInfoBase.CustomTransferReach customTransferReach = new StopRoutingInfoBase.CustomTransferReach(ct, reachTime);
                GetRoutingInfo(rp).Reaches[round] = customTransferReach;
            }
            else
            {
                throw new NotImplementedException();
            }

        }


        /// <summary>
        /// Sets an arrival by bike trip (from the other station) to the specified station in the specified round.
        /// </summary>
        /// <param name="reachedFrom">The source station</param>
        /// <param name="reachedTo">The station to which the arrival is being made</param>
        /// <param name="reachTime">The time at which the destination station is reached</param>
        /// <param name="round">The round in which to set the arrival</param>
        public void SetBikeTripReachInRound(BikeStation reachedFrom, BikeStation reachedTo, DateTime reachTime, int round)
        {
            StopRoutingInfoBase.BikeTripReach bikeTripReach = new StopRoutingInfoBase.BikeTripReach(reachedFrom, reachedTo, reachTime);
            GetRoutingInfo(reachedTo).Reaches[round] = bikeTripReach;
        }

        /// <summary>
        /// Initiates the search by setting the earliest arrival times to all the source stops as the departure time
        /// </summary>
        public void SetSearchBeginStopsReachTime()
        {
            foreach (Stop sourceStop in searchBeginStops)
            {
                UniversalStopRoutingInfo stopRoutingInfo = GetRoutingInfo(sourceStop);
                stopRoutingInfo.BestReachTime = searchBeginTime;
                stopRoutingInfo.Reaches[0] = new StopRoutingInfoBase.ImplicitSearchStartReach(searchBeginTime);
            }
        }

        /// <summary>
        /// Initiates the search by setting the earliest arrival times to all the source bike stations as the departure time
        /// </summary>
        public void SetSearchBeginBikeStationsReachTime()
        {
            foreach (BikeStation sourceBikeStation in searchBeginBikeStations)
            {
                UniversalStopRoutingInfo stopRoutingInfo = GetRoutingInfo(sourceBikeStation);
                stopRoutingInfo.BestReachTime = searchBeginTime;
                stopRoutingInfo.Reaches[0] = new StopRoutingInfoBase.ImplicitSearchStartReach(searchBeginTime);
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
            StopRoutingInfoBase.IEntry arrival = GetRoutingInfo(rp).Reaches[round];
            return arrival is StopRoutingInfoBase.TransferReach || arrival is StopRoutingInfoBase.BikeTransferReach || arrival is StopRoutingInfoBase.CustomTransferReach;
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
            StopRoutingInfoBase.IEntry arrival = GetRoutingInfo(rp).Reaches[round];
            return arrival is StopRoutingInfoBase.BikeTripReach;
        }

        /// <summary>
        /// Finds out whether the specified RoutePoint was reached by a public transit trip in the specified round
        /// </summary>
        /// <param name="rp">The RoutePoint</param>
        /// <param name="round">The round</param>
        /// <returns>Bool, specifying whether the RoutePoint was reached by a public transit trip in the round</returns>
        public bool RoutePointIsReachedByTripInRound(IRoutePoint rp, int round)
        {
            return GetRoutingInfo(rp).Reaches[round] is StopRoutingInfoBase.TripReach;
        }
        /// <summary>
        /// Gets the routing info for the specified stop if it exists. If not, creates a new one, adds it to the routingInfo and returns it
        /// </summary>
        /// <param name="rp">The RoutePoint for which to get the routing info</param>
        /// <returns>The routing info of the specified RoutePoint</returns>
        private UniversalStopRoutingInfo GetRoutingInfo(IRoutePoint rp)
        {
            if (routingInfo.ContainsKey(rp))
            {
                return routingInfo[rp];
            }
            else
            {
                UniversalStopRoutingInfo stopRoutingInfo = new UniversalStopRoutingInfo(forward);
                routingInfo.Add(rp, stopRoutingInfo);
                return stopRoutingInfo;
            }
        }
    }
}

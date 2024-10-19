using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Interfaces;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Custom;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.Models.Static;

namespace RAPTOR_Router.Models.Dynamic
{

    /// <summary>
    /// A class holding all the dynamic data of a single backward connection search. The BackwardRouteFinder uses this class to store the data of the search.
    /// </summary>
    /// <remarks>Is used for searches, where the latest possible arrival time is known and we need to calculate the latest possible departure time to arrive on time.</remarks>
    public class BackwardSearchModel : SearchModelBase
    {
        /// <summary>
        /// Dictionary indexed by the RoutePoints, holding the current routing information about each RoutePoint
        /// </summary>
        private Dictionary<IRoutePoint, BackwardStopRoutingInfo> routingInfo = new();

        private DateTime arrivalTime;
        private DateTime bestCurrentDepartureTime = DateTime.MinValue;

        /// <summary>
        /// Creates a new BackwardSearchModel object
        /// </summary>
        /// <param name="sourceStops">The list of stops considered as the source stops - typically stops from one node sharing the same name</param>
        /// <param name="destinationStops">The list of stops considered as the destination stops - typically stops from one node sharing the same name</param>
        /// <param name="sourceBikeStations">The list of bikeStations considered as the source stations</param>
        /// <param name="destinationBikeStations">The list of bikeStations considered as the destination stations</param>
        /// <param name="arrivalTime">The latest possible arrival time at the destination</param>
        /// <param name="settingsUsed">The settings used for the search</param>
        public BackwardSearchModel(List<Stop> sourceStops, List<Stop> destinationStops, List<BikeStation> sourceBikeStations, List<BikeStation> destinationBikeStations, DateTime arrivalTime, Settings settingsUsed)
            : base(sourceStops, destinationStops, sourceBikeStations, destinationBikeStations, settingsUsed)
        {
            this.arrivalTime = arrivalTime;
        }


        /// <summary>
        /// Extracts the result of the search from the current state of the search model and returns it
        /// </summary>
        /// <returns>The best result found in the search</returns>
        /// <exception cref="ApplicationException">Thrown if the extraction fails, meaning the search model was in an invalid state.</exception>
        public SearchResult ExtractResult(BikeModel bikeModel)
        {
            // For each round, get the stop with earliest arrival
            Stop[] latestSrcStopsRounds = new Stop[Settings.ROUNDS];
            for (int round = 0; round < Settings.ROUNDS; round++)
            {
                latestSrcStopsRounds[round] = GetSrcStopWithMaxDepartureTimeInRound(round);
            }

            SearchResult[] resultsRounds = new SearchResult[Settings.ROUNDS];
            for (int round = 0; round < Settings.ROUNDS; round++)
            {
                resultsRounds[round] = CreateResultFromStopInRound(latestSrcStopsRounds[round], round);
            }

            return GetBestResult(resultsRounds, latestSrcStopsRounds);


            SearchResult GetBestResult(SearchResult[] results, Stop[] latestSrcStops)
            {
                int bestRound = -1;
                DateTime bestDepartureTime = DateTime.MinValue;
                for (int round = 0; round < results.Length; round++)
                {
                    if (latestSrcStops[round] is not null)
                    {
                        var usedSegmentTypes = results[round].UsedSegmentTypes;

                        bool startsWithTransfer = usedSegmentTypes[0] == SearchResult.SegmentType.Transfer;
                        bool endsWithTransfer = usedSegmentTypes[usedSegmentTypes.Count - 1] == SearchResult.SegmentType.Transfer;

                        int penaltySecondsPerTransfer = settingsUsed.GetTransferPenaltySeconds(); // TODO: check if i didnt add this somewhere wrong

                        int transferCount = round == 0 ? round : round - 1;
                        if (startsWithTransfer)
                        {
                            transferCount++;
                        }
                        if (endsWithTransfer)
                        {
                            transferCount++;
                        }

                        var stopInfo = routingInfo[latestSrcStops[round]];

                        DateTime adjustedDepartureTime = GetLatestDepartureInRound(latestSrcStops[round], round).AddSeconds(-(transferCount * penaltySecondsPerTransfer));
                        //DateTime adjustedArrivalTime = stopInfo.earliestArrivalRounds[round].AddSeconds(transferCount * penaltySecondsPerTransfer);
                        //earliestArrivalRounds[round] = adjustedArrivalTime;

                        if (adjustedDepartureTime > bestDepartureTime)
                        {
                            bestDepartureTime = adjustedDepartureTime;
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

            SearchResult CreateResultFromStopInRound(Stop stop, int round)
            {
                if (stop is null)
                {
                    return null;
                }
                SearchResult result = new(settingsUsed);
                BackwardStopRoutingInfo currStopInfo = routingInfo[stop];


                if (sourceCustomRoutePoint is not null)
                {
                    CustomTransfer transfer = sourceCustomRoutePoint.GetTransferWithNormalRP(stop);
                    DateTime departureTimeAtSrcCustomRP = currStopInfo.Departures[round].Time.AddSeconds(-transfer.GetTransferTime(settingsUsed.WalkingPace));
                    result.AddUsedTransfer(transfer, departureTimeAtSrcCustomRP, true);
                }

                IRoutePoint nextRoundDestStop = stop;
                int currRound = round;
                while (currRound > 0)
                {
                    bool transferUsed = false;


                    IRoutePoint currStop;

                    var departure = currStopInfo.Departures[currRound];
                    if (departure is StopRoutingInfoBase.TransferDeparture)
                    {
                        StopRoutingInfoBase.TransferDeparture transferDeparture = departure as StopRoutingInfoBase.TransferDeparture;
                        result.AddUsedTransfer(transferDeparture.Transfer, transferDeparture.Time, true);
                        transferUsed = true;
                        currStop = transferDeparture.Transfer.To;
                    }
                    else if (departure is StopRoutingInfoBase.BikeTransferDeparture)
                    {
                        StopRoutingInfoBase.BikeTransferDeparture bikeTransferDeparture = departure as StopRoutingInfoBase.BikeTransferDeparture;
                        result.AddUsedTransfer(bikeTransferDeparture.Transfer, bikeTransferDeparture.Time, true);
                        transferUsed = true;
                        currStop = bikeTransferDeparture.Transfer.GetDestRoutePoint();
                    }


                    // In current round, no transfer has been used, i.e. we are continuing from the exact same stop -> we add a new 0 length transfer
                    else
                    {
                        currStop = nextRoundDestStop;
                        if (currStop is Stop)
                        {
                            if (currRound != round)
                            {
                                Stop s = currStop as Stop;
                                // in last round, we do not add a transfer
                                result.AddUsedTransfer(new Transfer(s, s, 0), currStopInfo.Departures[currRound].Time.AddSeconds(-settingsUsed.GetStationaryTransferMinimumSeconds()), true);
                            }
                        }
                    }

                    currStopInfo = routingInfo[currStop];
                    //Trip tripToReachStop = currStopInfo.tripsToReachRounds[currRound];
                    //Stop getOnStop = currStopInfo.getOnStopsToReachRounds[currRound];
                    departure = currStopInfo.Departures[currRound];

                    if (departure is StopRoutingInfoBase.TripDeparture)
                    {
                        StopRoutingInfoBase.TripDeparture tripDeparture = departure as StopRoutingInfoBase.TripDeparture;
                        Trip tripToReachStop = tripDeparture.Trip;
                        Stop getOffStop = tripDeparture.GetOffStop;
                        if (tripToReachStop is null || getOffStop is null)
                        {
                            throw new ApplicationException("Trip and getOffStop cannot be null in an used round");
                        }
                        //result.AddUsedTrip(tripToReachStop, (Stop)currStop, getOffStop, tripDeparture.Time, true);
                        currStop = getOffStop;
                    }
                    else if (departure is StopRoutingInfoBase.BikeTripDeparture)
                    {
                        StopRoutingInfoBase.BikeTripDeparture bikeTripDeparture = departure as StopRoutingInfoBase.BikeTripDeparture;
                        result.AddUsedBikeTrip(bikeTripDeparture.From, bikeTripDeparture.To, bikeModel.GetDistanceBetweenStations(bikeTripDeparture.From, bikeTripDeparture.To), true);
                        currStop = bikeTripDeparture.To;
                    }



                    currStopInfo = routingInfo[currStop];
                    nextRoundDestStop = currStop;
                    currRound--;
                }

                //TODO: check
                // Add the first transfer to the result -> that would be in round 0 and thus not added in the loop above
                var lastDeparture = currStopInfo.Departures[0];
                if (lastDeparture is StopRoutingInfoBase.TransferDeparture)
                {
                    StopRoutingInfoBase.TransferDeparture transferDeparture = lastDeparture as StopRoutingInfoBase.TransferDeparture;
                    result.AddUsedTransfer(transferDeparture.Transfer, lastDeparture.Time, true);
                }
                else if (lastDeparture is StopRoutingInfoBase.BikeTransferDeparture)
                {
                    StopRoutingInfoBase.BikeTransferDeparture bikeTransferDeparture = lastDeparture as StopRoutingInfoBase.BikeTransferDeparture;
                    result.AddUsedTransfer(bikeTransferDeparture.Transfer, lastDeparture.Time, true);
                }
                else if (lastDeparture is StopRoutingInfoBase.CustomTransferDeparture)
                {
                    StopRoutingInfoBase.CustomTransferDeparture customTransferDeparture = lastDeparture as StopRoutingInfoBase.CustomTransferDeparture;
                    result.AddUsedTransfer(customTransferDeparture.Transfer, customTransferDeparture.Time, true);
                }

                result.SetDepartureAndArrivalTimesByLatestArrival(arrivalTime);

                return result;
            }

            Stop? GetSrcStopWithMaxDepartureTimeInRound(int round)
            {
                Stop? stopWithMaxDepartureTime = null;
                DateTime latestDeparture = DateTime.MinValue;
                foreach (Stop stop in sourceStops)
                {
                    //arrival is earlier than best we found so far AND it is better than in last round - otherwise we do not process this round
                    if (
                        routingInfo.ContainsKey(stop)
                        && GetLatestDepartureInRound(stop, round) > latestDeparture
                        && (round == 0 || DepartureFromStopInRoundIsBetterThanAllEarlierRounds(stop, round))
                    )
                    {
                        stopWithMaxDepartureTime = stop;
                        latestDeparture = GetLatestDepartureInRound(stop, round);
                    }
                }
                return stopWithMaxDepartureTime;
            }

            bool DepartureFromStopInRoundIsBetterThanAllEarlierRounds(Stop stop, int round)
            {
                DateTime bestDepartureInEarlierRound = DateTime.MinValue;
                for (int i = 0; i < round; i++)
                {
                    DateTime departureInRoundI = GetLatestDepartureInRound(stop, i);
                    if (departureInRoundI > bestDepartureInEarlierRound)
                    {
                        bestDepartureInEarlierRound = departureInRoundI;
                    }
                }
                return bestDepartureInEarlierRound < GetLatestDepartureInRound(stop, round);
            }
        }

        /// <summary>
        /// Gets the latest possible arrival time of the search
        /// </summary>
        /// <returns>The latest possible arrival time</returns>
        public DateTime GetArrivalTime()
        {
            return arrivalTime;
        }
        /// <summary>
        /// Gets the current best/latest found departure time
        /// </summary>
        /// <returns>The best current departure time</returns>
        public DateTime GetCurrentBestDepartureTime()
        {
            return bestCurrentDepartureTime;
        }
        /// <summary>
        /// Gets the latest currently possible departure time from the specified RoutePoint to the destination RoutePoint
        /// </summary>
        /// <param name="rp">The RoutePoint to get latest departure from</param>
        /// <returns>The latest possible departure time from the RoutePoint</returns>
        public DateTime GetLatestDeparture(IRoutePoint rp)
        {
            return GetRoutingInfo(rp).LatestDeparture;
        }

        /// <summary>
        /// Gets the latest currently possible departure time from the specified RoutePoint to the destination RoutePoint in the specified round (i.e. with exactly so many trips)
        /// </summary>
        /// <param name="rp">The RoutePoint to get latest departure from</param>
        /// <param name="round">The round to get the information in</param>
        /// <returns>The latest possible departure time from the RoutePoint in the specified round</returns>
        public DateTime GetLatestDepartureInRound(IRoutePoint rp, int round)
        {
            var arrival = GetRoutingInfo(rp).Departures[round];
            if (arrival is null)
            {
                return DateTime.MinValue;
            }
            else
            {
                return arrival.Time;
            }
        }

        private bool DepartureTimeImprovesCurrBest(DateTime departureTime, IRoutePoint rp)
        {
            return departureTime > GetLatestDeparture(rp)
                   && departureTime > GetCurrentBestDepartureTime()
                   && departureTime >= GetArrivalTime().AddDays(-Settings.MAX_TRIP_LENGTH_DAYS);
        }

        public bool TryImproveDepartureByTrip(Stop stop, DateTime departureTime, Trip trip, Stop getOffStop, int round)
        {
            bool improves = DepartureTimeImprovesCurrBest(departureTime, stop);

            if (improves)
            {
                SetTripDepartureInRound(stop, trip, getOffStop, departureTime, round);
                SetLatestDeparture(stop, departureTime);

                // Check if it is best departure so far. Only check if the source is NOT a custom route point
                if (sourceCustomRoutePoint is null)
                {
                    if (sourceStops.Contains(stop) && departureTime > GetCurrentBestDepartureTime())
                    {
                        SetCurrentBestDepartureTime(departureTime);
                    }
                }
                else
                {
                    if (sourceCustomRoutePoint.transferDistances.TryGetValue(stop, out var distance))
                    {
                        int transferDuration = (int)(distance * settingsUsed.WalkingPace * settingsUsed.GetMovingTransferLengthMultiplier());
                        DateTime departureFromCustomRP = departureTime.AddSeconds(-transferDuration);
                        if (departureFromCustomRP > GetCurrentBestDepartureTime())
                        {
                            SetCurrentBestDepartureTime(departureFromCustomRP);
                        }
                    }
                }
            }

            return improves;
        }

        public bool TryImproveDepartureByBikeTrip(BikeStation fromBikeStation, BikeStation toBikeStation,
            DateTime departureTime, int round)
        {
            bool improves = DepartureTimeImprovesCurrBest(departureTime, fromBikeStation);

            if (improves)
            {
                SetBikeTripDepartureInRound(fromBikeStation, toBikeStation, departureTime, round);
                SetLatestDeparture(fromBikeStation, departureTime);



                if (sourceCustomRoutePoint is null)
                {
                    if (sourceBikeStations.Contains(fromBikeStation) && departureTime > GetCurrentBestDepartureTime())
                    {
                        SetCurrentBestDepartureTime(departureTime);
                    }
                }
                else
                {
                    if (sourceCustomRoutePoint.transferDistances.TryGetValue(fromBikeStation, out var distance))
                    {
                        int transferDuration = (int)(distance * settingsUsed.WalkingPace * settingsUsed.GetMovingTransferLengthMultiplier()) + settingsUsed.BikeLockTime;
                        DateTime departureFromCustomRP = departureTime.AddSeconds(-transferDuration);
                        if (departureFromCustomRP > GetCurrentBestDepartureTime())
                        {
                            SetCurrentBestDepartureTime(departureFromCustomRP);
                        }
                    }
                }
            }

            return improves;
        }


        private DateTime GetDepartureTimeUsingTransfer(ITransfer transfer, int round, bool fromBikeStation)
        {
            IRoutePoint dest = transfer.GetDestRoutePoint();
            DateTime latestDepartureFromDest = GetLatestDepartureInRound(dest, round);

            int transferTimeBase = transfer.GetTransferTime(settingsUsed.WalkingPace);
            double movingTransferMultiplier = settingsUsed.GetMovingTransferLengthMultiplier();
            int transferTimeAdjusted = (int)(transferTimeBase * movingTransferMultiplier);
            int bikeUnlockTime = settingsUsed.BikeUnlockTime;

            int stationaryTransferSeconds = settingsUsed.GetStationaryTransferMinimumSeconds();

            DateTime latestDepartureUsingTransfer;
            if (transfer.Distance == 0)
            {
                //
                latestDepartureUsingTransfer = latestDepartureFromDest.AddSeconds(-stationaryTransferSeconds);
            }
            else
            {
                //
                if (fromBikeStation)
                {
                    //
                    latestDepartureUsingTransfer = latestDepartureFromDest.AddSeconds(-transferTimeAdjusted - bikeUnlockTime);
                }
                else
                {
                    //
                    latestDepartureUsingTransfer = latestDepartureFromDest.AddSeconds(-Math.Max(transferTimeAdjusted, stationaryTransferSeconds));
                }
            }

            return latestDepartureUsingTransfer;
        }


        public bool TryImproveDepartureByTransfer(ITransfer transfer, bool fromBikeStation, int round,
            Func<IRoutePoint, bool> DoNotImproveFromRoutePoint = null)
        {
            IRoutePoint src = transfer.GetSrcRoutePoint();
            IRoutePoint dest = transfer.GetDestRoutePoint();
            int maxTransferDistance = settingsUsed.GetMaxTransferDistance();

            bool canBeUsed = transfer.Distance <= maxTransferDistance || src.Name == dest.Name;
            bool transferForbiddenFromSrc = DoNotImproveFromRoutePoint is not null && DoNotImproveFromRoutePoint(src);
            bool destReachedByTransferInRound = RoutePointIsReachedByTransferInRound(dest, round);
            if (!canBeUsed || transferForbiddenFromSrc || destReachedByTransferInRound) return false;


            DateTime departureWithTransfer = GetDepartureTimeUsingTransfer(transfer, round, fromBikeStation);
            DateTime currLatestDeparture = GetLatestDeparture(src);


            bool wouldImprove = departureWithTransfer > currLatestDeparture;

            bool improves = wouldImprove; // and implicitly canBeUsed && !transferForbiddenToDest && !destReachedByTransferInRound

            if (improves)
            {
                SetTransferDepartureInRound(src, transfer, departureWithTransfer, round);
                SetLatestDeparture(src, departureWithTransfer);


                if (sourceCustomRoutePoint is null)
                {
                    if (fromBikeStation)
                    {
                        if (sourceBikeStations.Contains(src) && departureWithTransfer > GetCurrentBestDepartureTime())
                        {
                            SetCurrentBestDepartureTime(departureWithTransfer);
                        }
                    }
                    else
                    {
                        if (sourceStops.Contains(src) && departureWithTransfer > GetCurrentBestDepartureTime())
                        {
                            SetCurrentBestDepartureTime(departureWithTransfer);
                        }
                    }
                }
                else
                {
                    // Cannot improve the best departureTime
                }
            }

            return improves;
        }





        /// <summary>
        /// Sets the current overall best departure time found from any of the source RoutePoints
        /// </summary>
        /// <param name="departureTime">The best departure time to set</param>
        public void SetCurrentBestDepartureTime(DateTime departureTime)
        {
            bestCurrentDepartureTime = departureTime;
        }
        /// <summary>
        /// Sets the current overall best departure time from the specified RoutePoint to the destination stop
        /// </summary>
        /// <param name="rp">The RoutePoint to set the latest departure for</param>
        /// <param name="departureTime">The latest departure time to set</param>
        public void SetLatestDeparture(IRoutePoint rp, DateTime departureTime)
        {
            GetRoutingInfo(rp).LatestDeparture = departureTime;
            if (sourceStops.Contains(rp) && departureTime > bestCurrentDepartureTime)
            {
                bestCurrentDepartureTime = departureTime;
            }
        }

        /// <summary>
        /// Sets a departure by trip from the specified stop in the specified round.
        /// </summary>
        /// <param name="stop">The stop from which the departure is being set</param>
        /// <param name="trip">The trip to be taken from the stop to get nearer to the destination</param>
        /// <param name="getOffStop">The stop at which the boarded trip should be exited.</param>
        /// <param name="departureTime">The time at which the trip departs from the stop</param>
        /// <param name="round">The round in which to set the departure</param>
        public void SetTripDepartureInRound(Stop stop, Trip trip, Stop getOffStop, DateTime departureTime, int round)
        {
            StopRoutingInfoBase.TripDeparture tripArrival = new StopRoutingInfoBase.TripDeparture(trip, getOffStop, departureTime);
            GetRoutingInfo(stop).Departures[round] = tripArrival;
        }
        /// <summary>
        /// Sets a departure by transfer from the specified RoutePoint in the specified round
        /// </summary>
        /// <param name="rp">The RoutePoint from which the departure is being set</param>
        /// <param name="transfer">The transfer to use</param>
        /// <param name="departureTime">The time at which the RoutePoint should be left to make the next connecting segment at the transfer destination</param>
        /// <param name="round">The round in which to set the departure</param>
        /// <exception cref="NotImplementedException">Thrown if the departure transfer is not between 2 stops, stop and bike station or custom route point and normal route point</exception>
        public void SetTransferDepartureInRound(IRoutePoint rp, ITransfer transfer, DateTime departureTime, int round)
        {
            if (transfer is Transfer t)
            {
                StopRoutingInfoBase.TransferDeparture transferDeparture = new StopRoutingInfoBase.TransferDeparture(t, departureTime);
                GetRoutingInfo(rp).Departures[round] = transferDeparture;
            }
            else if (transfer is BikeTransfer bt)
            {
                StopRoutingInfoBase.BikeTransferDeparture bikeTransferDeparture = new StopRoutingInfoBase.BikeTransferDeparture(bt, departureTime);
                GetRoutingInfo(rp).Departures[round] = bikeTransferDeparture;
            }
            else if (transfer is CustomTransfer ct)
            {
                StopRoutingInfoBase.CustomTransferDeparture customTransferDeparture = new StopRoutingInfoBase.CustomTransferDeparture(ct, departureTime);
                GetRoutingInfo(rp).Departures[round] = customTransferDeparture;
                var ri = GetRoutingInfo(rp);
            }
            else
            {
                throw new NotImplementedException();
            }

        }
        /// <summary>
        /// Sets a departure by bike trip from the specified station to the other station in the specified round.
        /// </summary>
        /// <param name="from">The station from which the departure is being made</param>
        /// <param name="to">The destination station</param>
        /// <param name="departureTime">The time at which the source station should be left, to make the next connecting segment at the destination station</param>
        /// <param name="round">The round in which to set the departure</param>
        public void SetBikeTripDepartureInRound(BikeStation from, BikeStation to, DateTime departureTime, int round)
        {
            StopRoutingInfoBase.BikeTripDeparture bikeTripDeparture = new StopRoutingInfoBase.BikeTripDeparture(from, to, departureTime);
            GetRoutingInfo(from).Departures[round] = bikeTripDeparture; // TODO: check if from is correct here
        }
        /// <summary>
        /// Initiates the search by setting the latest arrival times to all the destination stops as the arrival time
        /// </summary>
        public void SetDestStopsLatestArrival()
        {
            foreach (Stop destStop in destinationStops)
            {
                BackwardStopRoutingInfo stopRoutingInfo = GetRoutingInfo(destStop);
                stopRoutingInfo.LatestDeparture = arrivalTime;
                stopRoutingInfo.Departures[0] = new StopRoutingInfoBase.ImplicitEndArrival(arrivalTime);
            }
        }
        /// <summary>
        /// Initiates the search by setting the latest arrival times to all the destination bike stations as the arrival time
        /// </summary>
        public void SetDestBikeStationsLatestArrival()
        {
            foreach (BikeStation destBikeStation in destinationBikeStations)
            {
                BackwardStopRoutingInfo stopRoutingInfo = GetRoutingInfo(destBikeStation);
                stopRoutingInfo.LatestDeparture = arrivalTime;
                stopRoutingInfo.Departures[0] = new StopRoutingInfoBase.ImplicitStartDeparture(arrivalTime);
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
            StopRoutingInfoBase.IEntry departure = GetRoutingInfo(rp).Departures[round];
            bool reachedByTransferInThisRound = departure is StopRoutingInfoBase.TransferDeparture || departure is StopRoutingInfoBase.BikeTransferDeparture || departure is StopRoutingInfoBase.CustomTransferDeparture;
            return reachedByTransferInThisRound; // TODO: check
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
            StopRoutingInfoBase.IEntry departure = GetRoutingInfo(rp).Departures[round];
            return departure is StopRoutingInfoBase.BikeTripDeparture;
        }
        /// <summary>
        /// Finds out whether the specified RoutePoint was reached by a public transit trip in the specified round
        /// </summary>
        /// <param name="rp">The RoutePoint</param>
        /// <param name="round">The round</param>
        /// <returns>Bool, specifying whether the RoutePoint was reached by a public transit trip in the round</returns>
        public bool RoutePointIsReachedByTripInRound(IRoutePoint rp, int round)
        {
            return GetRoutingInfo(rp).Departures[round] is StopRoutingInfoBase.TripDeparture;
        }
        /// <summary>
        /// Gets the routing info entry for the specified stop if it exists. If not, creates a new one, adds it to the routingInfo and returns it
        /// </summary>
        /// <param name="rp">The RoutePoint for which to get the routing info</param>
        /// <returns>The routing info of the specified RoutePoint</returns>
        private BackwardStopRoutingInfo GetRoutingInfo(IRoutePoint rp)
        {
            if (routingInfo.ContainsKey(rp))
            {
                return routingInfo[rp];
            }
            else
            {
                BackwardStopRoutingInfo stopRoutingInfo = new BackwardStopRoutingInfo();
                routingInfo.Add(rp, stopRoutingInfo);
                return stopRoutingInfo;
            }
        }
    }
}

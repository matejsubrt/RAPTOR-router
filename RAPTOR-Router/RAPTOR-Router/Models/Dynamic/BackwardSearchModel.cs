using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Interfaces;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Custom;

namespace RAPTOR_Router.Models.Dynamic
{

    /// <summary>
    /// Class representing a single connection search. One exists for every connection search problem being solved.
    /// Typically is first initiated by adding sourceStops, destinationStops and the departure time, and then is provided to a router, which uses the object as the data holding object for its connection search algorithm.
    /// </summary>
    internal class BackwardSearchModel
    {
        internal List<Stop> sourceStops { get; set; }
        internal List<Stop> destinationStops { get; set; }
        internal List<BikeStation> sourceBikeStations { get; set; }
        internal List<BikeStation> destinationBikeStations { get; set; }
        internal CustomRoutePoint? sourceCustomRoutePoint { get; set; }
        internal CustomRoutePoint? destinationCustomRoutePoint { get; set; }


        private Dictionary<IRoutePoint, BackwardStopRoutingInfo> routingInfo = new();


        private DateTime arrivalTime;
        private DateTime bestCurrentDepartureTime = DateTime.MinValue;


        private Settings settingsUsed;

        /// <summary>
        /// Creates a new SearchModel object
        /// </summary>
        /// <param name="sourceStops">The list of stops considered as the source stops - typically stops from one node sharing the same name</param>
        /// <param name="destinationStops">The list of stops considered as the destination stops - typically stops from one node sharing the same name</param>
        /// <param name="departureTime">The earliest possible departure time of the found connection</param>
        public BackwardSearchModel(List<Stop> sourceStops, List<Stop> destinationStops, List<BikeStation> sourceBikeStations, List<BikeStation> destinationBikeStations, DateTime arrivalTime, Settings settingsUsed)
        {
            this.sourceStops = sourceStops;
            this.destinationStops = destinationStops;
            this.sourceBikeStations = sourceBikeStations;
            this.destinationBikeStations = destinationBikeStations;

            this.arrivalTime = arrivalTime;
            this.settingsUsed = settingsUsed;
        }

        /// <summary>
        /// Class representing the routing information about a certain stop
        /// </summary>
        public SearchResult ExtractResult()
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
                    result.AddUsedCustomTransfer(transfer, departureTimeAtSrcCustomRP, true);
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
                        currStop = transferDeparture.Transfer.From; // TODO: SWITCH had to be used here, it is weird
                    }
                    else if (departure is StopRoutingInfoBase.BikeTransferDeparture)
                    {
                        StopRoutingInfoBase.BikeTransferDeparture bikeTransferDeparture = departure as StopRoutingInfoBase.BikeTransferDeparture;
                        result.AddUsedBikeTransfer(bikeTransferDeparture.Transfer, bikeTransferDeparture.Time, true);
                        transferUsed = true;
                        currStop = bikeTransferDeparture.Transfer.GetSrcRoutePoint();  // TODO: SWITCH had to be used here, it is weird
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
                        result.AddUsedTrip(tripToReachStop, (Stop)currStop, getOffStop, tripDeparture.Time, true);
                        currStop = getOffStop;
                    }
                    else if (departure is StopRoutingInfoBase.BikeTripDeparture)
                    {
                        StopRoutingInfoBase.BikeTripDeparture bikeTripDeparture = departure as StopRoutingInfoBase.BikeTripDeparture;
                        result.AddUsedBikeTrip(bikeTripDeparture.From, bikeTripDeparture.To, DistanceExtensions.SimplifiedDistanceBetween(bikeTripDeparture.From, bikeTripDeparture.To), true);
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
                    result.AddUsedBikeTransfer(bikeTransferDeparture.Transfer, lastDeparture.Time, true);
                }
                else if (lastDeparture is StopRoutingInfoBase.CustomTransferArrival)
                {
                    StopRoutingInfoBase.CustomTransferDeparture customTransferDeparture = lastDeparture as StopRoutingInfoBase.CustomTransferDeparture;
                    result.AddUsedCustomTransfer(customTransferDeparture.Transfer, customTransferDeparture.Time, true);
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
        /// Finds out if it is possible and fastest to reach the stop by transfer in the specified round, rather by by trip
        /// </summary>
        /// <param name="stop">The stop to use</param>
        /// <param name="round">The round in which the reaching method is to be found</param>
        /// <returns></returns>
        //public bool StopIsReachedByTransferInRound(Stop stop, int round)
        //{
        //    return GetRoutingInfo(stop).transfersToReachRounds[round] is not null;
        //}
        /// <summary>
        /// Gets the latest arrival time of the search
        /// </summary>
        /// <returns>The arrival time</returns>
        public DateTime GetArrivalTime()
        {
            return arrivalTime;
        }
        /// <summary>
        /// Gets the current best/latest departure time
        /// </summary>
        /// <returns>The best current departure time</returns>
        public DateTime GetCurrentBestDepartureTime()
        {
            return bestCurrentDepartureTime;
        }
        /// <summary>
        /// Gets the latest currently possible departure time from the specified stop to the destination stop
        /// </summary>
        /// <param name="rp">The stop to get latest departure from</param>
        /// <returns>The latest possible departure time from the stop</returns>
        public DateTime GetLatestDeparture(IRoutePoint rp)
        {
            return GetRoutingInfo(rp).LatestDeparture;
        }


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

        



        /// <summary>
        /// Sets the current overall best departure time from one of the source stops
        /// </summary>
        /// <param name="departureTime">The best departure time to set</param>
        public void SetCurrentBestDepartureTime(DateTime departureTime)
        {
            bestCurrentDepartureTime = departureTime;
        }
        /// <summary>
        /// Sets the overall best departure time from the specified stop to the destination stop
        /// </summary>
        /// <param name="rp">The stop to set the earliest arrival for</param>
        /// <param name="departureTime">The earliest arrival time to set</param>
        public void SetLatestDeparture(IRoutePoint rp, DateTime departureTime)
        {
            GetRoutingInfo(rp).LatestDeparture = departureTime;
            if (sourceStops.Contains(rp) && departureTime > bestCurrentDepartureTime)
            {
                bestCurrentDepartureTime = departureTime;
            }
        }


        public void SetTripDepartureInRound(Stop stop, Trip trip, Stop getOffStop, DateTime departureTime, int round)
        {
            StopRoutingInfoBase.TripDeparture tripArrival = new StopRoutingInfoBase.TripDeparture(trip, getOffStop, departureTime);
            GetRoutingInfo(stop).Departures[round] = tripArrival;
        }
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
            }
            else
            {
                throw new NotImplementedException();
            }

        }
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
        public void SetDestBikeStationsLatestArrival()
        {
            foreach (BikeStation destBikeStation in destinationBikeStations)
            {
                BackwardStopRoutingInfo stopRoutingInfo = GetRoutingInfo(destBikeStation);
                stopRoutingInfo.LatestDeparture = arrivalTime;
                stopRoutingInfo.Departures[0] = new StopRoutingInfoBase.ImplicitStartDeparture(arrivalTime);
            }
        }
        public bool RoutePointIsReachedByTransferInRound(IRoutePoint rp, int round)
        {
            StopRoutingInfoBase.IEntry departure = GetRoutingInfo(rp).Departures[round];
            return departure is StopRoutingInfoBase.TransferDeparture || departure is StopRoutingInfoBase.BikeTransferDeparture || departure is StopRoutingInfoBase.CustomTransferDeparture; // TODO: check
        }
        public bool RoutePointIsReachedByBikeInRound(IRoutePoint rp, int round)
        {
            StopRoutingInfoBase.IEntry departure = GetRoutingInfo(rp).Departures[round];
            return departure is StopRoutingInfoBase.BikeTripDeparture;
        }
        public bool RoutePointIsReachedByTripInRound(IRoutePoint rp, int round)
        {
            return GetRoutingInfo(rp).Departures[round] is StopRoutingInfoBase.TripDeparture;
        }
        /// <summary>
        /// Gets the routing info for the specified stop if it exists. If not, creates on, adds it to the routingInfo and returns it
        /// </summary>
        /// <param name="rp"></param>
        /// <returns></returns>
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

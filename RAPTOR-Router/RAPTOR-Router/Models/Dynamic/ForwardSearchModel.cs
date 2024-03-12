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
    internal class ForwardSearchModel
    {
        internal List<Stop> sourceStops { get; set; }
        internal List<Stop> destinationStops { get; set; }
        internal List<BikeStation> sourceBikeStations { get; set; }
        internal List<BikeStation> destinationBikeStations { get; set; }
        internal CustomRoutePoint? sourceCustomRoutePoint { get; set; }
        internal CustomRoutePoint? destinationCustomRoutePoint { get; set; }


        private Dictionary<IRoutePoint, ForwardStopRoutingInfo> routingInfo = new();


        private DateTime departureTime;
        private DateTime bestCurrentArrivalTime = DateTime.MaxValue;


        private Settings settingsUsed;

        /// <summary>
        /// Creates a new SearchModel object
        /// </summary>
        /// <param name="sourceStops">The list of stops considered as the source stops - typically stops from one node sharing the same name</param>
        /// <param name="destinationStops">The list of stops considered as the destination stops - typically stops from one node sharing the same name</param>
        /// <param name="departureTime">The earliest possible departure time of the found connection</param>
        public ForwardSearchModel(List<Stop> sourceStops, List<Stop> destinationStops, List<BikeStation> sourceBikeStations, List<BikeStation> destinationBikeStations, DateTime departureTime, Settings settingsUsed)
        {
            this.sourceStops = sourceStops;
            this.destinationStops = destinationStops;
            this.sourceBikeStations = sourceBikeStations;
            this.destinationBikeStations = destinationBikeStations;

            this.departureTime = departureTime;
            this.settingsUsed = settingsUsed;
        }

        /// <summary>
        /// Class representing the routing information about a certain stop
        /// </summary>
        

        public SearchResult ExtractResult()
        {
            // For each round, get the stop with earliest arrival
            Stop[] earliestDestStopsRounds = new Stop[Settings.ROUNDS];
            for (int round = 0; round < Settings.ROUNDS; round++)
            {
                earliestDestStopsRounds[round] = GetDestStopWithMinArrivalTimeInRound(round);
            }

            SearchResult[] resultsRounds = new SearchResult[Settings.ROUNDS];
            for (int round = 0; round < Settings.ROUNDS; round++)
            {
                resultsRounds[round] = CreateResultFromStopInRound(earliestDestStopsRounds[round], round);
            }

            return GetBestResult(resultsRounds, earliestDestStopsRounds);


            SearchResult GetBestResult(SearchResult[] results, Stop[] earliestDestStops)
            {
                int bestRound = -1;
                DateTime bestArrivalTime = DateTime.MaxValue;
                for (int round = 0; round < results.Length; round++)
                {
                    if (earliestDestStops[round] is not null)
                    {
                        var usedSegmentTypes = results[round].UsedSegmentTypes;

                        bool startsWithTransfer = usedSegmentTypes[0] == SearchResult.SegmentType.Transfer;
                        bool endsWithTransfer = usedSegmentTypes[usedSegmentTypes.Count - 1] == SearchResult.SegmentType.Transfer;

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

                        var stopInfo = routingInfo[earliestDestStops[round]];

                        DateTime adjustedArrivalTime = GetEarliestArrivalInRound(earliestDestStops[round], round).AddSeconds(transferCount * penaltySecondsPerTransfer);
                        //DateTime adjustedArrivalTime = stopInfo.earliestArrivalRounds[round].AddSeconds(transferCount * penaltySecondsPerTransfer);
                        //earliestArrivalRounds[round] = adjustedArrivalTime;

                        if (adjustedArrivalTime < bestArrivalTime)
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

            SearchResult CreateResultFromStopInRound(Stop stop, int round)
            {
                if (stop is null)
                {
                    return null;
                }
                SearchResult result = new(settingsUsed);
                ForwardStopRoutingInfo currStopInfo = routingInfo[stop];


                if(destinationCustomRoutePoint is not null)
                {
                    CustomTransfer transfer = destinationCustomRoutePoint.GetTransferWithNormalRP(stop);
                    DateTime arrivalTimeAtDestCustomRP = currStopInfo.Arrivals[round].Time.AddSeconds(transfer.GetTransferTime(settingsUsed.WalkingPace));
                    result.AddUsedCustomTransfer(transfer, arrivalTimeAtDestCustomRP, false);
                }

                IRoutePoint nextRoundStartStop = stop;
                int currRound = round;
                while (currRound > 0)
                {
                    bool transferUsed = false;


                    IRoutePoint currStop;

                    var arrival = currStopInfo.Arrivals[currRound];
                    if (arrival is StopRoutingInfoBase.TransferArrival)
                    {
                        StopRoutingInfoBase.TransferArrival transferArrival = arrival as StopRoutingInfoBase.TransferArrival;
                        result.AddUsedTransfer(transferArrival.Transfer, transferArrival.Time, false);
                        transferUsed = true;
                        currStop = transferArrival.Transfer.From;
                    }
                    else if (arrival is StopRoutingInfoBase.BikeTransferArrival)
                    {
                        StopRoutingInfoBase.BikeTransferArrival bikeTransferArrival = arrival as StopRoutingInfoBase.BikeTransferArrival;
                        result.AddUsedBikeTransfer(bikeTransferArrival.Transfer, bikeTransferArrival.Time, false);
                        transferUsed = true;
                        currStop = bikeTransferArrival.Transfer.GetSrcRoutePoint();
                    }
                    

                    // In current round, no transfer has been used, i.e. we are continuing from the exact same stop -> we add a new 0 length transfer
                    else
                    {
                        currStop = nextRoundStartStop;
                        if (currStop is Stop)
                        {
                            if (currRound != round)
                            {
                                Stop s = currStop as Stop;
                                // in last round, we do not add a transfer
                                result.AddUsedTransfer(new Transfer(s, s, 0), currStopInfo.Arrivals[currRound].Time.AddSeconds(settingsUsed.GetStationaryTransferMinimumSeconds()), false);
                            }
                        }
                    }

                    currStopInfo = routingInfo[currStop];
                    //Trip tripToReachStop = currStopInfo.tripsToReachRounds[currRound];
                    //Stop getOnStop = currStopInfo.getOnStopsToReachRounds[currRound];
                    arrival = currStopInfo.Arrivals[currRound];

                    if (arrival is StopRoutingInfoBase.TripArrival)
                    {
                        StopRoutingInfoBase.TripArrival tripArrival = arrival as StopRoutingInfoBase.TripArrival;
                        Trip tripToReachStop = tripArrival.Trip;
                        Stop getOnStop = tripArrival.GetOnStop;
                        if (tripToReachStop is null || getOnStop is null)
                        {
                            throw new ApplicationException("Trip and getOnStop cannot be null in an used round");
                        }
                        result.AddUsedTrip(tripToReachStop, getOnStop, (Stop)currStop, tripArrival.Time, false);
                        currStop = getOnStop;
                    }
                    else if (arrival is StopRoutingInfoBase.BikeTripArrival)
                    {
                        StopRoutingInfoBase.BikeTripArrival bikeTripArrival = arrival as StopRoutingInfoBase.BikeTripArrival;
                        result.AddUsedBikeTrip(bikeTripArrival.From, bikeTripArrival.To, DistanceExtensions.SimplifiedDistanceBetween(bikeTripArrival.From, bikeTripArrival.To), false);
                        currStop = bikeTripArrival.From;
                    }



                    currStopInfo = routingInfo[currStop];
                    nextRoundStartStop = currStop;
                    currRound--;
                }

                //TODO: check
                // Add the first transfer to the result -> that would be in round 0 and thus not added in the loop above
                var firstArrival = currStopInfo.Arrivals[0];
                if (firstArrival is StopRoutingInfoBase.TransferArrival)
                {
                    StopRoutingInfoBase.TransferArrival transferArrival = firstArrival as StopRoutingInfoBase.TransferArrival;
                    result.AddUsedTransfer(transferArrival.Transfer, firstArrival.Time, false);
                }
                else if (firstArrival is StopRoutingInfoBase.BikeTransferArrival)
                {
                    StopRoutingInfoBase.BikeTransferArrival bikeTransferArrival = firstArrival as StopRoutingInfoBase.BikeTransferArrival;
                    result.AddUsedBikeTransfer(bikeTransferArrival.Transfer, firstArrival.Time, false);
                }
                else if (firstArrival is StopRoutingInfoBase.CustomTransferArrival)
                {
                    StopRoutingInfoBase.CustomTransferArrival customTransferArrival = firstArrival as StopRoutingInfoBase.CustomTransferArrival;
                    result.AddUsedCustomTransfer(customTransferArrival.Transfer, customTransferArrival.Time, false);
                }

                result.SetDepartureAndArrivalTimesByEarliestDeparture(departureTime);

                return result;
            }

            Stop? GetDestStopWithMinArrivalTimeInRound(int round)
            {
                Stop? stopWithMinArrTime = null;
                DateTime earliestArrival = DateTime.MaxValue;
                foreach (Stop stop in destinationStops)
                {
                    //arrival is earlier than best we found so far AND it is better than in last round - otherwise we do not process this round
                    if (
                        routingInfo.ContainsKey(stop)
                        && GetEarliestArrivalInRound(stop, round) < earliestArrival
                        && (round == 0 || ArrivalAtStopInRoundIsBetterThanAllEarlierRounds(stop, round))
                    )
                    {
                        stopWithMinArrTime = stop;
                        earliestArrival = GetEarliestArrivalInRound(stop, round);
                    }
                }
                return stopWithMinArrTime;
            }

            bool ArrivalAtStopInRoundIsBetterThanAllEarlierRounds(Stop stop, int round)
            {
                DateTime bestEarlierArrival = DateTime.MaxValue;
                for (int i = 0; i < round; i++)
                {
                    DateTime arrivalInRoundI = GetEarliestArrivalInRound(stop, i);
                    if (arrivalInRoundI < bestEarlierArrival)
                    {
                        bestEarlierArrival = arrivalInRoundI;
                    }
                }
                return bestEarlierArrival > GetEarliestArrivalInRound(stop, round);
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
        /// Gets the earliest departure time of the search
        /// </summary>
        /// <returns>The departure time</returns>
        public DateTime GetDepartureTime()
        {
            return departureTime;
        }
        /// <summary>
        /// Gets the current best/earliest arrival time
        /// </summary>
        /// <returns>The best current arrival time</returns>
        public DateTime GetCurrentBestArrivalTime()
        {
            return bestCurrentArrivalTime;
        }
        /// <summary>
        /// Gets the earliest currently possible arrival time to the specified stop
        /// </summary>
        /// <param name="rp">The stop to get earliest arrival to</param>
        /// <returns>The earliest possible arrival time to the stop</returns>
        public DateTime GetEarliestArrival(IRoutePoint rp)
        {
            return GetRoutingInfo(rp).EarliestArrival;
        }


        public DateTime GetEarliestArrivalInRound(IRoutePoint rp, int round)
        {
            var arrival = GetRoutingInfo(rp).Arrivals[round];
            if (arrival is null)
            {
                return DateTime.MaxValue;
            }
            else
            {
                return arrival.Time;
            }
        }

        



        /// <summary>
        /// Sets the current overall best arrival time to one of the destination stops
        /// </summary>
        /// <param name="arrivalTime">The best arrival time to set</param>
        public void SetCurrentBestArrivalTime(DateTime arrivalTime)
        {
            bestCurrentArrivalTime = arrivalTime;
        }
        /// <summary>
        /// Sets the overall best arrival time to the specified stop
        /// </summary>
        /// <param name="rp">The stop to set the earliest arrival for</param>
        /// <param name="arrivalTime">The earliest arrival time to set</param>
        public void SetEarliestArrival(IRoutePoint rp, DateTime arrivalTime)
        {
            GetRoutingInfo(rp).EarliestArrival = arrivalTime;
            if (destinationStops.Contains(rp) && arrivalTime < bestCurrentArrivalTime)
            {
                bestCurrentArrivalTime = arrivalTime;
            }
        }


        public void SetTripArrivalInRound(Stop stop, Trip trip, Stop getOnStop, DateTime arrivalTime, int round)
        {
            StopRoutingInfoBase.TripArrival tripArrival = new StopRoutingInfoBase.TripArrival(trip, getOnStop, arrivalTime);
            GetRoutingInfo(stop).Arrivals[round] = tripArrival;
        }
        public void SetTransferArrivalInRound(IRoutePoint rp, ITransfer transfer, DateTime arrivalTime, int round)
        {
            if (transfer is Transfer t)
            {
                StopRoutingInfoBase.TransferArrival transferArrival = new StopRoutingInfoBase.TransferArrival(t, arrivalTime);
                GetRoutingInfo(rp).Arrivals[round] = transferArrival;
            }
            else if (transfer is BikeTransfer bt)
            {
                StopRoutingInfoBase.BikeTransferArrival bikeTransferArrival = new StopRoutingInfoBase.BikeTransferArrival(bt, arrivalTime);
                GetRoutingInfo(rp).Arrivals[round] = bikeTransferArrival;
            }
            else if(transfer is CustomTransfer ct)
            {
                StopRoutingInfoBase.CustomTransferArrival customTransferArrival = new StopRoutingInfoBase.CustomTransferArrival(ct, arrivalTime);
                GetRoutingInfo(rp).Arrivals[round] = customTransferArrival;
            }
            else
            {
                throw new NotImplementedException();
            }

        }
        //public void SetBikeTransferArrivalInRound(BikeTransfer transfer, DateTime arrivalTime, int round)
        //{
        //    StopRoutingInfo.BikeTransferArrival bikeTransferArrival = new StopRoutingInfo.BikeTransferArrival(transfer, arrivalTime);
        //    GetRoutingInfo(transfer.GetDestRoutePoint()).arrivals[round] = bikeTransferArrival;
        //}
        public void SetBikeTripArrivalInRound(BikeStation from, BikeStation to, DateTime arrivalTime, int round)
        {
            StopRoutingInfoBase.BikeTripArrival bikeTripArrival = new StopRoutingInfoBase.BikeTripArrival(from, to, arrivalTime);
            GetRoutingInfo(to).Arrivals[round] = bikeTripArrival;
        }
        /// <summary>
        /// Initiates the search by setting the earliest arrival times to all the source stops as the departure time
        /// </summary>
        public void SetSourceStopsEarliestDeparture()
        {
            foreach (Stop sourceStop in sourceStops)
            {
                ForwardStopRoutingInfo stopRoutingInfo = GetRoutingInfo(sourceStop);
                stopRoutingInfo.EarliestArrival = departureTime;
                stopRoutingInfo.Arrivals[0] = new StopRoutingInfoBase.ImplicitStartDeparture(departureTime);
            }
        }
        public void SetSourceBikeStationsEarliestDeparture()
        {
            foreach (BikeStation sourceBikeStation in sourceBikeStations)
            {
                ForwardStopRoutingInfo stopRoutingInfo = GetRoutingInfo(sourceBikeStation);
                stopRoutingInfo.EarliestArrival = departureTime;
                stopRoutingInfo.Arrivals[0] = new StopRoutingInfoBase.ImplicitStartDeparture(departureTime);
            }
        }
        public bool RoutePointIsReachedByTransferInRound(IRoutePoint rp, int round)
        {
            StopRoutingInfoBase.IEntry arrival = GetRoutingInfo(rp).Arrivals[round];
            return arrival is StopRoutingInfoBase.TransferArrival || arrival is StopRoutingInfoBase.BikeTransferArrival;
        }
        public bool RoutePointIsReachedByBikeInRound(IRoutePoint rp, int round)
        {
            var ri = GetRoutingInfo(rp);
            StopRoutingInfoBase.IEntry arrival = GetRoutingInfo(rp).Arrivals[round];
            return arrival is StopRoutingInfoBase.BikeTripArrival;
        }
        public bool RoutePointIsReachedByTripInRound(IRoutePoint rp, int round)
        {
            return GetRoutingInfo(rp).Arrivals[round] is StopRoutingInfoBase.TripArrival;
        }
        /// <summary>
        /// Gets the routing info for the specified stop if it exists. If not, creates on, adds it to the routingInfo and returns it
        /// </summary>
        /// <param name="rp"></param>
        /// <returns></returns>
        private ForwardStopRoutingInfo GetRoutingInfo(IRoutePoint rp)
        {
            if (routingInfo.ContainsKey(rp))
            {
                return routingInfo[rp];
            }
            else
            {
                ForwardStopRoutingInfo stopRoutingInfo = new ForwardStopRoutingInfo();
                routingInfo.Add(rp, stopRoutingInfo);
                return stopRoutingInfo;
            }
        }
    }
}

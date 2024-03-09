using Microsoft.AspNetCore.Routing;
using RAPTOR_Router.RAPTORStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Interfaces;

namespace RAPTOR_Router.Models
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


        private Dictionary<IRoutePoint, StopRoutingInfo> routingInfo = new();


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
        internal class StopRoutingInfo
        {
            /// <summary>
            /// The current earliest possible arrival time at the stop
            /// </summary>
            internal DateTime earliestArrival;

            internal IArrival[] arrivals;
            /// <summary>
            /// Creates a new StopRoutingInfo object with all the arrivalTimes set to the maxValue
            /// </summary>
            internal StopRoutingInfo()
            {
                earliestArrival = DateTime.MaxValue;

                arrivals = new IArrival[Settings.ROUNDS + 1];
                for (int i = 0; i < arrivals.Count(); i++)
                {
                    arrivals[i] = null;
                }
            }

            public interface IArrival
            {
                public DateTime arrivalTime { get; set; }
            }

            public class TripArrival : IArrival
            {
                internal Trip trip { get; set; }
                internal Stop getOnStop { get; set; }
                public DateTime arrivalTime { get; set; }
                internal TripArrival(Trip trip, Stop getOnStop, DateTime arrivalTime)
                {
                    this.trip = trip;
                    this.getOnStop = getOnStop;
                    this.arrivalTime = arrivalTime;
                }
                public override string ToString()
                {
                    return arrivalTime.ToShortTimeString() + ": " + trip.Route.ShortName + " from " + getOnStop.Name;
                }
            }
            public class TransferArrival : IArrival
            {
                internal Transfer transfer { get; set; }
                public DateTime arrivalTime { get; set; }
                internal TransferArrival(Transfer transfer, DateTime arrivalTime)
                {
                    this.transfer = transfer;
                    this.arrivalTime = arrivalTime;
                }
                public override string ToString()
                {
                    return arrivalTime.ToShortTimeString() + ": " + transfer.From.Name + " to " + transfer.To.Name;
                }
            }
            public class BikeTransferArrival : IArrival
            {
                internal BikeTransfer transfer { get; set; }
                public DateTime arrivalTime { get; set; }
                internal BikeTransferArrival(BikeTransfer transfer, DateTime arrivalTime)
                {
                    this.transfer = transfer;
                    this.arrivalTime = arrivalTime;
                }
                public override string ToString()
                {
                    return arrivalTime.ToShortTimeString() + ": " + transfer.GetSrcRoutePoint().Name + " to " + transfer.GetDestRoutePoint().Name;
                }
            }
            public class BikeTripArrival : IArrival
            {
                internal BikeStation from { get; set; }
                internal BikeStation to { get; set; }
                public DateTime arrivalTime { get; set; }
                internal BikeTripArrival(BikeStation from, BikeStation to, DateTime arrivalTime)
                {
                    this.from = from;
                    this.to = to;
                    this.arrivalTime = arrivalTime;
                }
                public override string ToString()
                {
                    return arrivalTime.ToShortTimeString() + ": " + from.Name + " to " + to.Name;
                }
            }
            public class ImplicitStartArrival : IArrival
            {
                public DateTime arrivalTime { get; set; }
                public ImplicitStartArrival(DateTime arrivalTime)
                {
                    this.arrivalTime = arrivalTime;
                }
            }
        }

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

                        int transferCount = (round == 0 ? round : round - 1);
                        if (startsWithTransfer)
                        {
                            transferCount++;
                        }
                        if (endsWithTransfer)
                        {
                            transferCount++;
                        }

                        var stopInfo = routingInfo[earliestDestStops[round]];

                        DateTime adjustedArrivalTime = GetEarliestArrivalInRound(earliestDestStops[round], round);
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
                StopRoutingInfo currStopInfo = routingInfo[stop];


                IRoutePoint nextRoundStartStop = stop;
                int currRound = round;
                while (currRound > 0)
                {
                    bool transferUsed = false;

                    
                    IRoutePoint currStop;

                    var arrival = currStopInfo.arrivals[currRound];
                    if(arrival is StopRoutingInfo.TransferArrival)
                    {
                        StopRoutingInfo.TransferArrival transferArrival = arrival as StopRoutingInfo.TransferArrival;
                        result.AddUsedTransfer(transferArrival.transfer, transferArrival.arrivalTime);
                        transferUsed = true;
                        currStop = transferArrival.transfer.From;
                    }
                    else if(arrival is StopRoutingInfo.BikeTransferArrival)
                    {
                        StopRoutingInfo.BikeTransferArrival bikeTransferArrival = arrival as StopRoutingInfo.BikeTransferArrival;
                        result.AddUsedBikeTransfer(bikeTransferArrival.transfer, bikeTransferArrival.arrivalTime);
                        transferUsed = true;
                        currStop = bikeTransferArrival.transfer.GetSrcRoutePoint();
                    }

                    // In current round, no transfer has been used, i.e. we are continuing from the exact same stop -> we add a new 0 length transfer
                    else
                    {
                        currStop = nextRoundStartStop;
                        if(currStop is Stop)
                        {
                            if (currRound != round)
                            {
                                Stop s = currStop as Stop;
                                // in last round, we do not add a transfer
                                result.AddUsedTransfer(new Transfer(s, s, 0), currStopInfo.arrivals[currRound].arrivalTime.AddSeconds(settingsUsed.GetStationaryTransferMinimumSeconds()));
                            }
                        }                        
                    }

                    currStopInfo = routingInfo[currStop];
                    //Trip tripToReachStop = currStopInfo.tripsToReachRounds[currRound];
                    //Stop getOnStop = currStopInfo.getOnStopsToReachRounds[currRound];
                    arrival = currStopInfo.arrivals[currRound];
                    
                    if(arrival is StopRoutingInfo.TripArrival)
                    {
                        StopRoutingInfo.TripArrival tripArrival = arrival as StopRoutingInfo.TripArrival;
                        Trip tripToReachStop = tripArrival.trip;
                        Stop getOnStop = tripArrival.getOnStop;
                        if (tripToReachStop is null || getOnStop is null)
                        {
                            throw new ApplicationException("Trip and getOnStop cannot be null in an used round");
                        }
                        result.AddUsedTrip(tripToReachStop, getOnStop, (Stop)currStop, tripArrival.arrivalTime);
                        currStop = getOnStop;
                    }
                    else if(arrival is StopRoutingInfo.BikeTripArrival)
                    {
                        StopRoutingInfo.BikeTripArrival bikeTripArrival = arrival as StopRoutingInfo.BikeTripArrival;
                        result.AddUsedBikeTrip(bikeTripArrival.from, bikeTripArrival.to, DistanceExtensions.SimplifiedDistanceBetween(bikeTripArrival.from, bikeTripArrival.to));
                        currStop = bikeTripArrival.from;
                    }
                    

                    
                    currStopInfo = routingInfo[currStop];
                    nextRoundStartStop = currStop;
                    currRound--;
                }

                //TODO: check
                // Add the first transfer to the result -> that would be in round 0 and thus not added in the loop above
                var firstArrival = currStopInfo.arrivals[0];
                if (firstArrival is StopRoutingInfo.TransferArrival)
                {
                    StopRoutingInfo.TransferArrival transferArrival = firstArrival as StopRoutingInfo.TransferArrival;
                    result.AddUsedTransfer(transferArrival.transfer, firstArrival.arrivalTime);
                }
                else if(firstArrival is StopRoutingInfo.BikeTransferArrival)
                {
                    StopRoutingInfo.BikeTransferArrival bikeTransferArrival = firstArrival as StopRoutingInfo.BikeTransferArrival;
                    result.AddUsedBikeTransfer(bikeTransferArrival.transfer, firstArrival.arrivalTime);
                }

                result.SetDepartureAndArrivalTimes(departureTime);

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
            return GetRoutingInfo(rp).earliestArrival;
        }


        public DateTime GetEarliestArrivalInRound(IRoutePoint rp, int round)
        {
            var arrival = GetRoutingInfo(rp).arrivals[round];
            if(arrival is null)
            {
                return DateTime.MaxValue;
            }
            else
            {
                return arrival.arrivalTime;
            }
        }

        public bool ArrivalInRoundIsByTrip(IRoutePoint rp, int round)
        {
            return GetRoutingInfo(rp).arrivals[round] is StopRoutingInfo.TripArrival;
        }



        /// <summary>
        /// Sets the current overall best arrival time to one of the destination stops
        /// </summary>
        /// <param name="arrivalTime">The best arrival time to set</param>
        public void SetCurrentBestArrivalTime(DateTime arrivalTime)
        {
            this.bestCurrentArrivalTime = arrivalTime;
        }
        /// <summary>
        /// Sets the overall best arrival time to the specified stop
        /// </summary>
        /// <param name="rp">The stop to set the earliest arrival for</param>
        /// <param name="arrivalTime">The earliest arrival time to set</param>
        public void SetEarliestArrival(IRoutePoint rp, DateTime arrivalTime)
        {
            GetRoutingInfo(rp).earliestArrival = arrivalTime;
            if (destinationStops.Contains(rp) && arrivalTime < bestCurrentArrivalTime)
            {
                bestCurrentArrivalTime = arrivalTime;
            }
        }


        public void SetTripArrivalInRound(Stop stop, Trip trip, Stop getOnStop, DateTime arrivalTime, int round)
        {
            StopRoutingInfo.TripArrival tripArrival = new StopRoutingInfo.TripArrival(trip, getOnStop, arrivalTime);
            GetRoutingInfo(stop).arrivals[round] = tripArrival;
        }
        public void SetTransferArrivalInRound(IRoutePoint rp, ITransfer transfer, DateTime arrivalTime, int round)
        {
            if(transfer is Transfer)
            {
                Transfer t = transfer as Transfer;
                StopRoutingInfo.TransferArrival transferArrival = new StopRoutingInfo.TransferArrival(t, arrivalTime);
                GetRoutingInfo(rp).arrivals[round] = transferArrival;
            }
            else if(transfer is BikeTransfer)
            {
                BikeTransfer bt = transfer as BikeTransfer;
                StopRoutingInfo.BikeTransferArrival bikeTransferArrival = new StopRoutingInfo.BikeTransferArrival(bt, arrivalTime);
                GetRoutingInfo(rp).arrivals[round] = bikeTransferArrival;
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
            StopRoutingInfo.BikeTripArrival bikeTripArrival = new StopRoutingInfo.BikeTripArrival(from, to, arrivalTime);
            GetRoutingInfo(to).arrivals[round] = bikeTripArrival;
        }
        /// <summary>
        /// Initiates the search by setting the earliest arrival times to all the source stops as the departure time
        /// </summary>
        public void SetSourceStopsEarliestArrival()
        {
            foreach (Stop sourceStop in sourceStops)
            {
                StopRoutingInfo stopRoutingInfo = GetRoutingInfo(sourceStop);
                stopRoutingInfo.earliestArrival = departureTime;
                stopRoutingInfo.arrivals[0] = new StopRoutingInfo.ImplicitStartArrival(departureTime);
            }
        }
        public void SetSourceBikeStationsEarliestArrival()
        {
            foreach(BikeStation sourceBikeStation in sourceBikeStations)
            {
                StopRoutingInfo stopRoutingInfo = GetRoutingInfo(sourceBikeStation);
                stopRoutingInfo.earliestArrival = departureTime;
                stopRoutingInfo.arrivals[0] = new StopRoutingInfo.ImplicitStartArrival(departureTime);
            }
        }
        public bool RoutePointIsReachedByTransferInRound(IRoutePoint rp, int round)
        {
            StopRoutingInfo.IArrival arrival = GetRoutingInfo(rp).arrivals[round];
            return arrival is StopRoutingInfo.TransferArrival || arrival is StopRoutingInfo.BikeTransferArrival;
        }
        public bool RoutePointIsReachedByBikeInRound(IRoutePoint rp, int round)
        {
            var ri = GetRoutingInfo(rp);
            StopRoutingInfo.IArrival arrival = GetRoutingInfo(rp).arrivals[round];
            return arrival is StopRoutingInfo.BikeTripArrival;
        }
        /// <summary>
        /// Gets the routing info for the specified stop if it exists. If not, creates on, adds it to the routingInfo and returns it
        /// </summary>
        /// <param name="rp"></param>
        /// <returns></returns>
        private StopRoutingInfo GetRoutingInfo(IRoutePoint rp)
        {
            if (routingInfo.ContainsKey(rp))
            {
                return routingInfo[rp];
            }
            else
            {
                StopRoutingInfo stopRoutingInfo = new StopRoutingInfo();
                routingInfo.Add(rp, stopRoutingInfo);
                return stopRoutingInfo;
            }
        }
    }
}

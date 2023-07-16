using RAPTOR_Router.RAPTORStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Problems
{
    internal class JourneySearchModel
    {
        private RAPTORModel model;
        internal List<Stop> sourceStops { get; set; }
        internal List<Stop> destinationStops { get; set; }
        private Dictionary<Stop, StopRoutingInfo> routingInfo = new();
        private DateTime departureTime;
        private DateTime bestCurrentArrivalTime = DateTime.MaxValue;

        public JourneySearchModel(RAPTORModel model, List<Stop> sourceStops, List<Stop> destinationStops, DateTime departureTime)
        {
            this.model = model;
            this.sourceStops = sourceStops;
            this.destinationStops = destinationStops;
            this.departureTime = departureTime;
        }

        internal class StopRoutingInfo
        {
            internal DateTime earliestArrival;
            internal DateTime[] earliestArrivalRounds;
            //TODO: Add multiple trip/stop/transfer support
            //TODO: Convert to arrays
            internal Trip[] tripsToReachRounds;
            internal Stop[] getOnStopsToReachRounds;
            internal Transfer[] transfersToReachRounds;

            internal StopRoutingInfo()
            {
                earliestArrival = DateTime.MaxValue;
                earliestArrivalRounds = new DateTime[Settings.ROUNDS + 1];
                Array.Fill(earliestArrivalRounds, DateTime.MaxValue);

                tripsToReachRounds = new Trip[Settings.ROUNDS + 1];
                getOnStopsToReachRounds = new Stop[Settings.ROUNDS + 1];
                transfersToReachRounds = new Transfer[Settings.ROUNDS + 1];
            }
        }
        //TODO: Delete following 3
        public Dictionary<Stop, StopRoutingInfo> GetRoutingInfo()
        {
            return routingInfo;
        }
        public bool StopIsReachedByTransferInRound(Stop stop, int round)
        {
            return GetRoutingInfo(stop).transfersToReachRounds[round] is not null;
        }
        public void SetCurrentBestArrivalTime(DateTime arrivalTime)
        {
            this.bestCurrentArrivalTime = arrivalTime;
        }
        public void SetTransferToReachInRound(Stop stop, int round, Transfer transfer)
        {
            GetRoutingInfo(stop).transfersToReachRounds[round] = transfer;
        }
        public void SetEarliestArrivalInRound(Stop stop, int round, DateTime arrivalTime)
        {
            GetRoutingInfo(stop).earliestArrivalRounds[round] = arrivalTime;
        }
        public void SetEarliestArrival(Stop stop, DateTime arrivalTime)
        {
            GetRoutingInfo(stop).earliestArrival = arrivalTime;
            if (destinationStops.Contains(stop) && arrivalTime < bestCurrentArrivalTime)
            {
                bestCurrentArrivalTime = arrivalTime;
            }
        }
        public void SetTripToReachInRound(Stop stop, int round, Trip trip)
        {
            GetRoutingInfo(stop).tripsToReachRounds[round] = trip;
        }
        public void SetGetOnStopToReachInRound(Stop stop, int round, Stop getOnStop)
        {
            GetRoutingInfo(stop).getOnStopsToReachRounds[round] = getOnStop;
        }
        public DateTime GetDepartureTime()
        {
            return departureTime;
        }
        public DateTime GetCurrentBestArrivalTime()
        {
            return bestCurrentArrivalTime;
        }
        public DateTime GetEarliestArrival(Stop stop)
        {
            return GetRoutingInfo(stop).earliestArrival;
        }
        public DateTime GetEarliestArrivalInRound(Stop stop, int round)
        {
            return GetRoutingInfo(stop).earliestArrivalRounds[round];
        }

        public void SetSourceStopsEarliestArrival()
        {
            foreach(Stop sourceStop in sourceStops)
            {
                StopRoutingInfo stopRoutingInfo = GetRoutingInfo(sourceStop);
                stopRoutingInfo.earliestArrival = departureTime;
                stopRoutingInfo.earliestArrivalRounds[0] = departureTime;
            }
        }
        private StopRoutingInfo GetRoutingInfo(Stop stop)
        {
            if (routingInfo.ContainsKey(stop))
            {
                return routingInfo[stop];
            }
            else
            {
                StopRoutingInfo stopRoutingInfo = new StopRoutingInfo();
                routingInfo.Add(stop, stopRoutingInfo);
                return stopRoutingInfo;
            }
        }
    }
}

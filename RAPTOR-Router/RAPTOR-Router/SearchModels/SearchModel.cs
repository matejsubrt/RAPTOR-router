using RAPTOR_Router.RAPTORStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.SearchModels
{
    internal class SearchModel
    {
        internal List<Stop> sourceStops { get; set; }
        internal List<Stop> destinationStops { get; set; }
        private Dictionary<Stop, StopRoutingInfo> routingInfo = new();
        private DateTime departureTime;
        private DateTime bestCurrentArrivalTime = DateTime.MaxValue;

        public SearchModel(List<Stop> sourceStops, List<Stop> destinationStops, DateTime departureTime)
        {
            this.sourceStops = sourceStops;
            this.destinationStops = destinationStops;
            this.departureTime = departureTime;
        }

        internal class StopRoutingInfo
        {
            internal DateTime earliestArrival;
            internal DateTime[] earliestArrivalRounds;
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
        public SearchResult ExtractResult()
        {
            Stop? earliestDestStop = GetDestStopWithMinArrivalTime();
            if(earliestDestStop is null)
            {
                return null;
            }


            int round = GetFirstEarliestRound();
            List<Trip> usedTrips = new();
            List<Transfer> usedTransfers = new();
            Dictionary<Trip, Stop> getOnStops = new();
            Dictionary<Trip, Stop> getOffStops = new();
            Stop sourceStop;
            ExtractUsedTripsAndTransfers();

            SearchResult result = new SearchResult
            (
                usedTrips,
                usedTransfers,
                getOnStops,
                getOffStops,
                sourceStop,
                earliestDestStop
            );

            return result;

            void ExtractUsedTripsAndTransfers()
            {
                Stop currStop = earliestDestStop;
                Trip lastTrip = null;
                while(!sourceStops.Contains(currStop) && round >= 0)
                {
                    Trip usedTrip = routingInfo[currStop].tripsToReachRounds[round];
                    Transfer usedTransfer = routingInfo[currStop].transfersToReachRounds[round];

                    //lastTrip ended in the same stop as currTrip begins - i.e. no transfer
                    if (lastTrip != null && usedTrip != null)
                    {
                        usedTransfers.Add(new Transfer(currStop, currStop, 0));
                    }
                    //currently processing a trip
                    if (usedTrip != null)
                    {
                        usedTrips.Add(usedTrip);
                        getOnStops.Add(usedTrip, routingInfo[currStop].getOnStopsToReachRounds[round]);
                        getOffStops.Add(usedTrip, currStop);
                        currStop = routingInfo[currStop].getOnStopsToReachRounds[round];
                        round--;
                    }
                    //currently processing a transfer
                    else if (usedTransfer != null)
                    {
                        usedTransfers.Add(usedTransfer);
                        currStop = usedTransfer.From;
                    }
                    lastTrip = usedTrip;
                }
                sourceStop = currStop;
                usedTransfers.Reverse();
                usedTrips.Reverse();
            }
            int GetFirstEarliestRound()
            {
                for(int i = 0; i<=Settings.ROUNDS; i++)
                {
                    var info = routingInfo[earliestDestStop];
                    if (info.earliestArrivalRounds[i] == info.earliestArrival)
                    {
                        return i;
                    }
                }
                return -1;
            }
            Stop? GetDestStopWithMinArrivalTime()
            {
                Stop? stopWithMinArrTime = null;
                DateTime earliestArrival = DateTime.MaxValue;
                foreach (Stop stop in destinationStops)
                {
                    if (routingInfo.ContainsKey(stop) && routingInfo[stop].earliestArrival < earliestArrival)
                    {
                        stopWithMinArrTime = stop;
                        earliestArrival = routingInfo[stop].earliestArrival;
                    }
                }
                return stopWithMinArrTime;
            }
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

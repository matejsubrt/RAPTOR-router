using RAPTOR_Router.RAPTORStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using static RAPTOR_Router.Routers.BasicRouter;

namespace RAPTOR_Router.Routers
{
    internal class BasicRouterNew : IRouter
    {
        private static int ROUNDS = 5;
        private static int MAX_LENGTH_DAYS = 1;
        private RAPTORModel model;
        private Stop fromStop;
        private Stop toStop;
        private List<Stop> fromStops = new();
        private List<Stop> toStops = new();
        private DateTime earliestArrival = DateTime.MaxValue;
        private Dictionary<Stop, StopRoutingInfo> stopRoutingInfo = new();
        private HashSet<Stop> markedStops = new();
        private HashSet<Route> markedRoutes = new();
        private Dictionary<Route, Stop> routeGetOnStops = new();

        public class StopRoutingInfo
        {
            public DateTime earliestArrival;
            public List<DateTime> earliestArrivalRounds;
            public List<Trip> tripsToReach;
            public List<Transfer> transfersToReach;
            public List<Stop> stopsToReach;

            public DateTime[] earliestArrivalRoundsArr;
            public Trip tripToReach;
            public Stop getOnStopToReach;
            public Transfer transferToReach;
            public StopRoutingInfo()
            {
                this.earliestArrival = DateTime.MaxValue;
                this.earliestArrivalRounds = Enumerable.Repeat(DateTime.MaxValue, ROUNDS + 1).ToList();
                this.tripsToReach = new List<Trip>();
                this.transfersToReach = new List<Transfer>();
                this.stopsToReach = new List<Stop>();
            }
        }

        public BasicRouterNew(RAPTORModel model)
        {
            this.model = model;
        }

        public SearchResult FindConnection(List<string> fromStopIds, List<string> toStopIds, DateTime departureTime)
        {
            //this.fromStops = model.stops[fromStopId];
            //this.toStops = model.stops[toStopId];
            foreach(string id in fromStopIds)
            {
                fromStops.Add(model.stops[id]);
            }
            foreach (string id in toStopIds)
            {
                toStops.Add(model.stops[id]);
            }

            foreach (Stop stop in model.stops.Values)
            {
                stopRoutingInfo.Add(stop, new StopRoutingInfo());
            }
            /*
            stopRoutingInfo[fromStop].earliestArrival = departureTime;
            stopRoutingInfo[fromStop].earliestArrivalRounds[0] = departureTime;
            markedStops.Add(fromStop);
            */

            foreach(Stop stop in fromStops)
            {
                stopRoutingInfo[stop].earliestArrival = departureTime;
                stopRoutingInfo[stop].earliestArrivalRounds[0] = departureTime;
                markedStops.Add(stop);
            }

            for(int round = 1; round < ROUNDS; round++)
            {
                //Accumulate routes serving marked stops from previous round
                routeGetOnStops = new();
                foreach (Stop markedStop in markedStops)
                {
                    foreach (Route route in markedStop.StopRoutes)
                    {
                        if (routeGetOnStops.ContainsKey(route))
                        {
                            if (route.GetStopIndex(routeGetOnStops[route]) > route.GetStopIndex(markedStop))
                            {
                                routeGetOnStops[route] = markedStop;
                            }
                        }
                        else
                        {
                            //markedRoutes.Add(route);
                            routeGetOnStops.Add(route, markedStop);
                        }
                    }
                    //?
                    markedStops.Remove(markedStop);
                }

                //Traverse each route
                foreach (KeyValuePair<Route, Stop> keyValuePair in routeGetOnStops)
                {
                    Route route = keyValuePair.Key;
                    Stop getOnStop = keyValuePair.Value;
                    DateOnly tripDate;
                    Trip trip = route.GetEarliestTripAtStop(
                        getOnStop, 
                        DateOnly.FromDateTime(stopRoutingInfo[getOnStop].earliestArrivalRounds[round - 1]), 
                        TimeOnly.FromDateTime(stopRoutingInfo[getOnStop].earliestArrivalRounds[round - 1]), 
                        MAX_LENGTH_DAYS,
                        out tripDate
                    ); //The current trip

                    for (int i = route.GetStopIndex(getOnStop); i < route.RouteStops.Count; i++)
                    {
                        Stop currStop = route.RouteStops[i];
                        //TODO: bacha na pulnoc

                        //Can the label be improved in this round? Includes local and target pruning
                        if (trip is not null)
                        {
                            DateOnly realDate;
                            if (trip.StopTimes[route.GetStopIndex(getOnStop)].DepartureTime > trip.StopTimes[i].ArrivalTime)
                            {
                                realDate = tripDate.AddDays(1);
                            }
                            else
                            {
                                realDate = tripDate;
                            }
                            DateTime arrivalTime = new DateTime(realDate.Year, realDate.Month, realDate.Day, trip.StopTimes[i].ArrivalTime.Hour, trip.StopTimes[i].ArrivalTime.Minute, trip.StopTimes[i].ArrivalTime.Second);
                            DateTime departureTime1 = new DateTime(realDate.Year, realDate.Month, realDate.Day, trip.StopTimes[i].DepartureTime.Hour, trip.StopTimes[i].DepartureTime.Minute, trip.StopTimes[i].DepartureTime.Second);
                            if (arrivalTime < stopRoutingInfo[currStop].earliestArrival && arrivalTime < earliestArrival && arrivalTime <= departureTime.AddDays(1))
                            {
                                stopRoutingInfo[currStop].earliestArrivalRounds[round] = arrivalTime;
                                stopRoutingInfo[currStop].earliestArrival = arrivalTime;
                                //new
                                stopRoutingInfo[currStop].tripToReach = trip;
                                stopRoutingInfo[currStop].getOnStopToReach = getOnStop;
                                stopRoutingInfo[currStop].transferToReach = null;
                                
                                if(toStops.Contains(currStop) && arrivalTime < earliestArrival)
                                {
                                    earliestArrival = arrivalTime;
                                }
                                markedStops.Add(currStop);
                                
                            }
                            //Can we catch an earlier trip at pi
                            if (stopRoutingInfo[currStop].earliestArrivalRounds[round - 1] <= departureTime1)
                            {
                                trip = route.GetEarliestTripAtStop(currStop, DateOnly.FromDateTime(stopRoutingInfo[currStop].earliestArrivalRounds[round - 1]), TimeOnly.FromDateTime(stopRoutingInfo[currStop].earliestArrivalRounds[round - 1]), MAX_LENGTH_DAYS, out tripDate);
                                //new
                                getOnStop = currStop;
                            }
                        }
                    }
                }

                HashSet<Stop> newMarkedStops = new();
                foreach (Stop markedStop in markedStops)
                {
                    foreach (Transfer transfer in markedStop.Transfers)
                    {
                        if (stopRoutingInfo[transfer.To].earliestArrivalRounds[round] > stopRoutingInfo[markedStop].earliestArrivalRounds[round].AddSeconds(transfer.Time))
                        {
                            stopRoutingInfo[transfer.To].earliestArrivalRounds[round] = stopRoutingInfo[markedStop].earliestArrivalRounds[round].AddSeconds(transfer.Time);
                            if (stopRoutingInfo[transfer.To].earliestArrival > stopRoutingInfo[transfer.To].earliestArrivalRounds[round])
                            {
                                stopRoutingInfo[transfer.To].earliestArrival = stopRoutingInfo[transfer.To].earliestArrivalRounds[round];

                                stopRoutingInfo[transfer.To].tripToReach = null;
                                stopRoutingInfo[transfer.To].getOnStopToReach = null;
                                stopRoutingInfo[transfer.To].transferToReach = transfer;
                            }
                            newMarkedStops.Add(transfer.To);
                        }
                    }
                }
                markedStops.UnionWith(newMarkedStops);
                newMarkedStops = new();
            }
            return new SearchResult(stopRoutingInfo, toStops, fromStops);
        }
    }
}

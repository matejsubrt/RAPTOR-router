using RAPTOR_Router.RAPTORStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Routers
{
    internal class BasicRouter : IRouter
    {
        private static int ROUNDS = 5;
        private RAPTORModel model;
        private Stop fromStop;
        private Stop toStop;
        private DateTime departureTime;
        private DateTime bestCurrentArrivalTime = DateTime.MaxValue;
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
                this.earliestArrivalRounds = Enumerable.Repeat(DateTime.MaxValue, ROUNDS+1).ToList();
                this.tripsToReach = new List<Trip>();
                this.transfersToReach = new List<Transfer>();
                this.stopsToReach = new List<Stop>();
            }
        }
        private class StopRoutePair
        {
            public Stop stop;
            public Route route;

            public StopRoutePair(Stop stop, Route route)
            {
                this.stop = stop;
                this.route = route;
            }
        }
        public BasicRouter(RAPTORModel model)
        {
            this.model = model;
        }
        public SearchResult FindConnection(string fromStopId, string toStopId, DateTime departureTime)
        {
            this.fromStop = model.stops[fromStopId];
            this.toStop = model.stops[toStopId];
            this.departureTime = departureTime;

            //Initialization of the algorithm
            foreach(Stop stop in model.stops.Values)
            {
                stopRoutingInfo.Add(stop, new StopRoutingInfo());
            }
            stopRoutingInfo[fromStop].earliestArrival = departureTime;
            stopRoutingInfo[fromStop].earliestArrivalRounds[0] = departureTime;
            markedStops.Add(fromStop);

            for(int round = 1; round <= ROUNDS; round++ )
            {
                //Accumulate routes serving marked stops from previous round
                //markedRoutes = new();
                routeGetOnStops = new();
                foreach(Stop markedStop in markedStops)
                {
                    foreach(Route route in markedStop.StopRoutes)
                    {
                        if(routeGetOnStops.ContainsKey(route))
                        {
                            if(route.GetStopIndex(routeGetOnStops[route]) > route.GetStopIndex(markedStop))
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
                foreach(KeyValuePair<Route, Stop> keyValuePair in routeGetOnStops)
                {
                    Route route = keyValuePair.Key;
                    Stop getOnStop = keyValuePair.Value;
                    Trip t = route.GetEarliestTripAtStop(getOnStop, stopRoutingInfo[getOnStop].earliestArrivalRounds[round - 1]); ; //The current trip
                    for(int i = route.GetStopIndex(getOnStop); i < route.RouteStops.Count; i++)
                    {
                        Stop currStop = route.RouteStops[i];
                        //TODO: bacha na pulnoc
                        
                        //Can the label be improved in this round? Includes local and target pruning
                        if (t is not null)
                        {
                            DateOnly realDate;
                            if (t.StopTimes[0].DepartureTime > t.StopTimes[i].ArrivalTime)
                            {
                                realDate = t.Date.AddDays(1);
                            }
                            else
                            {
                                realDate = t.Date;
                            }
                            DateTime arrivalTime = new DateTime(realDate.Year, realDate.Month, realDate.Day, t.StopTimes[i].ArrivalTime.Hour, t.StopTimes[i].ArrivalTime.Minute, t.StopTimes[i].ArrivalTime.Second);
                            DateTime departureTime1 = new DateTime(realDate.Year, realDate.Month, realDate.Day, t.StopTimes[i].DepartureTime.Hour, t.StopTimes[i].DepartureTime.Minute, t.StopTimes[i].DepartureTime.Second);
                            if (arrivalTime < stopRoutingInfo[currStop].earliestArrival && arrivalTime < stopRoutingInfo[toStop].earliestArrival && arrivalTime <= departureTime.AddDays(1))
                            {
                                stopRoutingInfo[currStop].earliestArrivalRounds[round] = arrivalTime;
                                stopRoutingInfo[currStop].earliestArrival = arrivalTime;
                                //new
                                stopRoutingInfo[currStop].tripToReach = t;
                                stopRoutingInfo[currStop].getOnStopToReach = getOnStop;
                                stopRoutingInfo[currStop].transferToReach = null;

                                markedStops.Add(currStop);
                            }
                            //Can we catch an earlier trip at pi
                            if (stopRoutingInfo[currStop].earliestArrivalRounds[round - 1] <= departureTime1)
                            {
                                t = route.GetEarliestTripAtStop(currStop, stopRoutingInfo[currStop].earliestArrivalRounds[round - 1]);
                                //new
                                getOnStop = currStop;
                            }
                        }                       
                        
                    }
                }

                //Look at foot-paths
                HashSet<Stop> newMarkedStops = new();
                foreach(Stop markedStop in markedStops)
                {
                    foreach(Transfer transfer in markedStop.Transfers)
                    {
                        if(stopRoutingInfo[transfer.To].earliestArrivalRounds[round] > stopRoutingInfo[markedStop].earliestArrivalRounds[round].AddSeconds(transfer.Time))
                        {
                            stopRoutingInfo[transfer.To].earliestArrivalRounds[round] = stopRoutingInfo[markedStop].earliestArrivalRounds[round].AddSeconds(transfer.Time);
                            if(stopRoutingInfo[transfer.To].earliestArrival > stopRoutingInfo[transfer.To].earliestArrivalRounds[round])
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
            return new SearchResult(stopRoutingInfo, toStop, fromStop);
        }
    }
}

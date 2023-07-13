using RAPTOR_Router.Routers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
    internal class SearchResult
    {
        List<Trip> UsedTrips { get; set; }
        List<Transfer> usedTransfers { get; set; }
        Dictionary<Trip, Stop> GetOnStops { get; set; } = new();
        Stop toStop;
        public SearchResult(Dictionary<Stop, BasicRouterNew.StopRoutingInfo> stopRoutingInfo, List<Stop> toStops, List<Stop> fromStops)
        {

            LinkedList<Trip> usedTrips = new LinkedList<Trip>();
            LinkedList<Transfer> usedTransfers = new LinkedList<Transfer>();
            toStop = StopWithMinArrivalTime(toStops, stopRoutingInfo);
            Stop currStop = toStop;

            while(!fromStops.Contains(currStop))
            {
                Trip usedTrip = stopRoutingInfo[currStop].tripToReach;
                Transfer usedTransfer = stopRoutingInfo[currStop].transferToReach;

                if(usedTrip != null)
                {
                    usedTrips.AddFirst(usedTrip);
                    this.GetOnStops.Add(usedTrip, stopRoutingInfo[currStop].getOnStopToReach);
                    currStop = stopRoutingInfo[currStop].getOnStopToReach;
                }
                else if(usedTransfer != null)
                {
                    usedTransfers.AddFirst(usedTransfer);
                    currStop = usedTransfer.From;
                }
            }

            this.UsedTrips = usedTrips.ToList();
            this.usedTransfers = usedTransfers.ToList();

        }
        private Stop StopWithMinArrivalTime(List<Stop> toStops, Dictionary<Stop, BasicRouterNew.StopRoutingInfo> routingInfo)
        {
            Stop stopWithMinArrTime = toStops[0];
            foreach(Stop stop in toStops)
            {
                if (routingInfo[stop].earliestArrival < routingInfo[stopWithMinArrTime].earliestArrival)
                {
                    stopWithMinArrTime = stop;
                }
            }
            return stopWithMinArrTime;
        }
        public string ToString()
        {
            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("Get On:");
            //sb.AppendLine("\t" + GetOnStops[UsedTrips[0]]);

            for(int i = 0; i<UsedTrips.Count; i++)
            {
                Trip trip = UsedTrips[i];
                sb.AppendLine("Trip:");
                if(i < UsedTrips.Count - 1)
                {
                    //sb.AppendLine("\tRoute " + trip.Route.ShortName + " from " + GetOnStops[trip].ToString() + " to " + usedTransfers[i].From.ToString());
                    sb.AppendLine("\tRoute " + trip.Route.ShortName);
                    sb.AppendLine("\t\tFrom " + GetOnStops[trip].ToString() + " at " + trip.StopTimes[trip.Route.GetStopIndex(GetOnStops[trip])].DepartureTime.ToLongTimeString());
                    sb.AppendLine("\t\tTo: " + usedTransfers[i].From.ToString() + " at " + trip.StopTimes[trip.Route.GetStopIndex(usedTransfers[i].From)].ArrivalTime.ToLongTimeString());
                    sb.AppendLine("\tTransfer " + usedTransfers[i].Time + " s");
                }
                else if(i == UsedTrips.Count-1)
                {
                    //sb.AppendLine("\tRoute " + trip.Route.ShortName + " from " + GetOnStops[trip].ToString() + " to " + toStop.ToString());
                    sb.AppendLine("\tRoute " + trip.Route.ShortName);
                    sb.AppendLine("\t\tFrom " + GetOnStops[trip].ToString() + " at " + trip.StopTimes[trip.Route.GetStopIndex(GetOnStops[trip])].DepartureTime.ToLongTimeString());
                    if(UsedTrips.Count == usedTransfers.Count)
                    {
                        sb.AppendLine("\t\tTo: " + usedTransfers[i].From.ToString() + " at " + trip.StopTimes[trip.Route.GetStopIndex(usedTransfers[i].From)].ArrivalTime.ToLongTimeString());

                        sb.AppendLine("\tTransfer " + usedTransfers[i].Time + " s");
                        sb.AppendLine("\tArrived at " + toStop.ToString());
                    }
                    else
                    {
                        sb.AppendLine("\t\tTo: " + toStop.ToString() + " at " + trip.StopTimes[trip.Route.GetStopIndex(toStop)].ArrivalTime.ToLongTimeString());
                    }
                }
            }
            return sb.ToString();
        }
    }
}

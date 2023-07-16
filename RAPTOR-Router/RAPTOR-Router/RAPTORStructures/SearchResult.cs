using RAPTOR_Router.Problems;
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
        List<Transfer> UsedTransfers { get; set; }
        Dictionary<Trip, Stop> GetOnStops { get; set; } = new();
        Stop toStop;

        public SearchResult(JourneySearchModel searchModel)
        {
            Dictionary<Stop, JourneySearchModel.StopRoutingInfo> routingInfo = searchModel.GetRoutingInfo();
            List<Stop> fromStops = searchModel.sourceStops;
            List<Stop> toStops = searchModel.destinationStops;
            LinkedList<Trip> usedTrips = new LinkedList<Trip>();
            LinkedList<Transfer> usedTransfers = new LinkedList<Transfer>();
            toStop = StopWithMinArrivalTime(toStops, routingInfo);
            int round = int.MaxValue;
            for (int i = 0; i < Settings.ROUNDS; i++)
            {
                var info = routingInfo[toStop];
                if (routingInfo[toStop].earliestArrivalRounds[i] == routingInfo[toStop].earliestArrival)
                {
                    round = i;
                    break;
                }
            }

            Stop currStop = toStop;

            Trip lastTrip = null;
            while (!fromStops.Contains(currStop) && round >= 0)
            {
                Trip usedTrip = routingInfo[currStop].tripsToReachRounds[round];
                Transfer usedTransfer = routingInfo[currStop].transfersToReachRounds[round];

                if (lastTrip != null && usedTrip != null)
                {
                    usedTransfers.AddFirst(new Transfer(currStop, currStop, 0));
                }
                if (usedTrip != null)
                {
                    usedTrips.AddFirst(usedTrip);
                    this.GetOnStops.Add(usedTrip, routingInfo[currStop].getOnStopsToReachRounds[round]);
                    currStop = routingInfo[currStop].getOnStopsToReachRounds[round];
                    round--;
                }
                else if (usedTransfer != null)
                {
                    usedTransfers.AddFirst(usedTransfer);
                    currStop = usedTransfer.From;
                }
                lastTrip = usedTrip;
            }

            this.UsedTrips = usedTrips.ToList();
            this.UsedTransfers = usedTransfers.ToList();

        }
        private Stop StopWithMinArrivalTime(List<Stop> toStops, Dictionary<Stop, JourneySearchModel.StopRoutingInfo> routingInfo)
        {
            Stop stopWithMinArrTime = toStops[0];
            foreach (Stop stop in toStops)
            {
                if (routingInfo[stop].earliestArrival < routingInfo[stopWithMinArrTime].earliestArrival)
                {
                    stopWithMinArrTime = stop;
                }
            }
            return stopWithMinArrTime;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("Get On:");
            //sb.AppendLine("\t" + GetOnStops[UsedTrips[0]]);

            for (int i = 0; i < UsedTrips.Count; i++)
            {
                Trip trip = UsedTrips[i];
                sb.AppendLine("Trip:");
                if (i < UsedTrips.Count - 1)
                {
                    //sb.AppendLine("\tRoute " + trip.Route.ShortName + " from " + GetOnStops[trip].ToString() + " to " + usedTransfers[i].From.ToString());
                    sb.AppendLine("\tRoute " + trip.Route.ShortName);
                    sb.AppendLine("\t\tFrom " + GetOnStops[trip].ToString() + " at " + trip.StopTimes[trip.Route.GetStopIndex(GetOnStops[trip])].DepartureTime.ToLongTimeString());
                    sb.AppendLine("\t\tTo: " + UsedTransfers[i].From.ToString() + " at " + trip.StopTimes[trip.Route.GetStopIndex(UsedTransfers[i].From)].ArrivalTime.ToLongTimeString());
                    sb.AppendLine("\tTransfer " + UsedTransfers[i].Time + " s");
                }
                else if (i == UsedTrips.Count - 1)
                {
                    //sb.AppendLine("\tRoute " + trip.Route.ShortName + " from " + GetOnStops[trip].ToString() + " to " + toStop.ToString());
                    sb.AppendLine("\tRoute " + trip.Route.ShortName);
                    sb.AppendLine("\t\tFrom " + GetOnStops[trip].ToString() + " at " + trip.StopTimes[trip.Route.GetStopIndex(GetOnStops[trip])].DepartureTime.ToLongTimeString());
                    if (UsedTrips.Count == UsedTransfers.Count)
                    {
                        sb.AppendLine("\t\tTo: " + UsedTransfers[i].From.ToString() + " at " + trip.StopTimes[trip.Route.GetStopIndex(UsedTransfers[i].From)].ArrivalTime.ToLongTimeString());

                        sb.AppendLine("\tTransfer " + UsedTransfers[i].Time + " s");
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

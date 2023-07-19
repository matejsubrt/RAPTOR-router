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
        public List<Trip> UsedTrips { get; set; }
        public Dictionary<Trip, Stop> GetOnStops { get; set; } = new();
        public Stop toStop;
        public Stop fromStop;
        public List<Transfer> UsedTransfers { get; set; } = new List<Transfer>();

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
            fromStop = currStop;

            this.UsedTrips = usedTrips.ToList();
            this.UsedTransfers = usedTransfers.ToList();

        }
        private Stop? StopWithMinArrivalTime(List<Stop> toStops, Dictionary<Stop, JourneySearchModel.StopRoutingInfo> routingInfo)
        {
            Stop? stopWithMinArrTime = null;
            DateTime earliestArrival = DateTime.MaxValue;
            foreach (Stop stop in toStops)
            {
                if (routingInfo.ContainsKey(stop) && routingInfo[stop].earliestArrival < earliestArrival)
                {
                    stopWithMinArrTime = stop;
                    earliestArrival = routingInfo[stop].earliestArrival;
                }
            }
            return stopWithMinArrTime;
        }
        /*
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("Get On:");
            //sb.AppendLine("\t" + GetOnStops[UsedTrips[0]]);

            if (UsedTransfers.Count > 0 && (UsedTrips.Count == 0 || (UsedTrips.Count > 0 && UsedTransfers[0].To == GetOnStops[UsedTrips[0]])))
            {
                sb.AppendLine("\tTransfer " + UsedTransfers[0].Time + " s");
                UsedTransfers.RemoveAt(0);
            }

            for (int i = 0; i < UsedTrips.Count; i++)
            {
                Trip trip = UsedTrips[i];
                sb.AppendLine("Trip:");                

                if (i < UsedTrips.Count - 1)
                {
                    //sb.AppendLine("\tRoute " + trip.Route.ShortName + " from " + GetOnStops[trip].ToString() + " to " + usedTransfers[i].From.ToString());
                    sb.AppendLine("\tRoute " + trip.Route.ShortName);
                    sb.AppendLine("\t\tFrom: " + GetOnStops[trip].ToString() + " at " + trip.StopTimes[trip.Route.GetStopIndex(GetOnStops[trip])].DepartureTime.ToLongTimeString());
                    sb.AppendLine("\t\tTo: " + UsedTransfers[i].From.ToString() + " at " + trip.StopTimes[trip.Route.GetStopIndex(UsedTransfers[i].From)].ArrivalTime.ToLongTimeString());
                    sb.AppendLine("\tTransfer " + UsedTransfers[i].Time + " s");
                }
                else if (i == UsedTrips.Count - 1)
                {
                    //sb.AppendLine("\tRoute " + trip.Route.ShortName + " from " + GetOnStops[trip].ToString() + " to " + toStop.ToString());
                    sb.AppendLine("\tRoute " + trip.Route.ShortName);
                    sb.AppendLine("\t\tFrom: " + GetOnStops[trip].ToString() + " at " + trip.StopTimes[trip.Route.GetStopIndex(GetOnStops[trip])].DepartureTime.ToLongTimeString());
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
        */
    }
    public record SearchResultDTO
    {

        public List<UsedTrip> Trips { get; set; } = new List<UsedTrip>();
        public List<UsedTransfer> Transfers { get; set; } = new List<UsedTransfer>();


        internal SearchResultDTO(SearchResult result)
        {
            int tripIndex = 0;
            int transferIndex = 0;
            int segmentIndex = 0;

            bool startsWithTransfer = result.UsedTransfers[0].From == result.fromStop;
            bool endsWithTransfer = result.UsedTransfers[result.UsedTransfers.Count-1].To == result.toStop;

            if (startsWithTransfer)
            {
                Transfer firstTransfer = result.UsedTransfers[0];
                Transfers.Add(new UsedTransfer { segmentIndex = 0, srcStop = firstTransfer.From.Name, destStop = firstTransfer.To.Name, distance = firstTransfer.Distance, time = firstTransfer.Time });
                transferIndex++;
                segmentIndex++;
            }

            while(tripIndex < result.UsedTrips.Count && transferIndex < result.UsedTransfers.Count)
            {
                Trip currTrip = result.UsedTrips[tripIndex];
                Transfer currTransfer = result.UsedTransfers[transferIndex];

                Trips.Add(new UsedTrip
                {
                    segmentIndex = segmentIndex,
                    getOnStopIndex = currTrip.Route.RouteStops.IndexOf(result.GetOnStops[currTrip]),
                    getOffStopIndex = currTrip.Route.RouteStops.IndexOf(currTransfer.From),
                    stops = (from stop in currTrip.Route.RouteStops select stop.Name).ToList(),
                    getOnTime = currTrip.StopTimes[currTrip.Route.RouteStops.IndexOf(result.GetOnStops[currTrip])].DepartureTime,
                    getOffTime = currTrip.StopTimes[currTrip.Route.RouteStops.IndexOf(currTransfer.From)].ArrivalTime,
                    routeName = currTrip.Route.ShortName
                });
                segmentIndex++;
                tripIndex++;
                Transfers.Add(new UsedTransfer { segmentIndex = segmentIndex, srcStop = currTransfer.From.Name, destStop = currTransfer.To.Name, distance = currTransfer.Distance, time = currTransfer.Time });
                segmentIndex++;
                transferIndex++;
            }
            
            
            if (!endsWithTransfer)
            {
                Trip lastTrip = result.UsedTrips[result.UsedTrips.Count - 1];
                Trips.Add(new UsedTrip
                {
                    segmentIndex = segmentIndex,
                    getOnStopIndex = lastTrip.Route.RouteStops.IndexOf(result.GetOnStops[lastTrip]),
                    getOffStopIndex = lastTrip.Route.RouteStops.IndexOf(result.toStop),
                    stops = (from stop in lastTrip.Route.RouteStops select stop.Name).ToList(),
                    getOnTime = lastTrip.StopTimes[lastTrip.Route.RouteStops.IndexOf(result.GetOnStops[lastTrip])].DepartureTime,
                    getOffTime = lastTrip.StopTimes[lastTrip.Route.RouteStops.IndexOf(result.toStop)].ArrivalTime,
                    routeName = lastTrip.Route.ShortName
                });
            }
        }




        public class UsedTrip
        {
            public int segmentIndex { get; set; }
            public List<string> stops { get; set; } = new List<string>();
            public int getOnStopIndex { get; set; }
            public int getOffStopIndex { get; set; }
            public TimeOnly getOnTime { get; set; }
            public TimeOnly getOffTime { get; set; }
            public string routeName { get; set; }
        }
        public class UsedTransfer
        {
            public int segmentIndex { get; set; }
            public string srcStop { get; set; }
            public string destStop { get; set; }
            public int time { get; set; }
            public int distance { get; set; }
        }
    }
}

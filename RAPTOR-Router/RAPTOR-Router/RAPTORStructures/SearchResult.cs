using RAPTOR_Router.SearchModels;
using RAPTOR_Router.Routers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
    /// <summary>
    /// Class representing a result of a connection search
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// The trips used during the best found connection
        /// </summary>
        public List<UsedTrip> Trips { get; private set; } = new List<UsedTrip>();
        /// <summary>
        /// The transfers used during the best found connection
        /// </summary>
        public List<UsedTransfer> Transfers { get; private set; } = new List<UsedTransfer>();

        /// <summary>
        /// Creates the SearchResult from basic information about the found connection
        /// </summary>
        /// <param name="usedTrips">List of the used trips</param>
        /// <param name="usedTransfers">List of the used transfers</param>
        /// <param name="getOnStops">Get on stops for every used trip</param>
        /// <param name="getOffStops">Get off stops for every used trip</param>
        /// <param name="sourceStop">The stop from the source node with the best trip to destination leaving from it</param>
        /// <param name="destStop">The stop from the destination node with the best possible arrival time</param>
        internal SearchResult
        (
            List<Trip> usedTrips,
            List<Transfer> usedTransfers,
            Dictionary<Trip, Stop> getOnStops,
            Dictionary<Trip, Stop> getOffStops,
            Stop sourceStop, 
            Stop destStop
        ){
            if(usedTransfers.Count == 0 && usedTrips.Count == 1)
            {
                Trip trip = usedTrips[0];
                Trips.Add(new UsedTrip
                {
                    segmentIndex = 0,
                    getOnStopIndex = trip.Route.RouteStops.IndexOf(getOnStops[trip]),
                    getOffStopIndex = trip.Route.RouteStops.IndexOf(getOffStops[trip]),
                    stops = (from stop in trip.Route.RouteStops select stop.Name).ToList(),
                    getOnTime = trip.StopTimes[trip.Route.RouteStops.IndexOf(getOnStops[trip])].DepartureTime,
                    getOffTime = trip.StopTimes[trip.Route.RouteStops.IndexOf(getOffStops[trip])].ArrivalTime,
                    routeName = trip.Route.ShortName
                });
                return;
            }


            int tripIndex = 0;
            int transferIndex = 0;
            int segmentIndex = 0;

            bool startsWithTransfer = usedTransfers[0].From == sourceStop;
            bool endsWithTransfer = usedTransfers[usedTransfers.Count - 1].To == destStop;

            if (startsWithTransfer)
            {
                Transfer firstTransfer = usedTransfers[0];
                Transfers.Add(new UsedTransfer { segmentIndex = 0, srcStop = firstTransfer.From.Name, destStop = firstTransfer.To.Name, distance = firstTransfer.Distance, time = firstTransfer.Time });
                transferIndex++;
                segmentIndex++;
            }

            while (tripIndex < usedTrips.Count && transferIndex < usedTransfers.Count)
            {
                Trip currTrip = usedTrips[tripIndex];
                Transfer currTransfer = usedTransfers[transferIndex];

                Trips.Add(new UsedTrip
                {
                    segmentIndex = segmentIndex,
                    getOnStopIndex = currTrip.Route.RouteStops.IndexOf(getOnStops[currTrip]),
                    getOffStopIndex = currTrip.Route.RouteStops.IndexOf(getOffStops[currTrip]),
                    stops = (from stop in currTrip.Route.RouteStops select stop.Name).ToList(),
                    getOnTime = currTrip.StopTimes[currTrip.Route.RouteStops.IndexOf(getOnStops[currTrip])].DepartureTime,
                    getOffTime = currTrip.StopTimes[currTrip.Route.RouteStops.IndexOf(getOffStops[currTrip])].ArrivalTime,
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
                Trip lastTrip = usedTrips[usedTrips.Count - 1];
                Trips.Add(new UsedTrip
                {
                    segmentIndex = segmentIndex,
                    getOnStopIndex = lastTrip.Route.RouteStops.IndexOf(getOnStops[lastTrip]),
                    getOffStopIndex = lastTrip.Route.RouteStops.IndexOf(getOffStops[lastTrip]),
                    stops = (from stop in lastTrip.Route.RouteStops select stop.Name).ToList(),
                    getOnTime = lastTrip.StopTimes[lastTrip.Route.RouteStops.IndexOf(getOnStops[lastTrip])].DepartureTime,
                    getOffTime = lastTrip.StopTimes[lastTrip.Route.RouteStops.IndexOf(getOffStops[lastTrip])].ArrivalTime,
                    routeName = lastTrip.Route.ShortName
                });
            }
        }


        public override string ToString()
        {
            if(Trips.Count == 1 && Transfers.Count == 0)
            {
                return Trips[0].getOnTime.ToLongTimeString() + " - " + Trips[0].getOffTime.ToLongTimeString() + ": Line " + Trips[0].routeName + " from " + Trips[0].stops[Trips[0].getOnStopIndex] + " to " + Trips[0].stops[Trips[0].getOffStopIndex];
            }
            else if(Trips.Count == 0 && Transfers.Count == 1)
            {
                return "Transfer from " + Transfers[0].srcStop + " to " + Transfers[0].destStop + ", length: " + Transfers[0].time + "s = " + Transfers[0].distance + "m";
            }


            StringBuilder sb = new StringBuilder();

            int segmentIndex = 0;
            int tripIndex = 0;
            int transferIndex = 0;

            while(segmentIndex <= Math.Max(Trips[Trips.Count-1].segmentIndex, Transfers[Transfers.Count - 1].segmentIndex))
            {
                if (tripIndex < Trips.Count && Trips[tripIndex].segmentIndex == segmentIndex)
                {
                    sb.AppendLine(Trips[tripIndex].getOnTime.ToLongTimeString() + " - " + Trips[tripIndex].getOffTime.ToLongTimeString() + ": Line " + Trips[tripIndex].routeName + " from " + Trips[tripIndex].stops[Trips[tripIndex].getOnStopIndex] + " to " + Trips[tripIndex].stops[Trips[tripIndex].getOffStopIndex]);

                    tripIndex++;
                    segmentIndex++;
                }
                else if (transferIndex <Transfers.Count && Transfers[transferIndex].segmentIndex == segmentIndex)
                {
                    sb.AppendLine("Transfer from " + Transfers[transferIndex].srcStop + " to " + Transfers[transferIndex].destStop + ", length: " + Transfers[transferIndex].time + "s = " + Transfers[transferIndex].distance + "m");
                    transferIndex++;
                    segmentIndex++;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Class representing a trip used in a found connection
        /// </summary>
        public class UsedTrip
        {
            /// <summary>
            /// The index of this trip in the list of links on the connection (including the transfers)
            /// </summary>
            public int segmentIndex { get; set; }
            /// <summary>
            /// The list of stop names the trip passes through
            /// </summary>
            public List<string> stops { get; set; } = new List<string>();
            /// <summary>
            /// The index of the stop where the trip is boarded
            /// </summary>
            public int getOnStopIndex { get; set; }
            /// <summary>
            /// The index of the stop where the trip is gotten out of
            /// </summary>
            public int getOffStopIndex { get; set; }
            /// <summary>
            /// The time when the trip is boarded
            /// </summary>
            public TimeOnly getOnTime { get; set; }
            /// <summary>
            /// The timr when the trip is gotten out of
            /// </summary>
            public TimeOnly getOffTime { get; set; }
            /// <summary>
            /// The name (i.e. the headsign) of the route of the trip
            /// </summary>
            public string routeName { get; set; }
        }
        /// <summary>
        /// Class representing a used transfer in a found connection
        /// </summary>
        public class UsedTransfer
        {
            /// <summary>
            /// The index of this trip in the list of links on the connection (including the transfers)
            /// </summary>
            public int segmentIndex { get; set; }
            /// <summary>
            /// The name of the stop where the transfer begins
            /// </summary>
            public string srcStop { get; set; }
            /// <summary>
            /// The name of the stop where the trnsfer ends
            /// </summary>
            public string destStop { get; set; }
            /// <summary>
            /// The approximate number of seconds it takes to walk this transfer
            /// </summary>
            public int time { get; set; }
            /// <summary>
            /// The straight line distance between the 2 stops in the transfer in meters
            /// </summary>
            public int distance { get; set; }
        }
    }
}

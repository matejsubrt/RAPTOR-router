using System.Text;
#pragma warning disable 1591
using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Custom;

namespace RAPTOR_Router.Models.Results
{
    /// <summary>
    /// Class representing a result of a connection search
    /// </summary>
    public class SearchResult
    {
        private Settings usedSettings;
        /// <summary>
        /// The trips used during the best found connection
        /// </summary>
        public List<UsedTrip> UsedTrips { get; private set; } = new List<UsedTrip>();
        /// <summary>
        /// The transfers used during the best found connection
        /// </summary>
        public List<UsedTransfer> UsedTransfers { get; private set; } = new List<UsedTransfer>();

        public List<UsedBikeTrip> UsedBikeTrips { get; private set; } = new List<UsedBikeTrip>();

        public List<SegmentType> UsedSegmentTypes { get; private set; } = new List<SegmentType>();
        //public List<UsedSegment> UsedSegments { get; private set; } = new List<UsedSegment>();
        public int TransferCount { get; private set; }
        public int TripCount { get; private set; }
        public int BikeTripCount { get; private set; }

        //TODO: add time support
        public DateTime DepartureDateTime { get; set; }
        public DateTime ArrivalDateTime { get; set; }


        internal SearchResult(Settings settings)
        {
            usedSettings = settings;
        }

        /// <summary>
        /// Creates an used trip from the provided arguments, pushes it TO THE START of UsedSegments
        /// </summary>
        /// <param name="trip">The trip to add</param>
        /// <param name="getOnStop">The get on stop of this segment</param>
        /// <param name="getOffStop">The get off stop of this segment</param>
        internal void AddUsedTrip(Trip trip, Stop getOnStop, Stop getOffStop, DateTime time, bool toEnd)
        {
            var routeStops = trip.Route.RouteStops;

            DateTime destStopArrivalDateTime;
            if (toEnd)
            {
                // The connection is being constructed from the front (we append to the end), meaning the search was run backwards, meaning time is the SOURCE DEPARTURE time -> we need to find the destination arrival time
                DateOnly departureDate = DateOnly.FromDateTime(time);
                TimeOnly departureTime = TimeOnly.FromDateTime(time);
                var getOffStopIndex = routeStops.IndexOf(getOffStop);
                TimeOnly arrivalTime = trip.StopTimes[getOffStopIndex].ArrivalTime;

                if(arrivalTime >= departureTime)
                {
                    // The trip does not go over midnight
                    destStopArrivalDateTime = DateTimeExtensions.FromDateAndTime(departureDate, arrivalTime);
                }
                else
                {
                    // The trip does go over midnight between source and dest
                    DateOnly arrivalDate = departureDate.AddDays(1);
                    destStopArrivalDateTime = DateTimeExtensions.FromDateAndTime(arrivalDate, arrivalTime);
                }
            }
            else
            {
                // The connection is being constructed from the back (we append to the start), meaning the search was run forwards, meaning time is the DESTINATION ARRIVAL time
                destStopArrivalDateTime = time;
            }

            List<StopPass> stopPasses = GetStopPassesList(routeStops, trip.StopTimes, destStopArrivalDateTime);

            UsedTrip usedTrip = new UsedTrip(
                stopPasses,
                routeStops.IndexOf(getOnStop),
                routeStops.IndexOf(getOffStop),
                trip.Route.ShortName,
                trip.Route.Color);


            //UsedSegments.Insert(0, usedTrip);
            if (toEnd)
            {
                UsedTrips.Add(usedTrip);
                UsedSegmentTypes.Add(SegmentType.Trip);
            }
            else
            {
                UsedTrips.Insert(0, usedTrip);
                UsedSegmentTypes.Insert(0, SegmentType.Trip);
            }
            TripCount++;

            List<StopPass> GetStopPassesList(List<Stop> routeStops, List<StopTime> stopTimes, DateTime arrivalDateTime)
            {
                List<StopPass> stopPasses = new();
                DateOnly currDate = DateOnly.FromDateTime(arrivalDateTime);
                TimeOnly arrivalTime = TimeOnly.FromDateTime(arrivalDateTime);
                bool overMidnight = false;
                if (stopTimes[0].ArrivalTime > arrivalTime)
                {
                    // trip goes over midnight (i.e. the arrival time is before the departure time)
                    currDate = currDate.AddDays(-1);
                    overMidnight = true;
                }
                for (int i = 0; i < routeStops.Count; i++)
                {
                    DateTime stopArrivalDateTime;
                    DateTime stopDepartureDateTime;
                    if (overMidnight)
                    {
                        // We have reached the first stop after midnight
                        if (stopTimes[i].ArrivalTime < arrivalTime)
                        {
                            // Both the arrival and departure are after midnight
                            currDate = currDate.AddDays(1);
                            overMidnight = false;
                            stopArrivalDateTime = DateTimeExtensions.FromDateAndTime(currDate, stopTimes[i].ArrivalTime);
                            stopDepartureDateTime = DateTimeExtensions.FromDateAndTime(currDate, stopTimes[i].DepartureTime);
                        }
                        else if (stopTimes[i].DepartureTime != stopTimes[i].ArrivalTime && stopTimes[i].DepartureTime < arrivalTime)
                        {
                            // Only the departure is after midnight
                            stopArrivalDateTime = DateTimeExtensions.FromDateAndTime(currDate, stopTimes[i].ArrivalTime);
                            currDate = currDate.AddDays(1);
                            overMidnight = false;
                            stopDepartureDateTime = DateTimeExtensions.FromDateAndTime(currDate, stopTimes[i].DepartureTime);
                        }
                        else
                        {
                            stopArrivalDateTime = DateTimeExtensions.FromDateAndTime(currDate, stopTimes[i].ArrivalTime);
                            stopDepartureDateTime = DateTimeExtensions.FromDateAndTime(currDate, stopTimes[i].DepartureTime);
                        }
                    }
                    else
                    {
                        stopArrivalDateTime = DateTimeExtensions.FromDateAndTime(currDate, stopTimes[i].ArrivalTime);
                        stopDepartureDateTime = DateTimeExtensions.FromDateAndTime(currDate, stopTimes[i].DepartureTime);
                    }


                    StopPass stopPass = new();
                    stopPass.Name = routeStops[i].Name;
                    stopPass.Id = routeStops[i].Id;
                    stopPass.ArrivalTime = stopArrivalDateTime;
                    stopPass.DepartureTime = stopDepartureDateTime;
                    stopPasses.Add(stopPass);
                }
                return stopPasses;
            }
        }

        /// <summary>
        /// Creates an used transfer from the provided transfer, pushes it TO THE START of UsedSegments
        /// </summary>
        /// <param name="transfer">The transfer to add</param>
        internal void AddUsedTransfer(Transfer transfer, DateTime destArrivalTime, bool toEnd)
        {
            var realSrc = transfer.From;
            var realDest = transfer.To;
            StopInfo srcStopInfo = new(realSrc.Name, realSrc.Id, realSrc.Coords.Lat, realSrc.Coords.Lon);
            StopInfo destStopInfo = new(realDest.Name, realDest.Id, realDest.Coords.Lat, realDest.Coords.Lon);
            UsedTransfer usedTransfer = new UsedTransfer(
                srcStopInfo,
                destStopInfo,
                transfer.GetTransferTime(usedSettings.WalkingPace),
                transfer.Distance
            );
            if (toEnd)
            {
                UsedTransfers.Add(usedTransfer);
                UsedSegmentTypes.Add(SegmentType.Transfer);
            }
            else
            {
                UsedTransfers.Insert(0, usedTransfer);
                UsedSegmentTypes.Insert(0, SegmentType.Transfer);
            }
            TransferCount++;
        }
        internal void AddUsedBikeTrip(BikeStation from, BikeStation to, int distance, bool toEnd)
        {
            StopInfo srcStopInfo = new(from.Name, from.Id, from.Coords.Lat, from.Coords.Lon);
            StopInfo destStopInfo = new(to.Name, to.Id, to.Coords.Lat, to.Coords.Lon);
            UsedBikeTrip usedBikeTrip = new UsedBikeTrip(
                srcStopInfo,
                destStopInfo,
                distance,
                usedSettings.GetBikeTripTime(distance)
            );
            if (toEnd)
            {
                UsedBikeTrips.Add(usedBikeTrip);
                UsedSegmentTypes.Add(SegmentType.Bike);
            }
            else
            {
                UsedBikeTrips.Insert(0, usedBikeTrip);
                UsedSegmentTypes.Insert(0, SegmentType.Bike);
            }
            BikeTripCount++;
        }
        internal void AddUsedBikeTransfer(BikeTransfer transfer, DateTime arrivalTime, bool toEnd)
        {
            var realSrc = transfer.GetSrcRoutePoint();
            var realDest = transfer.GetDestRoutePoint();
            StopInfo srcStopInfo = new(realSrc.Name, realSrc.Id, realSrc.Coords.Lat, realSrc.Coords.Lon);
            StopInfo destStopInfo = new(realDest.Name, realDest.Id, realDest.Coords.Lat, realDest.Coords.Lon);
            UsedTransfer usedTransfer = new UsedTransfer(
                srcStopInfo,
                destStopInfo,
                transfer.GetTransferTime(usedSettings.WalkingPace),
                transfer.Distance
            );
            if (toEnd)
            {
                UsedTransfers.Add(usedTransfer);
                UsedSegmentTypes.Add(SegmentType.Transfer);
            }
            else
            {
                UsedTransfers.Insert(0, usedTransfer);
                UsedSegmentTypes.Insert(0, SegmentType.Transfer);
            }
            TransferCount++;
        }
        internal void AddUsedCustomTransfer(CustomTransfer transfer, DateTime arrivalTime, bool toEnd)
        {
            StopInfo srcStopInfo = new(transfer.GetSrcRoutePoint().Name, transfer.GetSrcRoutePoint().Id, transfer.GetSrcRoutePoint().Coords.Lat, transfer.GetSrcRoutePoint().Coords.Lon);
            StopInfo destStopInfo = new(transfer.GetDestRoutePoint().Name, transfer.GetDestRoutePoint().Id, transfer.GetDestRoutePoint().Coords.Lat, transfer.GetDestRoutePoint().Coords.Lon);
            UsedTransfer usedTransfer = new UsedTransfer(
                srcStopInfo,
                destStopInfo,
                transfer.GetTransferTime(usedSettings.WalkingPace),
                transfer.Distance
            );
            if (toEnd)
            {
                UsedTransfers.Add(usedTransfer);
                UsedSegmentTypes.Add(SegmentType.Transfer);
            }
            else
            {
                UsedTransfers.Insert(0, usedTransfer);
                UsedSegmentTypes.Insert(0, SegmentType.Transfer);
            }
            TransferCount++;
        }

        public override string ToString()
        {
            StringBuilder sb = new();

            sb.Append("DEPARTURE: " + DepartureDateTime.ToLongTimeString() + ", ARRIVAL: " + ArrivalDateTime.ToLongTimeString() + "\n");
            int tripIndex = 0;
            int transferIndex = 0;
            int bikeTripIndex = 0;
            foreach (SegmentType t in UsedSegmentTypes)
            {
                switch (t)
                {
                    case SegmentType.Transfer:
                        UsedTransfer transfer = UsedTransfers[transferIndex];
                        sb.AppendLine("Transfer from " + transfer.GetStartStopName() + " to " + transfer.GetEndStopName() + ", length: " + transfer.time + "s + reserve " + (usedSettings.GetMovingTransferLengthMultiplier() - 1.0) * transfer.time + "s = " + transfer.distance + "m");
                        transferIndex++;
                        break;
                    case SegmentType.Trip:
                        UsedTrip trip = UsedTrips[tripIndex];
                        sb.AppendLine(trip.stopPasses[trip.getOnStopIndex].DepartureTime.ToLongTimeString() + " - " + trip.stopPasses[trip.getOffStopIndex].ArrivalTime.ToLongTimeString() + ": Line " + trip.routeName + " from " + trip.GetStartStopName() + " to " + trip.GetEndStopName());
                        tripIndex++;
                        break;
                    case SegmentType.Bike:
                        UsedBikeTrip bikeTrip = UsedBikeTrips[bikeTripIndex];
                        sb.AppendLine("Bike from " + bikeTrip.GetStartStopName() + " to " + bikeTrip.GetEndStopName() + ", length: " + bikeTrip.distance + "m");
                        bikeTripIndex++;
                        break;
                    default:
                        sb.AppendLine("INVALID SEGMENT TYPE");
                        break;
                }
            }
            return sb.ToString();
        }
        public void SetDepartureAndArrivalTimesByEarliestDeparture(DateTime originalEarliestDepartureTime)
        {
            switch (UsedSegmentTypes[0])
            {
                case SegmentType.Transfer:
                    if (UsedSegmentTypes.Count == 1)
                    {
                        // The connection consists of a single transfer
                        DepartureDateTime = originalEarliestDepartureTime;
                        ArrivalDateTime = originalEarliestDepartureTime.AddSeconds(UsedTransfers[0].time);
                    }
                    else if (!UsedSegmentTypes.Contains(SegmentType.Trip))
                    {
                        DepartureDateTime = originalEarliestDepartureTime;
                        ArrivalDateTime = DepartureDateTime.AddSeconds(GetTotalSecondsBeforeFirstTrip());
                    }
                    else
                    {
                        DepartureDateTime = UsedTrips[0].stopPasses[UsedTrips[0].getOnStopIndex].DepartureTime.AddSeconds(-GetTotalSecondsBeforeFirstTrip());
                        ArrivalDateTime = UsedTrips[UsedTrips.Count - 1].stopPasses[UsedTrips[UsedTrips.Count - 1].getOffStopIndex].ArrivalTime.AddSeconds(GetTotalSecondsAfterLastTrip());
                    }
                    break;
                case SegmentType.Trip:
                    DepartureDateTime = UsedTrips[0].stopPasses[UsedTrips[0].getOnStopIndex].DepartureTime;
                    ArrivalDateTime = UsedTrips[UsedTrips.Count - 1].stopPasses[UsedTrips[UsedTrips.Count - 1].getOffStopIndex].ArrivalTime.AddSeconds(GetTotalSecondsAfterLastTrip());
                    break;
                case SegmentType.Bike:
                    if (UsedSegmentTypes.Count == 1)
                    {
                        // The connection consists of a single transfer
                        DepartureDateTime = originalEarliestDepartureTime;
                        ArrivalDateTime = originalEarliestDepartureTime.AddSeconds(UsedBikeTrips[0].time);
                    }
                    else if (!UsedSegmentTypes.Contains(SegmentType.Trip))
                    {
                        DepartureDateTime = originalEarliestDepartureTime;
                        ArrivalDateTime = DepartureDateTime.AddSeconds(GetTotalSecondsBeforeFirstTrip());
                    }
                    else
                    {
                        DepartureDateTime = UsedTrips[0].stopPasses[UsedTrips[0].getOnStopIndex].DepartureTime.AddSeconds(-GetTotalSecondsBeforeFirstTrip());
                        ArrivalDateTime = UsedTrips[UsedTrips.Count - 1].stopPasses[UsedTrips[UsedTrips.Count - 1].getOffStopIndex].ArrivalTime.AddSeconds(GetTotalSecondsAfterLastTrip());
                    }
                    break;
                default:
                    break;
            }

            int GetTotalSecondsBeforeFirstTrip()
            {
                int segmentIndex = 0;
                int transferIndex = 0;
                int bikeTripIndex = 0;
                int resultSeconds = 0;
                while (segmentIndex < UsedSegmentTypes.Count && UsedSegmentTypes[segmentIndex] != SegmentType.Trip)
                {
                    var segType = UsedSegmentTypes[segmentIndex];
                    if (segType == SegmentType.Transfer)
                    {
                        resultSeconds += UsedTransfers[transferIndex].time;
                        transferIndex++;
                    }
                    else if (segType == SegmentType.Bike)
                    {
                        resultSeconds += UsedBikeTrips[bikeTripIndex].time;
                        bikeTripIndex++;
                    }
                    segmentIndex++;
                }
                return resultSeconds;
            }
            int GetTotalSecondsAfterLastTrip()
            {
                int segmentIndex = UsedSegmentTypes.Count - 1;
                int transferIndex = UsedTransfers.Count - 1;
                int bikeTripIndex = UsedBikeTrips.Count - 1;
                int resultSeconds = 0;
                while (segmentIndex > 0 && UsedSegmentTypes[segmentIndex] != SegmentType.Trip)
                {
                    var segType = UsedSegmentTypes[segmentIndex];
                    if (segType == SegmentType.Transfer)
                    {
                        resultSeconds += UsedTransfers[transferIndex].time;
                        transferIndex--;
                    }
                    else if (segType == SegmentType.Bike)
                    {
                        resultSeconds += UsedBikeTrips[bikeTripIndex].time;
                        bikeTripIndex--;
                    }
                    segmentIndex--;
                }
                return resultSeconds;
            }
        }
        public void SetDepartureAndArrivalTimesByLatestArrival(DateTime originalLatestArrivalTime)
        {
            switch (UsedSegmentTypes[UsedSegmentTypes.Count - 1])
            {
                case SegmentType.Transfer:
                    if (UsedSegmentTypes.Count == 1)
                    {
                        // The connection consists of a single transfer
                        DepartureDateTime = originalLatestArrivalTime.AddSeconds(-UsedTransfers[0].time);
                        ArrivalDateTime = originalLatestArrivalTime;
                    }
                    else if (!UsedSegmentTypes.Contains(SegmentType.Trip))
                    {
                        DepartureDateTime = originalLatestArrivalTime.AddSeconds(-GetTotalSecondsAfterLastTrip());
                        ArrivalDateTime = originalLatestArrivalTime;
                    }
                    else
                    {
                        DepartureDateTime = UsedTrips[0].stopPasses[UsedTrips[0].getOnStopIndex].DepartureTime.AddSeconds(-GetTotalSecondsBeforeFirstTrip());
                        ArrivalDateTime = UsedTrips[UsedTrips.Count - 1].stopPasses[UsedTrips[UsedTrips.Count - 1].getOffStopIndex].ArrivalTime.AddSeconds(GetTotalSecondsAfterLastTrip());
                    }
                    break;
                case SegmentType.Trip:
                    DepartureDateTime = UsedTrips[0].stopPasses[UsedTrips[0].getOnStopIndex].DepartureTime;
                    ArrivalDateTime = UsedTrips[UsedTrips.Count - 1].stopPasses[UsedTrips[UsedTrips.Count - 1].getOffStopIndex].ArrivalTime.AddSeconds(GetTotalSecondsAfterLastTrip());
                    break;
                case SegmentType.Bike:
                    if (UsedSegmentTypes.Count == 1)
                    {
                        // The connection consists of a single transfer
                        DepartureDateTime = originalLatestArrivalTime.AddSeconds(-UsedBikeTrips[0].time);
                        ArrivalDateTime = originalLatestArrivalTime;
                    }
                    else if (!UsedSegmentTypes.Contains(SegmentType.Trip))
                    {
                        DepartureDateTime = originalLatestArrivalTime.AddSeconds(-GetTotalSecondsBeforeFirstTrip());
                        ArrivalDateTime = originalLatestArrivalTime;
                    }
                    else
                    {
                        DepartureDateTime = UsedTrips[0].stopPasses[UsedTrips[0].getOnStopIndex].DepartureTime.AddSeconds(-GetTotalSecondsBeforeFirstTrip());
                        ArrivalDateTime = UsedTrips[UsedTrips.Count - 1].stopPasses[UsedTrips[UsedTrips.Count - 1].getOffStopIndex].ArrivalTime.AddSeconds(GetTotalSecondsAfterLastTrip());
                    }
                    break;
                default:
                    break;
            }

            int GetTotalSecondsBeforeFirstTrip()
            {
                int segmentIndex = 0;
                int transferIndex = 0;
                int bikeTripIndex = 0;
                int resultSeconds = 0;
                while (segmentIndex < UsedSegmentTypes.Count && UsedSegmentTypes[segmentIndex] != SegmentType.Trip)
                {
                    var segType = UsedSegmentTypes[segmentIndex];
                    if (segType == SegmentType.Transfer)
                    {
                        resultSeconds += UsedTransfers[transferIndex].time;
                        transferIndex++;
                    }
                    else if (segType == SegmentType.Bike)
                    {
                        resultSeconds += UsedBikeTrips[bikeTripIndex].time;
                        bikeTripIndex++;
                    }
                    segmentIndex++;
                }
                return resultSeconds;
            }
            int GetTotalSecondsAfterLastTrip()
            {
                int segmentIndex = UsedSegmentTypes.Count - 1;
                int transferIndex = UsedTransfers.Count - 1;
                int bikeTripIndex = UsedBikeTrips.Count - 1;
                int resultSeconds = 0;
                while (segmentIndex > 0 && UsedSegmentTypes[segmentIndex] != SegmentType.Trip)
                {
                    var segType = UsedSegmentTypes[segmentIndex];
                    if (segType == SegmentType.Transfer)
                    {
                        resultSeconds += UsedTransfers[transferIndex].time;
                        transferIndex--;
                    }
                    else if (segType == SegmentType.Bike)
                    {
                        resultSeconds += UsedBikeTrips[bikeTripIndex].time;
                        bikeTripIndex--;
                    }
                    segmentIndex--;
                }
                return resultSeconds;
            }
        }

        public enum SegmentType
        {
            Transfer = 0,
            Trip = 1,
            Bike = 2
        }

        public interface UsedSegment
        {
            public string? ToString();
            public SegmentType segmentType { get; }
            public string GetStartStopName();
            public string GetEndStopName();
        }
        public class StopPass
        {
            public string Name;
            public string Id;
            public DateTime ArrivalTime;
            public DateTime DepartureTime;
        }
        public class StopInfo
        {
            public string Name;
            public string Id;
            public double Lat;
            public double Lon;
            public StopInfo(string name, string id, double lat, double lon)
            {
                Name = name;
                Id = id;
                Lat = lat;
                Lon = lon;
            }
        }

        /// <summary>
        /// Class representing a trip used in a found connection
        /// </summary>
        public class UsedTrip : UsedSegment
        {
            public SegmentType segmentType { get; private set; }
            public List<StopPass> stopPasses = new List<StopPass>();
            /// <summary>
            /// The index of the stop where the trip is boarded
            /// </summary>
            public int getOnStopIndex { get; set; }
            /// <summary>
            /// The index of the stop where the trip is gotten out of
            /// </summary>
            public int getOffStopIndex { get; set; }
            /// <summary>
            /// The name (i.e. the headsign) of the route of the trip
            /// </summary>
            public string routeName { get; set; }
            public Color Color { get; set; }

            public UsedTrip(
                List<StopPass> stopPasses,
                int getOnStopIndex,
                int getOffStopIndex,
                string routeName,
                Color color
            )
            {
                segmentType = SegmentType.Trip;
                this.stopPasses = stopPasses;
                this.getOnStopIndex = getOnStopIndex;
                this.getOffStopIndex = getOffStopIndex;
                this.routeName = routeName;
                Color = color;
            }
            public string GetStartStopName()
            {
                return stopPasses[getOnStopIndex].Name;
            }
            public string GetEndStopName()
            {
                return stopPasses[getOffStopIndex].Name;
            }
        }
        /// <summary>
        /// Class representing a used transfer in a found connection
        /// </summary>
        public class UsedTransfer : UsedSegment
        {
            public SegmentType segmentType { get; private set; }
            public UsedTransfer(
                StopInfo srcStopInfo,
                StopInfo destStopInfo,
                int time,
                int distance
            )
            {
                this.srcStopInfo = srcStopInfo;
                this.destStopInfo = destStopInfo;
                this.time = time;
                this.distance = distance;
            }
            /// <summary>
            /// The name of the stop where the transfer begins
            /// </summary>
            public StopInfo srcStopInfo { get; set; }
            public StopInfo destStopInfo { get; set; }
            /// <summary>
            /// The approximate number of seconds it takes to walk this transfer
            /// </summary>
            public int time { get; set; }
            /// <summary>
            /// The straight line distance between the 2 stops in the transfer in meters
            /// </summary>
            public int distance { get; set; }

            public string GetStartStopName()
            {
                return srcStopInfo.Name;
            }
            public string GetEndStopName()
            {
                return destStopInfo.Name;
            }
        }
        public class UsedBikeTrip : UsedSegment
        {
            public SegmentType segmentType { get; private set; }
            public UsedBikeTrip(
                StopInfo srcStopInfo,
                StopInfo destStopInfo,
                int distance,
                int time
            )
            {
                this.srcStopInfo = srcStopInfo;
                this.destStopInfo = destStopInfo;
                this.distance = distance;
                this.time = time;
            }
            public StopInfo srcStopInfo { get; set; }
            public StopInfo destStopInfo { get; set; }
            public int distance { get; set; }
            public int time { get; set; }

            public string GetStartStopName()
            {
                return srcStopInfo.Name;
            }
            public string GetEndStopName()
            {
                return destStopInfo.Name;
            }
        }
    }
}

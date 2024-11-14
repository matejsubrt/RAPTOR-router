using System.Text;
using RAPTOR_Router.Structures.Requests;
#pragma warning disable 1591
using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Transit;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Custom;
using System.Text.Json.Serialization;

namespace RAPTOR_Router.Models.Results
{
    public class AlternativeTripsSearchResult
    {
        public List<SearchResult.UsedTrip> Alternatives { get; set; } = new();
        public AlternativesSearchError Error { get; set; }
        public AlternativeTripsSearchResult() { }
        public AlternativeTripsSearchResult(List<SearchResult.UsedTrip> alternatives, AlternativesSearchError error)
        {
            Alternatives = alternatives;
            Error = error;
        }
    }


    //TODO: rename this
    public class CompleteSearchResult
    {
        public List<SearchResult>? Results { get; set; }
        public ConnectionSearchError Error { get; set; }

        public CompleteSearchResult()
        {
        }

        public CompleteSearchResult(List<SearchResult> results, ConnectionSearchError error)
        {
            this.Results = results;
            this.Error = error;
        }
    }

    /// <summary>
    /// Class representing the result of a connection search
    /// </summary>
    /// <remarks>If running the program as an API, this class is serialized and returned as json in the response body</remarks>
    public class SearchResult
    {
        public class TripAlternatives
        {
            public int CurrIndex { get; set; }
            public List<UsedTrip> Alternatives { get; set; } = new();
            public int Count { get; set; }

            public TripAlternatives(int currIndex, List<UsedTrip> alternatives)
            {
                CurrIndex = currIndex;
                Alternatives = alternatives;
                Count = alternatives.Count;
            }
        }

        /// <summary>
        /// The settings used for the search
        /// </summary>
        private Settings usedSettings;
        /// <summary>
        /// The public transit trips used during the best found connection
        /// </summary>
        /// <remarks>To be used for simpler client-side handling that does not support alternatives</remarks>
        public List<UsedTrip> UsedTrips { get; private set; } = new List<UsedTrip>();
        /// <summary>
        /// The public transit trips used during the best found connection, with their alternatives
        /// </summary>
        /// <remarks>To be used in client-side apps that do support trip alternatives. It is initialized with only the one best trip,
        /// with the expectation that the user will expand this via the alternative trips API</remarks>
        public List<TripAlternatives> UsedTripAlternatives { get; private set; } = new();
        /// <summary>
        /// The transfers used during the best found connection
        /// </summary>
        public List<UsedTransfer> UsedTransfers { get; private set; } = new List<UsedTransfer>();
        /// <summary>
        /// The bike trips used during the best found connection
        /// </summary>
        public List<UsedBikeTrip> UsedBikeTrips { get; private set; } = new List<UsedBikeTrip>();

        /// <summary>
        /// The list of segment types used in the connection -> used to determine the order of the segments and make deserialization easier
        /// </summary>
        public List<SegmentType> UsedSegmentTypes { get; private set; } = new List<SegmentType>();
        /// <summary>
        /// The number of used transfers in the connection
        /// </summary>
        public int TransferCount { get; private set; }
        /// <summary>
        /// The number of used public transit trips in the connection
        /// </summary>
        public int TripCount { get; private set; }
        /// <summary>
        /// The number of used bike trips in the connection
        /// </summary>
        public int BikeTripCount { get; private set; }


        /// <summary>
        /// The source departure time of the connection
        /// </summary>
        public DateTime DepartureDateTime { get; set; }
        /// <summary>
        /// The destination arrival time of the connection
        /// </summary>
        public DateTime ArrivalDateTime { get; set; }


        /// <summary>
        /// Creates a new SearchResult object
        /// </summary>
        /// <param name="settings">The settings that were used for the search</param>
        internal SearchResult(Settings settings)
        {
            usedSettings = settings;
        }

        internal void AddUsedTrip(Trip trip, DateOnly tripStartDate, Stop realGetOnStop, Stop realGetOffStop, bool hasDelayInfo, int delayWhenBoarded, int currentDelay, bool toEnd)
        {
            var routeStops = trip.Route.RouteStops;
            var stopTimes = trip.StopTimes;
            DateTime destStopArrivalDateTime;

            int getOnStopIndex = routeStops.IndexOf(realGetOnStop);
            int getOffStopIndex = routeStops.IndexOf(realGetOffStop);


            DateTime departureTime = trip.GetDepartureDateTime(getOnStopIndex, tripStartDate);//stopTimes[getOnStopIndex].GetDepartureDateTime(tripStartDate);
            DateTime arrivalTime = trip.GetArrivalDateTime(getOffStopIndex, tripStartDate);//stopTimes[getOffStopIndex].GetArrivalDateTime(tripStartDate);

            if (toEnd)
            {
                // The connection is being constructed from the front (we append to the end), meaning the search was run backwards, meaning time is the SOURCE DEPARTURE time -> we need to find the destination arrival time
                destStopArrivalDateTime = arrivalTime;
            }
            else
            {
                // The connection is being constructed from the back (we append to the start), meaning the search was run forwards, meaning time is the DESTINATION ARRIVAL time
                destStopArrivalDateTime = departureTime;
            }

            List<StopPass> stopPasses = GetStopPassesList(routeStops, trip.StopTimes, tripStartDate);

            UsedTrip usedTrip = new UsedTrip(
                stopPasses,
                getOnStopIndex,
                getOffStopIndex,
                trip.Route.ShortName,
                trip.Route.Color,
                trip.Route.Type,
                hasDelayInfo,
                delayWhenBoarded,
                currentDelay,
                trip.Id);


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


        }

        /// <summary>
        /// Creates the list of stop passes for a trip
        /// </summary>
        /// <param name="routeStops">The stop list of the trip</param>
        /// <param name="stopTimes">The stop times of the trip</param>
        /// <param name="tripStartDate">The date on which the trip starts</param>
        /// <returns></returns>
        public static List<StopPass> GetStopPassesList(List<Stop> routeStops, List<StopTime> stopTimes, DateOnly tripStartDate)
        {
            List<StopPass> stopPasses = new();
            for(int i = 0; i < routeStops.Count; i++)
            {
                DateTime stopArrivalDateTime = stopTimes[i].GetArrivalDateTime(tripStartDate);
                DateTime stopDepartureDateTime = stopTimes[i].GetDepartureDateTime(tripStartDate);
                StopPass stopPass = new(routeStops[i].Name, routeStops[i].Id, stopArrivalDateTime, stopDepartureDateTime);
                stopPasses.Add(stopPass);
            }

            return stopPasses;
        }

        internal void InitializeAlternatives()
        {
            foreach (UsedTrip usedTrip in UsedTrips)
            {
                UsedTripAlternatives.Add(new TripAlternatives(0, new List<UsedTrip> {usedTrip}));
            }
            
        }

        /// <summary>
        /// Creates an used transfer from the provided arguments, adds it to UsedTransfers
        /// </summary>
        /// <param name="toEnd">Specifies if the used transfer should be appended to the end or beginning of UsedTransfers</param>
        /// <remarks>
        ///     toEnd = true means the connection is being reconstructed from start to end -> backward search was used
        ///     toEnd = false means the connection is being reconstructed from end to start -> forward search was used
        /// </remarks>
        /// <param name="transfer">The transfer to add</param>
        internal void AddUsedTransfer(Transfer transfer, DateTime destArrivalTime, bool toEnd)
        {
            var realSrc = transfer.From;
            var realDest = transfer.To;
            RoutePointInfo srcStopInfo = new(realSrc.Name, realSrc.Id, realSrc.Coords.Lat, realSrc.Coords.Lon);
            RoutePointInfo destStopInfo = new(realDest.Name, realDest.Id, realDest.Coords.Lat, realDest.Coords.Lon);
            UsedTransfer usedTransfer = new UsedTransfer(
                srcStopInfo,
                destStopInfo,
                usedSettings.GetAdjustedWalkingTransferTime(transfer.Distance),
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


        /// <summary>
        /// Creates an used bike trip from the provided arguments, adds it to UsedBikeTrips
        /// </summary>
        /// <param name="from">The source station of the bike trip</param>
        /// <param name="to">The destination station of the bike trip</param>
        /// <param name="distance">The distance of the bike trip in meters</param>
        /// <param name="toEnd">Specifies if the used trip should be appended to the end or beginning of UsedTrips</param>
        /// <remarks>
        ///     toEnd = true means the connection is being reconstructed from start to end -> backward search was used
        ///     toEnd = false means the connection is being reconstructed from end to start -> forward search was used
        /// </remarks>
        internal void AddUsedBikeTrip(BikeStation from, BikeStation to, int distance, bool toEnd)
        {
            RoutePointInfo srcStopInfo = new(from.Name, from.Id, from.Coords.Lat, from.Coords.Lon);
            RoutePointInfo destStopInfo = new(to.Name, to.Id, to.Coords.Lat, to.Coords.Lon);
            UsedBikeTrip usedBikeTrip = new UsedBikeTrip(
                srcStopInfo,
                destStopInfo,
                distance,
                usedSettings.GetBikeTripTime(distance),
                from.BikeCount
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

        /// <summary>
        /// Creates an used transfer from the provided arguments, adds it to UsedTransfers
        /// </summary>
        /// <param name="toEnd">Specifies if the used transfer should be appended to the end or beginning of UsedTransfers</param>
        /// <remarks>
        ///     toEnd = true means the connection is being reconstructed from start to end -> backward search was used
        ///     toEnd = false means the connection is being reconstructed from end to start -> forward search was used
        /// </remarks>
        /// <param name="transfer">The transfer to add</param>
        internal void AddUsedTransfer(BikeTransfer transfer, DateTime arrivalTime, bool toEnd)
        {
            var realSrc = transfer.GetSrcRoutePoint();
            var realDest = transfer.GetDestRoutePoint();
            RoutePointInfo srcStopInfo = new(realSrc.Name, realSrc.Id, realSrc.Coords.Lat, realSrc.Coords.Lon);
            RoutePointInfo destStopInfo = new(realDest.Name, realDest.Id, realDest.Coords.Lat, realDest.Coords.Lon);
            UsedTransfer usedTransfer = new UsedTransfer(
                srcStopInfo,
                destStopInfo,
                usedSettings.GetAdjustedWalkingTransferTime(transfer.Distance),
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

        /// <summary>
        /// Creates an used transfer from the provided arguments, adds it to UsedTransfers
        /// </summary>
        /// <param name="toEnd">Specifies if the used transfer should be appended to the end or beginning of UsedTransfers</param>
        /// <remarks>
        ///     toEnd = true means the connection is being reconstructed from start to end -> backward search was used
        ///     toEnd = false means the connection is being reconstructed from end to start -> forward search was used
        /// </remarks>
        /// <param name="transfer">The transfer to add</param>
        internal void AddUsedTransfer(CustomTransfer transfer, DateTime arrivalTime, bool toEnd)
        {
            RoutePointInfo srcStopInfo = new(transfer.GetSrcRoutePoint().Name, transfer.GetSrcRoutePoint().Id, transfer.GetSrcRoutePoint().Coords.Lat, transfer.GetSrcRoutePoint().Coords.Lon);
            RoutePointInfo destStopInfo = new(transfer.GetDestRoutePoint().Name, transfer.GetDestRoutePoint().Id, transfer.GetDestRoutePoint().Coords.Lat, transfer.GetDestRoutePoint().Coords.Lon);
            UsedTransfer usedTransfer = new UsedTransfer(
                srcStopInfo,
                destStopInfo,
                usedSettings.GetAdjustedWalkingTransferTime(transfer.Distance),
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


        /// <summary>
        /// Creates a string representation of the result
        /// </summary>
        /// <returns>The string representation of the object</returns>
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
                        sb.AppendLine(transfer.ToString());
                        transferIndex++;
                        break;
                    case SegmentType.Trip:
                        UsedTrip trip = UsedTrips[tripIndex];
                        sb.AppendLine(trip.ToString());
                        tripIndex++;
                        break;
                    case SegmentType.Bike:
                        UsedBikeTrip bikeTrip = UsedBikeTrips[bikeTripIndex];
                        sb.AppendLine(bikeTrip.ToString());
                        bikeTripIndex++;
                        break;
                    default:
                        sb.AppendLine("INVALID SEGMENT TYPE");
                        break;
                }
            }
            return sb.ToString();
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


        public void SetDepartureAndArrivalTimesByEarliestDeparture(DateTime originalEarliestDepartureTime)
        {
            DateTime firstTripGetOnTime = UsedTrips.Count > 0 ? UsedTrips[0].stopPasses[UsedTrips[0].getOnStopIndex].DepartureTime : DateTime.MaxValue;
            DateTime lastTripGetOffTime = UsedTrips.Count > 0 ? UsedTrips[^1].stopPasses[UsedTrips[^1].getOffStopIndex].ArrivalTime : DateTime.MinValue;
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
                        DepartureDateTime = firstTripGetOnTime.AddSeconds(-GetTotalSecondsBeforeFirstTrip()).AddSeconds(UsedTrips[0].delayWhenBoarded);
                        ArrivalDateTime = lastTripGetOffTime.AddSeconds(UsedTrips[^1].currentDelay).AddSeconds(GetTotalSecondsAfterLastTrip());
                    }
                    break;
                case SegmentType.Trip:
                    DepartureDateTime = firstTripGetOnTime.AddSeconds(UsedTrips[0].delayWhenBoarded);
                    ArrivalDateTime = lastTripGetOffTime.AddSeconds(UsedTrips[^1].currentDelay).AddSeconds(GetTotalSecondsAfterLastTrip());
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
                        DepartureDateTime = firstTripGetOnTime.AddSeconds(UsedTrips[0].delayWhenBoarded).AddSeconds(-GetTotalSecondsBeforeFirstTrip());
                        ArrivalDateTime = lastTripGetOffTime.AddSeconds(UsedTrips[^1].currentDelay).AddSeconds(GetTotalSecondsAfterLastTrip());
                    }
                    break;
                default:
                    break;
            }

            
        }
        public void SetDepartureAndArrivalTimesByLatestArrival(DateTime originalLatestArrivalTime)
        {
            DateTime firstTripGetOnTime = UsedTrips.Count > 0 ? UsedTrips[0].stopPasses[UsedTrips[0].getOnStopIndex].DepartureTime : DateTime.MaxValue;
            DateTime lastTripGetOffTime = UsedTrips.Count > 0 ? UsedTrips[^1].stopPasses[UsedTrips[^1].getOffStopIndex].ArrivalTime : DateTime.MinValue;
            switch (UsedSegmentTypes[^1])
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
                        DepartureDateTime = firstTripGetOnTime.AddSeconds(-GetTotalSecondsBeforeFirstTrip());
                        ArrivalDateTime = lastTripGetOffTime.AddSeconds(GetTotalSecondsAfterLastTrip());
                    }
                    break;
                case SegmentType.Trip:
                    DepartureDateTime = firstTripGetOnTime;
                    ArrivalDateTime = lastTripGetOffTime.AddSeconds(GetTotalSecondsAfterLastTrip());
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
                        DepartureDateTime = firstTripGetOnTime.AddSeconds(-GetTotalSecondsBeforeFirstTrip());
                        ArrivalDateTime = lastTripGetOffTime.AddSeconds(GetTotalSecondsAfterLastTrip());
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Enum representing the segment type
        /// </summary>
        public enum SegmentType
        {
            Transfer = 0,
            Trip = 1,
            Bike = 2
        }

        /// <summary>
        /// Interface representing a segment used in a found connection
        /// </summary>
        public interface UsedSegment
        {
            public string? ToString();
            public string GetStartStopName();
            public string GetEndStopName();
        }



        /// <summary>
        /// Class repressenting a stop of a trip used in a found connection, containing the stop name, id, arrival and departure time
        /// </summary>
        /// <remarks>Intended for easy serialization</remarks>
        public class StopPass
        {
            /// <summary>
            /// The name of the stop
            /// </summary>
            [JsonInclude]
            public string Name { get; }
            /// <summary>
            /// The id of the stop
            /// </summary>
            [JsonInclude]
            public string Id { get; }
            /// <summary>
            /// The arrival time at the stop
            /// </summary>
            [JsonInclude]
            public DateTime ArrivalTime { get; }
            /// <summary>
            /// The departure time from the stop
            /// </summary>
            [JsonInclude]
            public DateTime DepartureTime { get; }

            /// <summary>
            /// Creates a new StopPass object
            /// </summary>
            /// <param name="name">The name of the stop</param>
            /// <param name="id">The id of the stop</param>
            /// <param name="arrivalTime">The arrival time at the stop</param>
            /// <param name="departureTime">The departure time from the stop</param>
            public StopPass(string name, string id, DateTime arrivalTime, DateTime departureTime)
            {
                Name = name;
                Id = id;
                ArrivalTime = arrivalTime;
                DepartureTime = departureTime;
            }
        }


        /// <summary>
        /// Class representing information about a route point
        /// </summary>
        /// <remarks>Intended for easy serialization</remarks>
        public class RoutePointInfo
        {
            /// <summary>
            /// The name of the route point
            /// </summary>
            [JsonInclude]
            public string Name { get; }
            /// <summary>
            /// The Id of the route point
            /// </summary>
            [JsonInclude]
            public string Id { get; }
            /// <summary>
            /// The latitude of the route point
            /// </summary>
            [JsonInclude]
            public double Lat { get; }
            /// <summary>
            /// The longitude of the route point
            /// </summary>
            [JsonInclude]
            public double Lon { get; }
            /// <summary>
            /// Creates a new RoutePointInfo object
            /// </summary>
            /// <param name="name">The name of te RoutePoint</param>
            /// <param name="id">The id of the RoutePoint></param>
            /// <param name="lat">The latitude of the RoutePoint</param>
            /// <param name="lon">The longitude of the RoutePoint</param>
            public RoutePointInfo(string name, string id, double lat, double lon)
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
        /// <remarks>Intended for easy serialization</remarks>
        public class UsedTrip : UsedSegment
        {
            /// <summary>
            /// The list of the stop passes of the trip
            /// </summary>
            [JsonInclude]
            public List<StopPass> stopPasses = new List<StopPass>();
            /// <summary>
            /// The index of the stop where the trip is boarded
            /// </summary>
            [JsonInclude]
            public int getOnStopIndex { get; set; }
            /// <summary>
            /// The index of the stop where the trip is gotten out of
            /// </summary>
            [JsonInclude]
            public int getOffStopIndex { get; set; }
            /// <summary>
            /// The name (i.e. the headsign) of the route of the trip
            /// </summary>
            [JsonInclude]
            public string routeName { get; set; }
            /// <summary>
            /// The color of the trip's route
            /// </summary>
            [JsonInclude]
            public Color color { get; set; }
            /// <summary>
            /// The vehicle type of the trip
            /// </summary>
            [JsonInclude]
            public Route.VehicleType vehicleType { get; set; }
            /// <summary>
            /// Whether the trip has current delay information available (i.e. this typically 
            /// means the trip is en-route, or will be in the soon future)
            /// </summary>
            [JsonInclude]
            public bool hasDelayInfo { get; set; }
            /// <summary>
            /// The delay of the trip at the getOnStop - this can either mean the expected delay 
            /// there (if the trip has not yet arrived there), or the actual delay at that stop 
            /// (if the trip has already been there)
            /// </summary>
            [JsonInclude]
            public int delayWhenBoarded { get; set; }
            /// <summary>
            /// The delay of the trip at the moment of the search. This can be different from 
            /// delayWhenBoarded if the connection is (partly) in the past - i.e. the trip has 
            /// already been through the getOnStop, but has not yet ended.
            /// The value is only valid at the moment of the search, and it is expected that
            /// the client will update this value as time progresses using the TODO: API delay update endpoint
            /// </summary>
            [JsonInclude]
            public int currentDelay { get; set; }
            /// <summary>
            /// The trip Id of the associated trip
            /// </summary>
            [JsonInclude]
            public string tripId { get; set; }


            /// <summary>
            /// Creates a new UsedTrip object
            /// </summary>
            /// <param name="stopPasses">The list of stop passes of the trip</param>
            /// <param name="getOnStopIndex">The index of the stop where the trip is boarded</param>
            /// <param name="getOffStopIndex">The index of the stop where the trip is exited</param>
            /// <param name="routeName">The name of the trip's route</param>
            /// <param name="color">The color of the trip's route</param>
            public UsedTrip(
                List<StopPass> stopPasses,
                int getOnStopIndex,
                int getOffStopIndex,
                string routeName,
                Color color,
                Route.VehicleType vehicleType,
                bool hasDelayInfo,
                int delayWhenBoarded,
                int currentDelay,
                string tripId
            )
            {
                this.stopPasses = stopPasses;
                this.getOnStopIndex = getOnStopIndex;
                this.getOffStopIndex = getOffStopIndex;
                this.routeName = routeName;
                this.color = color;
                this.vehicleType = vehicleType;
                this.delayWhenBoarded = delayWhenBoarded;
                this.hasDelayInfo = hasDelayInfo;
                this.currentDelay = currentDelay;
                this.tripId = tripId;
            }

            public override string ToString()
            {
                DateTime regularDepartureTime = stopPasses[getOnStopIndex].DepartureTime;
                DateTime actualDepartureTime = regularDepartureTime.AddSeconds(delayWhenBoarded);

                DateTime regularArrivalTime = stopPasses[getOffStopIndex].ArrivalTime;
                DateTime actualArrivalTime = regularArrivalTime.AddSeconds(currentDelay);
                return "(" + regularDepartureTime.ToLongTimeString() + ")" + actualDepartureTime.ToLongTimeString() + " - (" + regularArrivalTime.ToLongTimeString() + ")" + actualArrivalTime.ToLongTimeString() + ": Line " + routeName + " from " + GetStartStopName() + " to " + GetEndStopName();
            }

            /// <summary>
            /// Gets the name of the stop where the trip is boarded
            /// </summary>
            /// <returns>The name of the stop</returns>
            public string GetStartStopName()
            {
                return stopPasses[getOnStopIndex].Name;
            }
            /// <summary>
            /// Gets the name of the stop where the trip is exited
            /// </summary>
            /// <returns>The name of the stop</returns>
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
            /// <summary>
            /// Theinformation about the stop where the transfer begins
            /// </summary>
            [JsonInclude]
            public RoutePointInfo srcStopInfo { get; set; }
            /// <summary>
            /// The information about the stop where the transfer ends
            /// </summary>
            [JsonInclude]
            public RoutePointInfo destStopInfo { get; set; }
            /// <summary>
            /// The approximate number of seconds it takes to walk this transfer
            /// </summary>
            [JsonInclude]
            public int time { get; set; }
            /// <summary>
            /// The straight line distance between the 2 stops in the transfer in meters
            /// </summary>
            [JsonInclude]
            public int distance { get; set; }

            /// <summary>
            /// Creates a new UsedTransfer object
            /// </summary>
            /// <param name="srcStopInfo">The info about the transfer source stop</param>
            /// <param name="destStopInfo">The information about the transfer destination stop</param>
            /// <param name="time">The time it takes to trasfer</param>
            /// <param name="distance">The distance of the transfer</param>
            public UsedTransfer(
                RoutePointInfo srcStopInfo,
                RoutePointInfo destStopInfo,
                int time,
                int distance
            ){
                this.srcStopInfo = srcStopInfo;
                this.destStopInfo = destStopInfo;
                this.time = time;
                this.distance = distance;
            }

            public override string ToString()
            {
                return "Transfer from " + srcStopInfo.Name + " to " + destStopInfo.Name + ", length: " + time + "s = " + distance + "m";
            }

            /// <summary>
            /// Gets the name of the stop where the transfer begins
            /// </summary>
            /// <returns></returns>
            public string GetStartStopName()
            {
                return srcStopInfo.Name;
            }

            /// <summary>
            /// Gets the name of the stop where the transfer ends
            /// </summary>
            /// <returns></returns>
            public string GetEndStopName()
            {
                return destStopInfo.Name;
            }
        }


        /// <summary>
        /// Class representing a used bike trip in a found connection
        /// </summary>
        public class UsedBikeTrip : UsedSegment
        {
            /// <summary>
            /// The information about the source station of the bike trip
            /// </summary>
            [JsonInclude]
            public RoutePointInfo srcStopInfo { get; set; }
            /// <summary>
            /// The information about the destination station of the bike trip
            /// </summary>
            [JsonInclude]
            public RoutePointInfo destStopInfo { get; set; }
            /// <summary>
            /// The distance of the bike trip in meters
            /// </summary>
            [JsonInclude]
            public int distance { get; set; }
            /// <summary>
            /// The time it takes to complete the bike trip in seconds
            /// </summary>
            [JsonInclude]
            public int time { get; set; }

            [JsonInclude]
            public int remainingBikes { get; set; }

            /// <summary>
            /// Creates a new UsedBikeTrip object
            /// </summary>
            /// <param name="srcStopInfo">The info about the trip source station</param>
            /// <param name="destStopInfo">The info about the trip destination station</param>
            /// <param name="distance">The length of the trip in meters</param>
            /// <param name="time">The length of the trip in seconds</param>
            public UsedBikeTrip(
                RoutePointInfo srcStopInfo,
                RoutePointInfo destStopInfo,
                int distance,
                int time,
                int remainingBikes
            ){
                this.srcStopInfo = srcStopInfo;
                this.destStopInfo = destStopInfo;
                this.distance = distance;
                this.time = time;
                this.remainingBikes = remainingBikes;
            }

            public override string ToString()
            {
                return "BIKE from " + GetStartStopName() + " to " + GetEndStopName() + ", time: " + time + " = length: " + distance + "m";
            }

            /// <summary>
            /// Gets the name of the stop where the bike trip begins
            /// </summary>
            /// <returns></returns>
            public string GetStartStopName()
            {
                return srcStopInfo.Name;
            }

            /// <summary>
            /// Gets the name of the stop where the bike trip ends
            /// </summary>
            /// <returns></returns>
            public string GetEndStopName()
            {
                return destStopInfo.Name;
            }
        }
    }
}

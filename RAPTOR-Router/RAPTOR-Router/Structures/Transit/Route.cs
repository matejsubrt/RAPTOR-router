using System.Security.Cryptography.X509Certificates;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Structures.Generic;

namespace RAPTOR_Router.Structures.Transit
{
    /// <summary>
    /// Class representing an unique public transit route
    /// </summary>
    /// <remarks>There is a different instance for each variation of one "normal" route - i.e. tram trips returning to depot, ...</remarks>
    public class Route
    {
        /// <summary>
        /// The Id of this unique route - combination of the GTFSId and the stop Id's of the unique route. Is unique for every Route object.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The GTFS Id of the GTFS Route entry associated with the unique route. 
        /// Is NOT unique for every Route object - for example a single tram line (single GTFS Route) can have multiple unique Routes associated with it (trams returning to depot/coming out of it, shortened trips, ...)
        /// </summary>
        public string GTFSId { get; }

        /// <summary>
        /// The short name of the associated GTFS Route
        /// </summary>
        public string ShortName { get; }

        /// <summary>
        /// The long name of the associated GTFS Route
        /// </summary>
        public string LongName { get; }

        /// <summary>
        /// The type of the vehicle that serves the route (i.e. bus/tram/metro/...)
        /// </summary>
        public VehicleType Type { get; }

        /// <summary>
        /// The color that should be used for the route
        /// </summary>
        public Color Color { get; }

        /// <summary>
        /// The list of all stops on the route in correct order on the route
        /// </summary>
        public List<Stop> RouteStops { get; set; } = new();

        /// <summary>
        /// A dictionary, which for every date when the Route has at least one operating trip contains the list of trips that operate on the day. This is necessary, as a trip does NOT contain information about the day, only about time.
        /// </summary>
        public Dictionary<DateOnly, List<Trip>> RouteTrips { get; set; } = new();

        /// <summary>
        /// Creates a new Route object
        /// </summary>
        /// <param name="id">The unique Route Id to use (Should be unique for each Route object)</param>
        /// <param name="gtfsId">The Id of the associated GTFS route (does not have to be unique for each Route object)</param>
        /// <param name="shortName">The short name of the route</param>
        /// <param name="longName">The long name of the route</param>
        public Route(string id, string gtfsId, string shortName, string longName)
        {
            Id = id;
            GTFSId = gtfsId;
            ShortName = shortName;
            LongName = longName;
        }

        /// <summary>
        /// Creates a new Route object
        /// </summary>
        /// <param name="id">The unique Route Id to use (Should be unique for each Route object)</param>
        /// <param name="gtfsRoute">The GTFSRoute object, from which the Route should be constructed</param>
        public Route(string id, GTFSRoute gtfsRoute)
        {
            Id = id;
            GTFSId = gtfsRoute.Id;
            ShortName = gtfsRoute.ShortName;
            LongName = gtfsRoute.LongName;
            Type = (VehicleType)gtfsRoute.Type;
            Color = new Color(gtfsRoute.Color);
        }

        /// <summary>
        /// Finds the first index of a specified stop in the route's list of stops
        /// </summary>
        /// <param name="stop">The stop to find the index of</param>
        /// <returns>The first index of the stop in the stops list</returns>
        /// <exception cref="InvalidOperationException">Thrown if stop is not found in the stops list</exception>
        public int GetFirstStopIndex(Stop stop)
        {
            int res = RouteStops.IndexOf(stop);
            if (res == -1)
            {
                throw new InvalidOperationException("Stop not found");
            }
            return res;
        }

        /// <summary>
        /// Finds the last index of a specified stop in the route's list of stops
        /// </summary>
        /// <param name="stop">The stop to find the index of</param>
        /// <returns>The last index of the stop in the stops list</returns>
        /// <exception cref="InvalidOperationException">Thrown if stop is not found in the stops list</exception>
        public int GetLastStopIndex(Stop stop)
        {
            int res = RouteStops.LastIndexOf(stop);
            if (res == -1)
            {
                throw new InvalidOperationException("Stop not found");
            }
            return res;
        }


        public List<DateTime> GetFirstNTripTimesAtStop(Stop stop, DateTime dateTime, int dateTimeOffsetSeconds, int count, bool forward)
        {
            DateOnly date = DateOnly.FromDateTime(dateTime);
            DateOnly prevDate = date.AddDays(-1);
            DateOnly nextDate = date.AddDays(1);

            List<Trip> tripsOnDate = RouteTrips.ContainsKey(date) ? RouteTrips[date] : new();
            List<Trip> tripsOnPrevDate = RouteTrips.ContainsKey(prevDate) ? RouteTrips[prevDate] : new();
            List<Trip> tripsOnNextDate = RouteTrips.ContainsKey(nextDate) ? RouteTrips[nextDate] : new();


            int stopIndex = forward ? GetFirstStopIndex(stop) : GetLastStopIndex(stop);

            List<DateTime> tripTimes = new();

            if (forward)
            {
                IEnumerable<Trip> allTrips = TripsOnAllDatesInOrder(new[] { tripsOnPrevDate, tripsOnDate, tripsOnNextDate });
                foreach (Trip trip in allTrips)
                {
                    DateTime tripTime = trip.GetDepartureDateTime(stopIndex, date).AddSeconds(-dateTimeOffsetSeconds);

                    if (tripTime > dateTime)
                    {
                        tripTimes.Add(tripTime);
                    }

                    if (tripTimes.Count >= count)
                    {
                        break;
                    }
                }
            }
            else
            {
                IEnumerable<Trip> allTrips = TripsOnAllDatesInReverseOrder(new[] { tripsOnPrevDate, tripsOnDate });
                foreach (Trip trip in allTrips)
                {
                    DateTime tripTime = trip.GetArrivalDateTime(stopIndex, date).AddSeconds(dateTimeOffsetSeconds);

                    if (tripTime < dateTime)
                    {
                        tripTimes.Add(tripTime);
                    }

                    if (tripTimes.Count >= count)
                    {
                        break;
                    }
                }
            }

            return tripTimes;



            IEnumerable<Trip> TripsOnAllDatesInOrder(IList<Trip>[] tripsOnDates)
            {
                foreach (var tripsOnDate in tripsOnDates)
                {
                    foreach(Trip trip in tripsOnDate)
                    {
                        yield return trip;
                    }
                }
            }
            IEnumerable<Trip> TripsOnAllDatesInReverseOrder(IList<Trip>[] tripsOnDates)
            {
                for (int i = tripsOnDates.Length - 1; i >= 0; i--)
                {
                    foreach (Trip trip in tripsOnDates[i].Reverse())
                    {
                        yield return trip;
                    }
                }
            }
        }


        /// <summary>
        /// For the given stop and a time range, finds all regular departure/arrival times of trips at the stop within the range
        /// </summary>
        /// <param name="stop">The stop</param>
        /// <param name="rangeStart">The start of the time range</param>
        /// <param name="rangeEnd">The end of the time range</param>
        /// <param name="forward">Whether the search is performed forward (-> departure times) or backward (->arrival times)</param>
        /// <returns>List of all times within the time range at which a trip departs/arrives at the stop</returns>
        /// <exception cref="ArgumentException">Thrown if the range is invalid or too long</exception>
        public List<DateTime> GetTripTimesAtStopWithinRange(Stop stop, DateTime rangeStart, DateTime rangeEnd, bool forward)
        {
            if (rangeStart >= rangeEnd)
            {
                throw new ArgumentException("rangeStart must be before rangeEnd");
            }

            if (rangeStart.AddHours(6) < rangeEnd)
            {
                throw new ArgumentException("Range is too long. Max length is 6 hours");
            }

            DateOnly rangeStartDate = DateOnly.FromDateTime(rangeStart);
            DateOnly rangeEndDate = DateOnly.FromDateTime(rangeEnd);
            DateOnly prevDate = rangeStartDate.AddDays(-1);
            DateOnly nextDate = rangeStartDate.AddDays(1);

            List<Trip> tripsOnRangeStartDate = RouteTrips.ContainsKey(rangeStartDate) ? RouteTrips[rangeStartDate] : new();
            List<Trip> tripsOnPrevDate = RouteTrips.ContainsKey(prevDate) ? RouteTrips[prevDate] : new();
            List<Trip> tripsOnNextDate = (RouteTrips.ContainsKey(nextDate) && rangeEndDate > rangeStartDate) ? RouteTrips[nextDate] : new();

            int stopIndex = forward ? GetFirstStopIndex(stop) : GetLastStopIndex(stop);

            List<DateTime> tripTimes = new();


            foreach (Trip trip in tripsOnPrevDate)
            {
                DateTime tripTime = forward ? trip.GetDepartureDateTime(stopIndex, prevDate) : trip.GetArrivalDateTime(stopIndex, prevDate);
                if (tripTime >= rangeStart && tripTime <= rangeEnd)
                {
                    tripTimes.Add(tripTime);
                }
            }
            foreach (Trip trip in tripsOnRangeStartDate)
            {
                DateTime tripTime = forward ? trip.GetDepartureDateTime(stopIndex, rangeStartDate) : trip.GetArrivalDateTime(stopIndex, rangeStartDate);//trip.StopTimes[stopIndex].GetDepartureDateTime(rangeStartDate);
                if (tripTime >= rangeStart && tripTime <= rangeEnd)
                {
                    tripTimes.Add(tripTime);
                }
            }
            foreach (Trip trip in tripsOnNextDate)
            {
                // TODO: can there be a trip on nextDate that is before rangeEnd?
                DateTime tripTime = forward ? trip.GetDepartureDateTime(stopIndex, nextDate) : trip.GetArrivalDateTime(stopIndex, nextDate);//trip.StopTimes[stopIndex].GetDepartureDateTime(nextDate);
                if (tripTime >= rangeStart && tripTime <= rangeEnd)
                {
                    tripTimes.Add(tripTime);
                }
            }
            //TODO: code repetition

            return tripTimes;
        }


        
        private Trip? GetEarliestTripDepartingAfterTimeAtStop(Stop stop, DateTime dateTime, DelayModel delayModel, out DateOnly tripStartDate)
        {
            var stopIndex = GetFirstStopIndex(stop);

            var baseDate = DateOnly.FromDateTime(dateTime);

            var prevDate = baseDate.AddDays(-1);
            var followingDate = baseDate.AddDays(1);

            var trip = ProcessDate(prevDate);
            if(trip is not null)
            {
                tripStartDate = prevDate;
                return trip;
            }

            trip = ProcessDate(baseDate);
            if(trip is not null)
            {
                tripStartDate = baseDate;
                return trip;
            }

            trip = ProcessDate(followingDate);
            if(trip is not null)
            {
                tripStartDate = followingDate;
                return trip;
            }


            tripStartDate = new DateOnly();
            return null;

            Trip? ProcessDate(DateOnly date){
                if (RouteTrips.TryGetValue(date, out List<Trip>? tripsOnDate))
                {
                    foreach (Trip trip in tripsOnDate)
                    {
                        var regularDepartureTime = trip.GetDepartureDateTime(stopIndex, date);


                        DateTime actualDepartureTime;
                        if (regularDepartureTime.AddHours(2) < dateTime)
                        {
                            // do not try adding delay
                            actualDepartureTime = regularDepartureTime;
                        }
                        else
                        {
                            bool hasDelayData = delayModel.TryGetDelay(date, trip.Id, stopIndex, out int arrivalDelay,
                                out int departureDelay);
                            int delayOnStop = hasDelayData ? departureDelay : 0;

                            actualDepartureTime = regularDepartureTime.AddSeconds(delayOnStop);
                        }


                        if (actualDepartureTime >= dateTime)
                        {
                            return trip;
                        }
                    }
                }

                return null;
            }
        }

        private Trip? GetLatestTripArrivingBeforeTimeAtStop(Stop stop, DateTime dateTime, DelayModel delayModel, out DateOnly tripStartDate)
        {
            
            var stopIndex = GetLastStopIndex(stop);

            var baseDate = DateOnly.FromDateTime(dateTime);
            var prevDate = baseDate.AddDays(-1);

            var trip = ProcessDate(baseDate);
            if (trip is not null)
            {
                tripStartDate = baseDate;
                return trip;
            }

            trip = ProcessDate(prevDate);
            if (trip is not null)
            {
                tripStartDate = prevDate;
                return trip;
            }

            tripStartDate = new DateOnly();
            return null;

            Trip? ProcessDate(DateOnly date)
            {
                if (RouteTrips.TryGetValue(date, out List<Trip>? tripsOnDate))
                {
                    for (int i = tripsOnDate.Count - 1; i >= 0; i--)
                    {
                        var trip = tripsOnDate[i];

                        var regularArrivalTime = trip.GetArrivalDateTime(stopIndex, date);

                        bool hasDelayData = delayModel.TryGetDelay(date, trip.Id, stopIndex, out int arrivalDelay, out int departureDelay);
                        int delayOnStop = hasDelayData ? arrivalDelay : 0;

                        DateTime actualArrivalTime = regularArrivalTime.AddSeconds(delayOnStop);

                        if (actualArrivalTime <= dateTime)
                        {
                            return trip;
                        }
                    }
                }

                return null;
            }
        }


        /// <summary>
        /// For the given direction, stop and time, finds the first trip that can be transferred to/from at the stop at the given time
        /// </summary>
        /// <param name="forward">Whether the search runs forward</param>
        /// <param name="stop">The stop</param>
        /// <param name="dateTime">The time</param>
        /// <param name="delayModel">The delay model</param>
        /// <param name="tripStartDate">The start date of the trip, if a trip was found</param>
        /// <returns>The first transferable trip, null if none exists</returns>
        public Trip? GetFirstTransferableTripAtStopByReachTime(bool forward, Stop stop, DateTime dateTime, DelayModel delayModel, out DateOnly tripStartDate)
        {
            return forward ?
                GetEarliestTripDepartingAfterTimeAtStop(stop, dateTime, delayModel, out tripStartDate) :
                GetLatestTripArrivingBeforeTimeAtStop(stop, dateTime, delayModel, out tripStartDate);
        }

        /// <summary>
        /// Creates a string representation of the route object
        /// </summary>
        /// <returns>The string representation</returns>
        public override string ToString()
        {
            return ShortName + ": From " + RouteStops[0] + " to " + RouteStops[^1];
        }
    }
}

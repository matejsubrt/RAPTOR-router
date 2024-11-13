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

        public List<DateTime> GetTripTimesAtStopWithinRange(Stop stop, DateTime rangeStart, DateTime rangeEnd,
            DelayModel delayModel, bool departure)
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

            int stopIndex = departure ? GetFirstStopIndex(stop) : GetLastStopIndex(stop);

            List<DateTime> tripTimes = new();


            foreach (Trip trip in tripsOnPrevDate)
            {
                DateTime tripTime = trip.StopTimes[stopIndex].GetDepartureDateTime(prevDate);
                if (tripTime >= rangeStart && tripTime <= rangeEnd)
                {
                    tripTimes.Add(tripTime);
                }
            }
            foreach (Trip trip in tripsOnRangeStartDate)
            {
                DateTime tripTime = trip.StopTimes[stopIndex].GetDepartureDateTime(rangeStartDate);
                if (tripTime >= rangeStart && tripTime <= rangeEnd)
                {
                    tripTimes.Add(tripTime);
                }
            }
            foreach (Trip trip in tripsOnNextDate)
            {
                DateTime tripTime = trip.StopTimes[stopIndex].GetDepartureDateTime(nextDate);
                if (tripTime >= rangeStart && tripTime <= rangeEnd)
                {
                    tripTimes.Add(tripTime);
                }
            }

            return tripTimes;
        }


        
        public Trip GetEarliestTripDepartingAfterTimeAtStop(Stop stop, DateTime dateTime, DelayModel delayModel, out DateOnly tripStartDate)
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

            Trip ProcessDate(DateOnly date){
                if (RouteTrips.TryGetValue(date, out List<Trip> tripsOnDate))
                {
                    foreach (Trip trip in tripsOnDate)
                    {
                        var stopTime = trip.StopTimes[stopIndex];
                        var regularDepartureTime = stopTime.GetDepartureDateTime(date);


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

        public Trip GetLatestTripArrivingBeforeTimeAtStop(Stop stop, DateTime dateTime, DelayModel delayModel, out DateOnly tripStartDate)
        {
            var stopIndex = GetLastStopIndex(stop);

            var baseDate = DateOnly.FromDateTime(dateTime);
            var tripStartDatesToSearch = new List<DateOnly> { baseDate, baseDate.AddDays(-1) };


            foreach (DateOnly date in tripStartDatesToSearch)
            {
                if (RouteTrips.TryGetValue(date, out List<Trip> tripsOnDate))
                {
                    for (int i = tripsOnDate.Count - 1; i >= 0; i--)
                    {
                        var trip = tripsOnDate[i];

                        var stopTime = trip.StopTimes[stopIndex];
                        var regularArrivalTime = stopTime.GetArrivalDateTime(date);
                        bool hasDelayData = delayModel.TryGetDelay(date, trip.Id, stopIndex, out int arrivalDelay, out int departureDelay);
                        int delayOnStop = hasDelayData ? arrivalDelay : 0;

                        DateTime actualArrivalTime = regularArrivalTime.AddSeconds(delayOnStop);

                        if (actualArrivalTime <= dateTime)
                        {
                            tripStartDate = date;
                            return trip;
                        }
                    }
                }
            }

            tripStartDate = new DateOnly();
            return null;
        }

        public Trip GetFirstTransferableTripAtStopByReachTimeBeta(bool forward, Stop stop, DateTime dateTime, DelayModel delayModel, out DateOnly tripStartDate)
        {
            return forward ?
                GetEarliestTripDepartingAfterTimeAtStop(stop, dateTime, delayModel, out tripStartDate) :
                GetLatestTripArrivingBeforeTimeAtStop(stop, dateTime, delayModel, out tripStartDate);
        }

        public override string ToString()
        {
            return ShortName + ": From " + RouteStops[0] + " to " + RouteStops[RouteStops.Count - 1];
        }


        /// <summary>
        /// Enum representing the type of the vehicle that serves the route
        /// </summary>
        public enum VehicleType
        {
            TRAM = 0,                       // Tram, Streetcar, Light rail
            METRO = 1,                      // Subway, Metro
            RAIL = 2,                       // Rail (Intercity or long-distance travel)
            BUS = 3,                        // Bus (Short- and long-distance routes)
            FERRY = 4,                      // Ferry
            CABLE_TRAM = 5,                 // Cable tram
            AERIAL_LIFT = 6,                // Aerial lift, suspended cable car
            FUNICULAR = 7,                  // Funicular
            TROLLEYBUS = 11,                // Trolleybus
            MONORAIL = 12                   // Monorail
        }
    }
}

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
        

        public Trip GetFirstTransferableTripAtStopByReachTime(bool forward, Stop stop, DateOnly date, TimeOnly time,
            DateTime worstAllowedReachTime, DelayModel delayModel, out DateOnly tripDate)
        {
            return forward ?
                GetEarliestTripDepartingAfterTimeAtStop(stop, date, time, worstAllowedReachTime, delayModel, out tripDate) :
                GetLatestTripArrivingBeforeTimeAtStop(stop, date, time, worstAllowedReachTime, delayModel, out tripDate);
        }


        //TODO: make private
        /// <summary>
        /// Finds the earliest trip serving the route at the specified stop leaving after the specified time
        /// </summary>
        /// <param name="stop">The stop to find the earliest trip from</param>
        /// <param name="date">The earliest possible date of the trip</param>
        /// <param name="time">The earliest possible time of the trip</param>
        /// <param name="maxDaysAfter">The maximum number of days between the specified earliest time and the trip departure time</param>
        /// <param name="tripDate">The date on which the trip actually leaves -> if the first found trip is after midnight, this date is different than the date input parameter</param>
        /// <returns>The earliest trip, that leaves the stop after the specified time on the route, null if no trip is found</returns>
        public Trip GetEarliestTripDepartingAfterTimeAtStop(Stop stop, DateOnly date, TimeOnly time, DateTime worstAllowedReachTime, DelayModel delayModel, out DateOnly tripDate)
        {
            int stopIndex = GetFirstStopIndex(stop);
            DateOnly currDate = date;
            DateOnly maxDate = DateOnly.FromDateTime(worstAllowedReachTime);
            //DateOnly maxDate = date.AddDays(maxDaysAfter);

            List<Trip> tripsOnDate;
            if (RouteTrips.ContainsKey(currDate))
            {
                tripsOnDate = RouteTrips[currDate];

                //TimeOnly departureTime;


                Trip firstTripOnDay = tripsOnDate[0];
                TimeOnly firstTripDepartureTime = firstTripOnDay.StopTimes[stopIndex].DepartureTime;

                // if first trip of the day already crosses into the next day, immediately return it
                if (firstTripDepartureTime < firstTripOnDay.StopTimes[0].DepartureTime)
                {
                    tripDate = currDate.AddDays(1);
                    return firstTripOnDay;
                }



                // if first trip of the day already is after the specified time, we need to check trips that depart before midnight on the previous day
                if (firstTripDepartureTime >= time)
                {
                    List<Trip> tripsOnPreviousDay;
                    if (RouteTrips.TryGetValue(currDate.AddDays(-1), out tripsOnPreviousDay))
                    {
                        int i = tripsOnPreviousDay.Count - 1;
                        TimeOnly srcDepartureTime = tripsOnPreviousDay[i].StopTimes[0].DepartureTime;
                        TimeOnly departureTime = tripsOnPreviousDay[i].StopTimes[stopIndex].DepartureTime;
                        Trip? bestTrip = null;
                        while (i >= 0 && departureTime < srcDepartureTime && departureTime >= time)
                        {
                            // The trip goes over midnight into the day we reached the stop AND it departs from it after its reach time
                            bestTrip = tripsOnPreviousDay[i];

                            i--;

                            srcDepartureTime = tripsOnPreviousDay[i].StopTimes[0].DepartureTime;
                            departureTime = tripsOnPreviousDay[i].StopTimes[stopIndex].DepartureTime;
                        }

                        if (bestTrip is not null)
                        {
                            tripDate = currDate.AddDays(-1);
                            return bestTrip;
                        }
                    }
                }



                TimeOnly regularDepartureTime;








                //Scan the first day for trips leaving after specified time
                for (int i = 0; i < tripsOnDate.Count; i++)
                {
                    //departureTime = tripsOnDate[i].StopTimes[stopIndex].DepartureTime;

                    regularDepartureTime = tripsOnDate[i].StopTimes[stopIndex].DepartureTime;
                    bool hasDelayData = delayModel.TryGetDelay(currDate, tripsOnDate[i].Id, stopIndex, out int arrivalDelay, out int departureDelay);
                    int delayOnStop = hasDelayData ? departureDelay : 0;
                    TimeOnly actualDepartureTime = regularDepartureTime.AddSeconds(delayOnStop);



                    if (actualDepartureTime < tripsOnDate[i].StopTimes[0].DepartureTime)
                    {
                        tripDate = currDate.AddDays(1);
                        return tripsOnDate[i];
                    }

                    if (actualDepartureTime >= time)
                    {
                        tripDate = currDate;
                        return tripsOnDate[i];
                    }
                }
            }




            //scan the following days till maxDay and select first available trip
            while (currDate < maxDate)
            {
                currDate = currDate.AddDays(1);

                if (RouteTrips.ContainsKey(currDate) && RouteTrips[currDate].Count > 0)
                {
                    tripDate = currDate;
                    return RouteTrips[currDate][0];
                }
            }
            //No trip found in the specified timeframe
            tripDate = new DateOnly();
            return null;
        }

        /// <summary>
        /// Finds the latest trip serving the route at the specified stop arriving before the specified time
        /// </summary>
        /// <param name="stop">The stop to find the latest trip to</param>
        /// <param name="date">The latest possible date of the trip</param>
        /// <param name="time">The latest possible time of the trip</param>
        /// <param name="maxDaysBefore">The maximum number of days between the specified latest time and the trip arrival time</param>
        /// <param name="tripDate">The date on which the trip actually arrives -> if the first found trip is before midnight, this date is different than the date input parameter</param>
        /// <returns>The latest trip, that arrives at the stop before the specified time on the route, null if no trip is found</returns>
        public Trip GetLatestTripArrivingBeforeTimeAtStop(Stop stop, DateOnly date, TimeOnly time, DateTime worstAllowedReachTime, DelayModel delayModel, out DateOnly tripDate)
        {
            int stopIndex = GetLastStopIndex(stop);
            DateOnly currDate = date;
            DateOnly minDate = DateOnly.FromDateTime(worstAllowedReachTime);
            //DateOnly minDate = date.AddDays(-maxDaysBefore);

            List<Trip> tripsOnDate;
            if (RouteTrips.ContainsKey(currDate))
            {
                tripsOnDate = RouteTrips[currDate];

                //TimeOnly arrivalTime;

                Trip lastTripOnDay = tripsOnDate[tripsOnDate.Count - 1];
                TimeOnly lastTripArrivalTime = lastTripOnDay.StopTimes[stopIndex].ArrivalTime;

                // if last trip of the day already crosses into the previous day, immediately return it
                //if (lastTripArrivalTime < lastTripOnDay.StopTimes[0].DepartureTime)
                //{
                //    tripDate = currDate.AddDays(-1);
                //    return lastTripOnDay;
                //}



                // if last trip of the day already is before the specified time, we do not need to check trips that arrive after midnight on the next day, as they are already covered by the last trip of the day




                TimeOnly regularArrivalTime;







                //Scan the first day for trips arriving before specified time


                // We start with the last trip of the day and go backwards
                //TODO: check, if there could not be a trip that arrives at the stop after midnight, but is still the last trip of the day
                int lastTripArrivingAtStopBeforeMidnightIndex = tripsOnDate.Count - 1;
                var stopTimes1 = tripsOnDate[lastTripArrivingAtStopBeforeMidnightIndex].StopTimes;
                var firstStopArrivalTime = stopTimes1[0].ArrivalTime;
                var lastStopArrivalTime = stopTimes1[stopTimes1.Count - 1].ArrivalTime;
                while(firstStopArrivalTime > lastStopArrivalTime)
                {
                    lastTripArrivingAtStopBeforeMidnightIndex--;
                    if(lastTripArrivingAtStopBeforeMidnightIndex < 0)
                    {
                        break;
                    }
                    stopTimes1 = tripsOnDate[lastTripArrivingAtStopBeforeMidnightIndex].StopTimes;
                    firstStopArrivalTime = stopTimes1[0].ArrivalTime;
                    lastStopArrivalTime = stopTimes1[stopTimes1.Count - 1].ArrivalTime;
                }


                for (int i = lastTripArrivingAtStopBeforeMidnightIndex; i >= 0; i--)
                {
                    var stopTimes = tripsOnDate[i].StopTimes;
                    //arrivalTime = stopTimes[stopIndex].ArrivalTime;

                    regularArrivalTime = stopTimes[stopIndex].ArrivalTime;
                    bool hasDelayData = delayModel.TryGetDelay(currDate, tripsOnDate[i].Id, stopIndex, out int arrivalDelay, out int departureDelay);
                    int delayAtStop = hasDelayData ? arrivalDelay : 0;
                    TimeOnly actualArrivalTime = regularArrivalTime.AddSeconds(delayAtStop);




                    if (actualArrivalTime > stopTimes[stopTimes.Count - 1].ArrivalTime)
                    {
                        tripDate = currDate.AddDays(-1);
                        return tripsOnDate[i];
                    }

                    if (actualArrivalTime <= time)
                    {
                        tripDate = currDate;
                        return tripsOnDate[i];
                    }
                }
            }


            //scan the preceding days till minDay and select first available trip
            while (currDate > minDate)
            {
                currDate = currDate.AddDays(-1);

                if (RouteTrips.ContainsKey(currDate) && RouteTrips[currDate].Count > 0)
                {
                    tripDate = currDate;
                    // last trip of the preceding day
                    return RouteTrips[currDate][RouteTrips[currDate].Count - 1];
                }
            }
            //No trip found in the specified timeframe
            tripDate = new DateOnly();
            return null;
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

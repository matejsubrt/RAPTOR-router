﻿using RAPTOR_Router.GTFSParsing;

namespace RAPTOR_Router.Structures.Transit
{
    /// <summary>
    /// Class representing a unique public transit trip
    /// </summary>
    public class Trip
    {
        /// <summary>
        /// The route, on which the trip operates
        /// </summary>
        public Route Route;

        /// <summary>
        /// A list of all stop times on the trip. The indices correspond to the indices of the stops on the associated route - i.e. the stop time for the first stop is at the index 0
        /// </summary>
        public List<StopTime> StopTimes;

        /// <summary>
        /// The unique id of the trip
        /// </summary>
        public string Id;

        /// <summary>
        /// Creates a new Trip object
        /// </summary>
        /// <param name="gtfsTripStopTimes">A list of the GTFSStopTimes of the trip</param>
        /// <param name="route">The associated route on which the trip is operating</param>
        /// <param name="id">The unique id of the trip</param>
        public Trip(List<GTFSStopTime> gtfsTripStopTimes, Route route, string id)
        {
            Route = route;
            StopTimes = new();
            Id = id;

            byte daysSinceTripStart = 0;
            var lastStopTime = gtfsTripStopTimes[0].ArrivalTime; // TODO: CHECK change here
            foreach (GTFSStopTime gtfsTripStopTime in gtfsTripStopTimes)
            {
                byte daysAfterTripStartArrival;
                byte daysAfterTripStartDeparture;

                var arrTime = gtfsTripStopTime.ArrivalTime;
                var depTime = gtfsTripStopTime.DepartureTime;


                if (arrTime < lastStopTime)
                {
                    // we crossed midnight between last stop and this stop
                    daysSinceTripStart += 1;
                    daysAfterTripStartArrival = daysSinceTripStart;
                    daysAfterTripStartDeparture = daysSinceTripStart;
                }
                else if (depTime < arrTime)
                {
                    // we crossed midnight while stationary at this stop - between arrival and departure
                    daysAfterTripStartArrival = daysSinceTripStart;
                    daysSinceTripStart += 1;
                    daysAfterTripStartDeparture = daysSinceTripStart;
                }
                else
                {
                    // we did not cross midnight
                    daysAfterTripStartArrival = daysSinceTripStart;
                    daysAfterTripStartDeparture = daysSinceTripStart;
                }

                lastStopTime = depTime;
                StopTime stopTime = new StopTime(arrTime, depTime, daysAfterTripStartArrival, daysAfterTripStartDeparture);
                StopTimes.Add(stopTime);
            }
        }

        /// <summary>
        /// Returns a string representation of the trip
        /// </summary>
        /// <returns>The string representation</returns>
        public override string ToString()
        {
            return Route.ShortName + ": " + StopTimes[0].DepartureTime;
        }

        /// <summary>
        /// Compares 2 trips by their departure times from their first stop
        /// </summary>
        /// <param name="trip1">First trip</param>
        /// <param name="trip2">Second trip</param>
        /// <returns>1 if trip1 departureTime is later, 0 if equal, -1 if earlier</returns>
        public static int CompareTrips(Trip trip1, Trip trip2)
        {
            return trip1.StopTimes[0].DepartureTime.CompareTo(trip2.StopTimes[0].DepartureTime);
        }

        /// <summary>
        /// Gets the arrival time of the trip at the given stop
        /// </summary>
        /// <param name="stopIndex">The index of the stop</param>
        /// <param name="tripDate">The start date of the trip</param>
        /// <returns>The dateTime of arrival at the stop</returns>
        public DateTime GetArrivalDateTime(int stopIndex, DateOnly tripDate)
        {
            return StopTimes[stopIndex].GetArrivalDateTime(tripDate);
        }

        /// <summary>
        /// Gets the departure time of the trip at the given stop
        /// </summary>
        /// <param name="stopIndex">The index of the stop</param>
        /// <param name="tripDate">The start date of the trip</param>
        /// <returns>The dateTime of departure from the stop</returns>
        public DateTime GetDepartureDateTime(int stopIndex, DateOnly tripDate)
        {
            return StopTimes[stopIndex].GetDepartureDateTime(tripDate);
        }
    }
}

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

        public string Id;
        /// <summary>
        /// Creates a new Trip object
        /// </summary>
        /// <param name="gtfsTripStopTimes">A list of the GTFSStopTimes of the trip</param>
        /// <param name="route">The associated route on which the trip is operating</param>
        public Trip(List<GTFSStopTime> gtfsTripStopTimes, Route route, string id)
        {
            Route = route;
            StopTimes = new();
            Id = id;
            foreach (GTFSStopTime gtfsTripStopTime in gtfsTripStopTimes)
            {
                StopTime stopTime = new StopTime(gtfsTripStopTime.ArrivalTime, gtfsTripStopTime.DepartureTime);
                StopTimes.Add(stopTime);
            }
        }
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
    }
}

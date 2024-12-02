namespace RAPTOR_Router.Structures.Transit
{
    /// <summary>
    /// Struct representing a stop time of a single trip in a single stop
    /// </summary>
    public struct StopTime
    {
        /// <summary>
        /// The time, at which a trip arrives at the stop
        /// </summary>
        public TimeOnly ArrivalTime { get; set; }
        /// <summary>
        /// The time, at which the trip departs from the stop
        /// </summary>
        public TimeOnly DepartureTime { get; set; }

        /// <summary>
        /// Specifies how many days we need to add to the trip's start date to get the date of the arrival
        /// </summary>
        public byte DaysAfterTripStartArrival { get; set; }
        /// <summary>
        /// Specifies how many days we need to add to the trip's start date to get the date of the departure
        /// </summary>
        public byte DaysAfterTripStartDeparture { get; set; }


        /// <summary>
        /// Creates a new StopTime object
        /// </summary>
        /// <param name="arrivalTime">The arrival time</param>
        /// <param name="departureTime">The departure time</param>
        /// <param name="daysAfterTripStartArrival">The number of days after the trip start for the arrival</param>
        /// <param name="daysAfterTripStartDeparture">The number of days after the trip start for the departure</param>
        public StopTime(TimeOnly arrivalTime, TimeOnly departureTime, byte daysAfterTripStartArrival, byte daysAfterTripStartDeparture)
        {
            ArrivalTime = arrivalTime;
            DepartureTime = departureTime;
            DaysAfterTripStartArrival = daysAfterTripStartArrival;
            DaysAfterTripStartDeparture = daysAfterTripStartDeparture;
        }

        /// <summary>
        /// Returns a string representation of the stop time
        /// </summary>
        /// <returns>The string representation</returns>
        public override string ToString()
        {
            return "Arr: " + ArrivalTime.ToString() + ", Dep: " + DepartureTime.ToString();
        }

        /// <summary>
        /// Gets the arrival dateTime of the stop time
        /// </summary>
        /// <param name="tripStartDate">The date at which the trip started</param>
        /// <returns>The arrival dateTime</returns>
        public DateTime GetArrivalDateTime(DateOnly tripStartDate)
        {
            var date = tripStartDate.AddDays(DaysAfterTripStartArrival);
            return date.ToDateTime(ArrivalTime);
        }

        /// <summary>
        /// Gets the departure dateTime of the stop time
        /// </summary>
        /// <param name="tripStartDate">The date at which the trip started</param>
        /// <returns>The departure dateTime</returns>
        public DateTime GetDepartureDateTime(DateOnly tripStartDate)
        {
            var date = tripStartDate.AddDays(DaysAfterTripStartDeparture);
            return date.ToDateTime(DepartureTime);
        }
    }
}

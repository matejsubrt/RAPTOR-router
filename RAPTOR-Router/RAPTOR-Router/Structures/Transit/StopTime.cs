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

        public byte DaysAfterTripStartArrival { get; set; }
        public byte DaysAfterTripStartDeparture { get; set; }
        /// <summary>
        /// Creates a new StopTime object
        /// </summary>
        /// <param name="arrivalTime">The arrival time</param>
        /// <param name="departureTime">The departure time</param>

        public StopTime(TimeOnly arrivalTime, TimeOnly departureTime, byte daysAfterTripStartArrival, byte daysAfterTripStartDeparture)
        {
            ArrivalTime = arrivalTime;
            DepartureTime = departureTime;
            DaysAfterTripStartArrival = daysAfterTripStartArrival;
            DaysAfterTripStartDeparture = daysAfterTripStartDeparture;
        }
        public override string ToString()
        {
            return "Arr: " + ArrivalTime.ToString() + ", Dep: " + DepartureTime.ToString();
        }

        public DateTime GetArrivalDateTime(DateOnly tripStartDate)
        {
            var date = tripStartDate.AddDays(DaysAfterTripStartArrival);
            return date.ToDateTime(ArrivalTime);
        }

        public DateTime GetDepartureDateTime(DateOnly tripStartDate)
        {
            var date = tripStartDate.AddDays(DaysAfterTripStartDeparture);
            return date.ToDateTime(DepartureTime);
        }
    }
}

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
        /// Creates a new StopTime object
        /// </summary>
        /// <param name="arrivalTime">The arrival time</param>
        /// <param name="departureTime">The departure time</param>

        public StopTime(TimeOnly arrivalTime, TimeOnly departureTime)
        {
            ArrivalTime = arrivalTime;
            DepartureTime = departureTime;
        }
        public override string ToString()
        {
            return "Arr: " + ArrivalTime.ToString() + ", Dep: " + DepartureTime.ToString();
        }
    }
}

using RAPTOR_Router.Structures.Interfaces;

namespace RAPTOR_Router.Structures.Transit
{
    /// <summary>
    /// Class representing a foot transfer between two stops
    /// </summary>
    public class Transfer : ITransfer
    {
        /// <summary>
        /// The stop where the transfer begins
        /// </summary>
        public Stop From { get; }
        /// <summary>
        /// The stop where the transfer ends
        /// </summary>
        public Stop To { get; }
        /// <summary>
        /// The straight-line distance between the start and end of the transfer
        /// </summary>
        public int Distance { get; }
        /// <summary>
        /// A reference to the same transfer in the opposite direction
        /// </summary>
        public Transfer? OppositeTransfer { get; set; }

        /// <summary>
        /// Creates a new Transfer object
        /// </summary>
        /// <param name="from">The source stop of the transfer</param>
        /// <param name="to">The destination stop of the transfer</param>
        /// <param name="dist">The distance between the stops</param>
        public Transfer(Stop from, Stop to, int dist)
        {
            From = from;
            To = to;
            Distance = dist;
        }
        public override string ToString()
        {
            return "Transfer from " + From.Name + " to " + To.Name;
        }
        /// <summary>
        /// Gets the time it takes to make the transfer at the given walking pace
        /// </summary>
        /// <param name="walkingPace">The pace to use in min/km</param>
        /// <returns>The time in seconds</returns>
        public int GetTransferTime(int walkingPace)
        {
            return (int)(Distance / 1000.0 * walkingPace * 60);
        }

        /// <summary>
        /// Gets the source point of the transfer
        /// </summary>
        /// <returns></returns>
        public IRoutePoint GetSrcRoutePoint()
        {
            return From;
        }
        /// <summary>
        /// Gets the destination point of the transfer
        /// </summary>
        /// <returns></returns>
        public IRoutePoint GetDestRoutePoint()
        {
            return To;
        }
    }
}

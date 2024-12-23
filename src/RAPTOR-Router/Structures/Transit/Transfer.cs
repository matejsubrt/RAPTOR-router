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

        /// <summary>
        /// Returns a string representation of the transfer
        /// </summary>
        /// <returns>The string representation</returns>
        public override string ToString()
        {
            return "Transfer from " + From.Name + " to " + To.Name;
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

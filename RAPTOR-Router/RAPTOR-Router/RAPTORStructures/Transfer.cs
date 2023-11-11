using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.RAPTORStructures
{
    /// <summary>
    /// Class representing a foot transfer between two stops
    /// </summary>
    internal class Transfer
    {
        /// <summary>
        /// The stop where the transfer begins
        /// </summary>
        public Stop From { get; }
        /// <summary>
        /// The stop where the transfer ends
        /// </summary>
        public Stop To { get;}
        /// <summary>
        /// The straight-line distance between the start and end of the transfer
        /// </summary>
        public int Distance { get;}

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
        public int GetTransferTime(int walkingPace)
        {
            return (int)(Distance / 1000.0 * walkingPace * 60);
        }
    }
}

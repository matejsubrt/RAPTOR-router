using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Custom
{
    /// <summary>
    /// Class representing a transfer to/from a custom route point
    /// </summary>
    public class CustomTransfer : ITransfer
    {
        /// <summary>
        /// The length of the transfer in meters
        /// </summary>
        public int Distance { get; protected set; }

        /// <summary>
        /// The source route point
        /// </summary>
        public IRoutePoint From { get; }

        /// <summary>
        /// The destination route point
        /// </summary>
        public IRoutePoint To { get; }

        /// <summary>
        /// Creates a new CustomTransfer object
        /// </summary>
        /// <param name="srcRP">The source route point</param>
        /// <param name="destRP">The destination route point</param>
        /// <param name="dist">The distance of the transfer</param>
        public CustomTransfer(IRoutePoint srcRP, IRoutePoint destRP, int dist)
        {
            From = srcRP;
            To = destRP;
            Distance = dist;
        }

        /// <summary>
        /// Creates a string representation of the transfer
        /// </summary>
        /// <returns>The string representation</returns>
        public override string ToString()
        {
            return "Transfer from " + From.Name + " to " + To.Name;
        }

        /// <summary>
        /// Gets the source route point of the transfer
        /// </summary>
        /// <returns>The source route point</returns>
        public IRoutePoint GetSrcRoutePoint()
        {
            return From;
        }

        /// <summary>
        /// Gets the destination route point of the transfer
        /// </summary>
        /// <returns>The destination route point</returns>
        public IRoutePoint GetDestRoutePoint()
        {
            return To;
        }
    }
}

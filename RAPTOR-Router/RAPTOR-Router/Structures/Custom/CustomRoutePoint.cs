using RAPTOR_Router.Extensions;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Custom
{
    /// <summary>
    /// Class representing a custom route point - a point on the route that is not a stop or a station (typically the custom start or end point given by only its coordinates)
    /// </summary>
    public class CustomRoutePoint : IRoutePoint
    {
        /// <summary>
        /// The id of the route point
        /// </summary>
        public string Id { get; }
        /// <summary>
        /// The name of the route point
        /// </summary>
        public string Name { get; }
        /// <summary>
        ///  The coordinates of the route point
        /// </summary>
        public Coordinates Coords { get; }
        /// <summary>
        /// The list of all possible foot transfers from this route point
        /// </summary>
        public List<CustomTransfer> possibleTransfers { get; private set; } = new();

        /// <summary>
        /// Dictionary containing the distances to all other route points reachable from this route point via a transfer
        /// </summary>
        public Dictionary<IRoutePoint, int> transferDistances { get; private set; } = new();

        /// <summary>
        /// Creates a new CustomRoutePoint object
        /// </summary>
        /// <param name="id">The id to use</param>
        /// <param name="name">The name to use</param>
        /// <param name="coords">The coordinates to use</param>
        public CustomRoutePoint(string id, string name, Coordinates coords)
        {
            Id = id;
            Name = name;
            Coords = coords;
        }

        /// <summary>
        /// Adds a transfer that leads TO this route point
        /// </summary>
        /// <param name="rp">The RoutePoint from which the transfer leads to the custom RoutePoint</param>
        public void AddTransferToRoutePoint(IRoutePoint rp)
        {
            int distance = DistanceExtensions.DistanceBetween(this, rp);
            possibleTransfers.Add(new FromCustomTransfer(this, rp, distance));

            if (!transferDistances.ContainsKey(rp))
            {
                transferDistances.Add(rp, distance);
            }
        }
        /// <summary>
        /// Ads a transfer that leads FROM this route point
        /// </summary>
        /// <param name="rp">The RoutePoint to which the transfer leads from the custom RoutePoint</param>
        public void AddTransferFromRoutePoint(IRoutePoint rp)
        {
            int distance = DistanceExtensions.DistanceBetween(rp, this);
            possibleTransfers.Add(new ToCustomTransfer(rp, this, distance));

            if (!transferDistances.ContainsKey(rp))
            {
                transferDistances.Add(rp, distance);
            }
        }

        /// <summary>
        /// Gets the transfer from this route point to/from the given route point
        /// </summary>
        /// <param name="rp">The RoutePoint to get the transfer to/from</param>
        /// <returns>The found transfer, null if none found</returns>
        public CustomTransfer GetTransferWithNormalRP(IRoutePoint rp)
        {
            return possibleTransfers.Where(t => (t.GetSrcRoutePoint() == rp || t.GetDestRoutePoint() == rp)).First();
        }
    }
}

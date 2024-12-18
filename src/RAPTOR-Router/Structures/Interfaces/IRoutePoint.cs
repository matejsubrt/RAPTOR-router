using RAPTOR_Router.Structures.Generic;

namespace RAPTOR_Router.Structures.Interfaces
{
    /// <summary>
    /// An interface for a route point (Stop, BikeStation, CustomRoutePoint)
    /// </summary>
    public interface IRoutePoint
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
        /// The coordinates of the route point
        /// </summary>
        public Coordinates Coords { get; }
    }
}

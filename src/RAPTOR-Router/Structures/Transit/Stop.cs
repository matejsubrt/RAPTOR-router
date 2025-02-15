﻿using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Interfaces;

namespace RAPTOR_Router.Structures.Transit
{
    /// <summary>
    /// Class representing a unique stop
    /// </summary>
    /// <remarks>For one stop name there can be multiple stops for different vehicle types/directions</remarks>
    public class Stop : IRoutePoint
    {
        /// <summary>
        /// The unique Id of the stop (different even for each stop in a Node sharing the same name)
        /// </summary>
        public string Id { get; private set; }
        /// <summary>
        /// The human-friendly name of the stop
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The coordinates of the stop
        /// </summary>
        public Coordinates Coords { get; private set; }
        /// <summary>
        /// A list of all routes that contain the stop
        /// </summary>
        public List<Route> StopRoutes { get; private set; } = new List<Route>();
        /// <summary>
        /// A set of all possible transfers that can be made from the stop
        /// </summary>
        public HashSet<Transfer> Transfers { get; private set; } = new HashSet<Transfer>();
        /// <summary>
        /// List of all bike transfers that can be made from the stop
        /// </summary>
        public List<ToBikeTransfer> BikeTransfers { get; private set; } = new List<ToBikeTransfer>();

        /// <summary>
        /// Creates a new Stop object
        /// </summary>
        /// <param name="id">The unique Id of the stop</param>
        /// <param name="name">The name of the stop</param>
        /// <param name="lat">The latitude of the stop</param>
        /// <param name="lon">The longitude of the stop</param>
        public Stop(string id, string name, double lat, double lon)
        {
            Id = id;
            Name = name;
            //Lat = lat;
            //Lon = lon;
            Coords = new Coordinates(lat, lon);
        }

        /// <summary>
        /// Returns a string representation of the stop
        /// </summary>
        /// <returns>The string representation</returns>
        public override string ToString()
        {
            return Name + "  " + Id;
        }

        /// <summary>
        /// Adds a new possible bike transfer from the stop
        /// </summary>
        /// <param name="transfer">The transfer to add</param>
        public void AddBikeTransfer(ToBikeTransfer transfer)
        {
            BikeTransfers.Add(transfer);
        }
    }
}

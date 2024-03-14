using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Interfaces;

namespace RAPTOR_Router.Structures.Bike
{
    /// <summary>
    /// Class representing a single bike station
    /// </summary>
    public class BikeStation : IRoutePoint
    {
        /// <summary>
        /// The given id for the station
        /// </summary>
        public int LocalId { get; set; }
        /// <summary>
        /// The unique identifier of the station (as parsed from the API)
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// The name of the station
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The coordinates of the station
        /// </summary>
        public Coordinates Coords { get; private set; }
        /// <summary>
        /// The bike capacity of the station
        /// </summary>
        public int Capacity { get; set; }
        /// <summary>
        /// The current number of bikes at the station
        /// </summary>
        public int BikeCount { get; set; }
        /// <summary>
        /// The list of all possible foot transfers from this station
        /// </summary>
        public List<FromBikeTransfer> Transfers { get; private set; } = new List<FromBikeTransfer>();


        /// <summary>
        /// Creates a new BikeStation object
        /// </summary>
        /// <param name="id">The id of the station</param>
        /// <param name="name">The name of the station</param>
        /// <param name="lat">The latitude of the station</param>
        /// <param name="lon">The longitude of the station</param>
        /// <param name="capacity">The bike capacity of the station</param>
        /// <param name="localId">The given id for the station</param>
        public BikeStation(string id, string name, double lat, double lon, int capacity, int localId)
        {
            Id = id;
            Name = name;
            Coords = new Coordinates(lat, lon);
            Capacity = capacity;
            LocalId = localId;
        }

        public override string ToString()
        {
            return Name + ": BikeCount = " + BikeCount;
        }

        /// <summary>
        /// Finds out whether any bikes are available at the station
        /// </summary>
        /// <returns>Whether at least 1 bike is available</returns>
        public bool HasBikes()
        {
            return BikeCount > 0;
        }
        /// <summary>
        /// Adds a transfer from the station
        /// </summary>
        /// <param name="transfer">The transfer to add</param>
        public void AddTransfer(FromBikeTransfer transfer)
        {
            Transfers.Add(transfer);
        }
    }
}

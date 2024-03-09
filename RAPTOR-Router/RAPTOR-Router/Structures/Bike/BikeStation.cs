using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Bike
{
    public class BikeStation : IRoutePoint
    {
        public int LocalId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public Coordinates Coords { get; private set; }
        public int Capacity { get; set; }

        public int BikeCount { get; set; }
        public List<FromBikeTransfer> Transfers { get; private set; } = new List<FromBikeTransfer>();

        public BikeStation(string id, string name, double lat, double lon, int capacity, int localId)
        {
            Id = id;
            Name = name;
            //Lat = lat;
            //Lon = lon;
            Coords = new Coordinates(lat, lon);
            Capacity = capacity;
            LocalId = localId;
        }

        public override string ToString()
        {
            return Name + ": BikeCount = " + BikeCount;
        }

        public bool HasBikes()
        {
            return BikeCount > 0;
        }
        public void AddTransfer(FromBikeTransfer transfer)
        {
            Transfers.Add(transfer);
        }
    }
}

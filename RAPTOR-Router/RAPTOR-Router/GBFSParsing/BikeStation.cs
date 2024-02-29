using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.GBFSParsing
{
    public class BikeStation
    {
        public int LocalId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public int Capacity { get; set; }

        public int BikeCount { get; set; }

        public BikeStation(string id, string name, double lat, double lon, int capacity, int localId)
        {
            Id = id;
            Name = name;
            Lat = lat;
            Lon = lon;
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
    }
}

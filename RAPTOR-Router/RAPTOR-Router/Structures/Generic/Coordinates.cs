using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Generic
{
    public struct Coordinates
    {
        public double Lat { get; }
        public double Lon { get; }
        public Coordinates(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;
        }
    }
}

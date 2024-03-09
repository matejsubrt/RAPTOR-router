using RAPTOR_Router.Structures.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Interfaces
{
    public interface IRoutePoint
    {
        public string Id { get; }
        public string Name { get; }
        //public double Lat { get; }
        //public double Lon { get; }
        public Coordinates Coords { get; }
    }
}

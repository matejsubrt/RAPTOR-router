using RAPTOR_Router.Structures.Generic;

namespace RAPTOR_Router.Structures.Interfaces
{
    public interface IRoutePoint
    {
        public string Id { get; }
        public string Name { get; }
        public Coordinates Coords { get; }
    }
}

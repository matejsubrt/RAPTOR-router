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
    public class CustomRoutePoint : IRoutePoint
    {
        public string Id { get; }
        public string Name { get; }
        public Coordinates Coords { get; }
        public List<CustomTransfer> possibleTransfers { get; private set; } = new();

        public CustomRoutePoint(string id, string name, Coordinates coords)
        {
            Id = id;
            Name = name;
            Coords = coords;
        }

        public void AddTransferToRoutePoint(IRoutePoint rp)
        {
            int distance = DistanceExtensions.DistanceBetween(this, rp);
            possibleTransfers.Add(new FromCustomTransfer(this, rp, distance));
        }
        public void AddTransferFromRoutePoint(IRoutePoint rp)
        {
            int distance = DistanceExtensions.DistanceBetween(rp, this);
            possibleTransfers.Add(new ToCustomTransfer(rp, this, distance));
        }
        public CustomTransfer GetTransferWithNormalRP(IRoutePoint rp)
        {
            return possibleTransfers.Where(t => (t.GetSrcRoutePoint() == rp || t.GetDestRoutePoint() == rp)).First();
        }
    }
}

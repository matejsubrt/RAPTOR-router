using RAPTOR_Router.Structures.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Structures.Generic
{
    public class ForbiddenCrossingPoint
    {
        public Coordinates Coords { get; }
        public int Id { get; }
        public ForbiddenCrossingPoint(Coordinates coords, int id)
        {
            Coords = coords;
            Id = id;
        }
    }
    public class ForbiddenCrossingLine
    {
        public ForbiddenCrossingPoint P1 { get; }
        public ForbiddenCrossingPoint P2 { get; }
        public string Comment { get; }
        public int Id { get; }
        public ForbiddenCrossingLine(ForbiddenCrossingPoint p1, ForbiddenCrossingPoint p2, int id, string comment)
        {
            P1 = p1;
            P2 = p2;
            Id = id;
            Comment = comment;
        }

        public bool IsCrossingForbidden(IRoutePoint rp1, IRoutePoint rp2)
        {
            return DoLinesIntersect(P1.Coords, P2.Coords, rp1.Coords, rp2.Coords);
        }

        private int Orientation(Coordinates p, Coordinates q, Coordinates r)
        {
            double val = (q.Lon - p.Lon) * (r.Lat - q.Lat) - (q.Lat - p.Lat) * (r.Lon - q.Lon);

            if (val == 0) return 0; // collinear
            return (val > 0) ? 1 : 2; // clock or counterclock wise
        }

        // The main function that returns true if line segment 'p1q1'
        // and 'p2q2' intersect.
        private bool DoLinesIntersect(Coordinates p1, Coordinates q1, Coordinates p2, Coordinates q2)
        {
            // Find the four orientations needed for general and special cases
            int o1 = Orientation(p1, q1, p2);
            int o2 = Orientation(p1, q1, q2);
            int o3 = Orientation(p2, q2, p1);
            int o4 = Orientation(p2, q2, q1);

            // General case
            if (o1 != o2 && o3 != o4)
                return true;

            return false; // Doesn't fall in any of the above cases
        }
    }
}

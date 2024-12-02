using RAPTOR_Router.Structures.Interfaces;

namespace RAPTOR_Router.Structures.Generic
{
    /// <summary>
    /// Class representing a forbidden crossing point. These points are used to create lines that no transfer can cross
    /// </summary>
    public class ForbiddenCrossingPoint
    {
        /// <summary>
        /// The coordinates of the point
        /// </summary>
        public Coordinates Coords { get; }

        /// <summary>
        /// The id of the point
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Creates a new ForbiddenCrossingPoint object
        /// </summary>
        /// <param name="coords">The coordinates of the point</param>
        /// <param name="id">The id of the point</param>
        public ForbiddenCrossingPoint(Coordinates coords, int id)
        {
            Coords = coords;
            Id = id;
        }
    }

    /// <summary>
    /// Class representing a forbidden crossing line, that no transfer can cross
    /// </summary>
    /// <remarks>Used in places, where there are pairs of stops which are close to each other, but there is no way of transfering (i.e. over rivers, railways, highways, ...)</remarks>
    public class ForbiddenCrossingLine
    {
        /// <summary>
        /// The first point of the line segment
        /// </summary>
        public ForbiddenCrossingPoint P1 { get; }

        /// <summary>
        /// The second point of the line segment
        /// </summary>
        public ForbiddenCrossingPoint P2 { get; }

        /// <summary>
        /// A comment describing the reason for the line existing
        /// </summary>
        public string Comment { get; }

        /// <summary>
        /// An id of the line
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Creates a new ForbiddenCrossingLine object
        /// </summary>
        /// <param name="p1">The first point</param>
        /// <param name="p2">The second point</param>
        /// <param name="id">The id of the line</param>
        /// <param name="comment">A comment describing the line</param>
        public ForbiddenCrossingLine(ForbiddenCrossingPoint p1, ForbiddenCrossingPoint p2, int id, string comment)
        {
            P1 = p1;
            P2 = p2;
            Id = id;
            Comment = comment;
        }

        /// <summary>
        /// For a pair of route points, checks if the line segment between them crosses this forbidden line
        /// </summary>
        /// <param name="rp1">The first route point</param>
        /// <param name="rp2">The second route point</param>
        /// <returns>Whether crossing between the 2 route points is forbidden due to crossing this line</returns>
        public bool IsCrossingForbidden(IRoutePoint rp1, IRoutePoint rp2)
        {
            return DoLinesIntersect(P1.Coords, P2.Coords, rp1.Coords, rp2.Coords);
        }

        // Given three points, p, q, r, the function checks if they are collinear (0), clockwise (1) or counterclockwise (2) in th order
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

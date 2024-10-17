using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Interfaces;

namespace RAPTOR_Router.Extensions
{
    internal static class ListExtensions
    {
        /// <summary>
        /// Extracts the list of stop ids from the specified list of GTFSStopTimes
        /// </summary>
        /// <param name="stopTimes">The list of GTFSStopTimes to be extracted from</param>
        /// <returns>List of the stop ids of stops present in the stop times list</returns>
        public static List<string> GetStopIds(this List<GTFSStopTime> stopTimes)
        {
            List<string> ids = new List<string>();
            foreach (var stopTime in stopTimes)
            {
                ids.Add(stopTime.StopId);
            }
            return ids;
        }
    }
    

    
    internal static class ForbiddenCrossingExtensions
    {
        /// <summary>
        /// Finds out, whether the line between the two RoutePoints is forbidden to cross - i.e. the list of forbidden lines contains a line, that intersects the line between the two points
        /// </summary>
        /// <param name="lines">The list of lines that are forbidden to cross</param>
        /// <param name="rp1">RoutePoint 1</param>
        /// <param name="rp2">RoutePoint2</param>
        /// <returns>Bool specifying whether transfer between the two stops is forbidden</returns>
        public static bool ForbidsTransferBetween(this List<ForbiddenCrossingLine> lines, IRoutePoint rp1, IRoutePoint rp2)
        {
            foreach (var line in lines)
            {
                if (line.IsCrossingForbidden(rp1, rp2))
                {
                    return true;
                }
            }
            return false;
        }
    }

    internal class IndexComparator
    {
        private readonly Func<int, int, bool> _precedesInSearchDirection;

        public IndexComparator(bool forward)
        {
            _precedesInSearchDirection = forward ?
                (a, b) => a < b :
                (a, b) => a > b;
        }

        public bool PrecedesInSearchDirection(int a, int b)
        {
            return _precedesInSearchDirection(a, b);
        }
    }
}

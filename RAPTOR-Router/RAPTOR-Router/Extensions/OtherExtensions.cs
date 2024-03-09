using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Interfaces;
using RAPTOR_Router.Structures.Transit;

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
}

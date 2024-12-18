using RAPTOR_Router.Structures.Bike;

namespace RAPTOR_Router.GBFSParsing.Distances
{
    /// <summary>
    /// A class representing the matrix of distances between the bike stations
    /// </summary>
    public class StationDistanceMatrix
    {
        private Dictionary<BikeStation, Dictionary<BikeStation, int>> distances = new();

        /// <summary>
        /// Adds a distance between two bike stations to the matrix
        /// </summary>
        /// <remarks>Note, that this function assumes the distances in opposite directions are equal, which is not neccessarily always true. It usually is a good approximation though and it saves time</remarks>
        /// <param name="s1">The first bike station</param>
        /// <param name="s2">The second bike station</param>
        /// <param name="distance">The distance in meters</param>
        public void AddDistance(BikeStation s1, BikeStation s2, int distance)
        {
            if (!distances.ContainsKey(s1))
            {
                distances.Add(s1, new Dictionary<BikeStation, int>());
            }
            distances[s1][s2] = distance;

            if (!distances.ContainsKey(s2))
            {
                distances.Add(s2, new Dictionary<BikeStation, int>());
            }
            distances[s2][s1] = distance;
        }
        
        /// <summary>
        /// Gets the distance between the two bike stations
        /// </summary>
        /// <param name="s1">The first bike station</param>
        /// <param name="s2">The second bike station</param>
        /// <returns>The distance between the two station in meters. -1 if they are not connected.</returns>
        public int GetDistance(BikeStation s1, BikeStation s2)
        {
            if (!distances.ContainsKey(s1))
            {
                return -1;
            }
            if (!distances[s1].ContainsKey(s2))
            {
                return -1;
            }
            return distances[s1][s2];
        }

        /// <summary>
        /// Finds out, whether the matrix contains the distance between the two bike stations
        /// </summary>
        /// <param name="s1">The first bike station</param>
        /// <param name="s2">The second bike station</param>
        /// <returns>Bool specifying whethet the matrix contains the distance</returns>
        public bool HasDistance(BikeStation s1, BikeStation s2)
        {
            if (!distances.ContainsKey(s1))
            {
                return false;
            }
            if (!distances[s1].ContainsKey(s2))
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Gets the distances from a bike station to all other bike stations
        /// </summary>
        /// <param name="station">The station for which to get all the distances to other stations</param>
        /// <returns>A dictionary indexed by the destination bike stations containing the distances as values</returns>
        public Dictionary<BikeStation, int> GetDistancesFromStation(BikeStation station)
        {
            return distances[station];
        }

        /// <summary>
        /// Adds a new distance matrix to the current one - used for merging the distances from different sources (bike systems)
        /// </summary>
        /// <param name="newDistances">The new distance matrix to be merged into the current one</param>
        public void MergeNewDistances(StationDistanceMatrix newDistances)
        {
            foreach (var newStation in newDistances.distances)
            {
                if (!distances.ContainsKey(newStation.Key))
                {
                    distances.Add(newStation.Key, new Dictionary<BikeStation, int>());
                }
                foreach (var newDistance in newStation.Value)
                {
                    distances[newStation.Key][newDistance.Key] = newDistance.Value;
                }
            }
        }
    }
}

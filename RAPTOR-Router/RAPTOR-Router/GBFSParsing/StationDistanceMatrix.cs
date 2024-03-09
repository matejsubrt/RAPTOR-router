using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.Structures.Bike;

namespace RAPTOR_Router.GBFSParsing
{
    public class StationDistanceMatrix
    {
        private Dictionary<BikeStation, Dictionary<BikeStation, int>> distances = new();

        public void AddDistance(BikeStation s1, BikeStation s2, int distance)
        {
            if(!distances.ContainsKey(s1))
            {
                distances.Add(s1, new Dictionary<BikeStation, int>());
            }
            distances[s1][s2] = distance;

            if(!distances.ContainsKey(s2))
            {
                distances.Add(s2, new Dictionary<BikeStation, int>());
            }
            distances[s2][s1] = distance;
        }

        public int GetDistance(BikeStation s1, BikeStation s2)
        {
            if(!distances.ContainsKey(s1))
            {
                return -1;
            }
            if (!distances[s1].ContainsKey(s2))
            {
                return -1;
            }
            return distances[s1][s2];
        }
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
        public Dictionary<BikeStation, int> GetDistancesFromStation(BikeStation station)
        {
            return distances[station];
        }

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

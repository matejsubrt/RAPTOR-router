using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.GBFSParsing
{
    public class StationDistanceMatrix
    {
        private Dictionary<string, Dictionary<string, int>> distances = new();

        public void AddDistance(BikeStation s1, BikeStation s2, int distance)
        {
            if(!distances.ContainsKey(s1.Id))
            {
                distances.Add(s1.Id, new Dictionary<string, int>());
            }
            distances[s1.Id][s2.Id] = distance;

            if(!distances.ContainsKey(s2.Id))
            {
                distances.Add(s2.Id, new Dictionary<string, int>());
            }
            distances[s2.Id][s1.Id] = distance;
        }

        public int GetDistance(BikeStation s1, BikeStation s2)
        {
            if(!distances.ContainsKey(s1.Id))
            {
                return -1;
            }
            if (!distances[s1.Id].ContainsKey(s2.Id))
            {
                return -1;
            }
            return distances[s1.Id][s2.Id];
        }
        public bool HasDistance(BikeStation s1, BikeStation s2)
        {
            if (!distances.ContainsKey(s1.Id))
            {
                return false;
            }
            if (!distances[s1.Id].ContainsKey(s2.Id))
            {
                return false;
            }
            return true;
        }
    }
}

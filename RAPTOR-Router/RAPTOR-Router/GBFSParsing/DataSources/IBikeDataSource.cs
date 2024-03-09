using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RAPTOR_Router.GBFSParsing.Distances;
using RAPTOR_Router.Structures.Bike;

namespace RAPTOR_Router.GBFSParsing.DataSources
{
    public interface IBikeDataSource
    {
        public void LoadStations();
        public void LoadStationDistances();
        public void UpdateStationStatus();

        public List<BikeStation> Stations { get; }
        public Dictionary<string, BikeStation> StationsById { get; }
        public StationDistanceMatrix Distances { get; }
    }
}

using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Interfaces;
using System.Timers;

using Timer = System.Timers.Timer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RAPTOR_Router.GBFSParsing.DataSources;
using RAPTOR_Router.GBFSParsing.Distances;
using RAPTOR_Router.Extensions;

namespace RAPTOR_Router.Models.Static
{
    public class BikeModel
    {

        public List<BikeStation> Stations { get; private set; }
        private Dictionary<string, BikeStation> StationsById;
        private StationDistanceMatrix Distances;
        private List<IBikeDataSource> bikeDataSources;

        private Timer statusUpdateTimer;

        //public BikeModel(List<BikeStation> stations, Dictionary<string, BikeStation> stationsById, StationDistanceMatrix distances)
        //{
        //    this.Stations = stations;
        //    this.StationsById = stationsById;
        //    this.Distances = distances;
        //}

        public BikeModel()
        {
            Stations = new();
            StationsById = new();
            Distances = new();
            bikeDataSources = new();


            statusUpdateTimer = new Timer(60000);
            statusUpdateTimer.Elapsed += UpdateAllStationStatus;
            statusUpdateTimer.AutoReset = true;
            statusUpdateTimer.Enabled = true;
        }

        public void AddDataSource(IBikeDataSource source)
        {
            if (Stations.Count == 0)
            {
                Stations = source.Stations;
                StationsById = source.StationsById;
                Distances = source.Distances;
            }
            else
            {
                Stations.AddRange(source.Stations);
                source.StationsById.ToList().ForEach(x => StationsById.Add(x.Key, x.Value));
                Distances.MergeNewDistances(source.Distances);
            }

            bikeDataSources.Add(source);
        }

        public void StartUpdateTimer()
        {
            foreach (IBikeDataSource dataSource in bikeDataSources)
            {
                dataSource.UpdateStationStatus();
            }
            statusUpdateTimer.Start();
        }

        public void UpdateAllStationStatus(object source, ElapsedEventArgs e)
        {
            foreach (IBikeDataSource dataSource in bikeDataSources)
            {
                dataSource.UpdateStationStatus();
            }
        }

        public Dictionary<BikeStation, int> GetDistancesFromStation(BikeStation station)
        {
            return Distances.GetDistancesFromStation(station);
        }

        public List<BikeStation> GetNearStations(double lat, double lon, int radius)
        {
            List<BikeStation> nearStations = new List<BikeStation>();
            foreach (BikeStation s in Stations)
            {
                // Skip stations that are too far away in one direction to speed up the calculation
                if (DistanceExtensions.TooFarInOneDirection(lat, lon, s.Coords.Lat, s.Coords.Lon, radius))
                {
                    continue;
                }
                if (DistanceExtensions.SimplifiedDistanceBetween(s.Coords.Lat, s.Coords.Lon, lat, lon) < radius)
                {
                    nearStations.Add(s);
                }
            }
            return nearStations;
        }
        public List<BikeStation> GetNearStations(IRoutePoint rp, int radius)
        {
            return GetNearStations(rp.Coords.Lat, rp.Coords.Lon, radius);
        }

        public BikeStation ResolveCoordinates(double lat, double lon, int radius)
        {
            int minDistance = int.MaxValue;
            BikeStation nearestStation = null;
            foreach (BikeStation s in Stations)
            {
                int distance = DistanceExtensions.SimplifiedDistanceBetween(s.Coords.Lat, s.Coords.Lon, lat, lon);
                if (distance < minDistance)
                {
                    nearestStation = s;
                    minDistance = distance;
                }
            }
            return nearestStation;
        }
    }
}

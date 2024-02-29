using RAPTOR_Router.GBFSParsing;
using RAPTOR_Router.RAPTORStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RAPTOR_Router.SearchModels
{
    public class BikeModel
    {
        static string stationStatusUrl = "https://gbfs.nextbike.net/maps/gbfs/v2/nextbike_tg/cs/station_status.json";

        private List<BikeStation> Stations;
        private Dictionary<string, BikeStation> StationsById;
        private StationDistanceMatrix Distances;

        public BikeModel(List<BikeStation> stations, Dictionary<string, BikeStation> stationsById, StationDistanceMatrix distances)
        {
            this.Stations = stations;
            this.StationsById = stationsById;
            this.Distances = distances;
        }

        public void UpdateStationStatus()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = client.GetAsync(stationStatusUrl).Result;
                    response.EnsureSuccessStatusCode();

                    GBFSStationStatus root = JsonSerializer.Deserialize<GBFSStationStatus>(response.Content.ReadAsStringAsync().Result);

                    foreach (GBFSSingleStationStatus station in root.Data.Stations)
                    {
                        BikeStation s = StationsById[station.StationId];
                        s.BikeCount = station.NumBikesAvailable;
                    }
                }
                catch (HttpRequestException e)
                {
                    // Handle any errors that occurred during the request
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }
        }

        public List<BikeStation> GetNearStations(double lat, double lon, int radius)
        {
            List<BikeStation> nearStations = new List<BikeStation>();
            foreach (BikeStation s in Stations)
            {
                // Skip stations that are too far away in one direction to speed up the calculation
                if(DistanceExtensions.TooFarInOneDirection(lat, lon, s.Lat, s.Lon, radius))
                {
                    continue;
                }
                if (DistanceExtensions.SimplifiedDistanceBetween(s.Lat, s.Lon, lat, lon) < radius)
                {
                    nearStations.Add(s);
                }
            }
            return nearStations;
        }

        public BikeStation ResolveCoordinates(double lat, double lon, int radius)
        {
            int minDistance = int.MaxValue;
            BikeStation nearestStation = null;
            foreach (BikeStation s in Stations)
            {
                int distance = DistanceExtensions.SimplifiedDistanceBetween(s.Lat, s.Lon, lat, lon);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RAPTOR_Router.GBFSParsing
{
    public interface IBikeDataSource
    {
        public void LoadStations(out List<BikeStation> stations, out Dictionary<string, BikeStation> stationsById, out StationDistanceMatrix distances);
        public void UpdateStationStatus(Dictionary<string, BikeStation> stationsById);
    }
    public class NextbikeDataSource : IBikeDataSource
    {
        static string stationInfoUrl = "https://gbfs.nextbike.net/maps/gbfs/v2/nextbike_tg/cs/station_information.json";
        static string stationStatusUrl = "https://gbfs.nextbike.net/maps/gbfs/v2/nextbike_tg/cs/station_status.json";


        public void LoadStations(out List<BikeStation> stations, out Dictionary<string, BikeStation> stationsById, out StationDistanceMatrix distances)
        {
            stations = new List<BikeStation>();
            stationsById = new Dictionary<string, BikeStation>();

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = client.GetAsync(stationInfoUrl).Result;
                    response.EnsureSuccessStatusCode();

                    GBFSStationInfo root = JsonSerializer.Deserialize<GBFSStationInfo>(response.Content.ReadAsStringAsync().Result);

                    int local_id = 0;
                    foreach (GBFSStation station in root.Data.Stations)
                    {
                        BikeStation newStation = new BikeStation(station.StationId, station.Name, station.Lat, station.Lon, station.Capacity, local_id);
                        stations.Add(newStation);
                        stationsById.Add(newStation.Id, newStation);
                        local_id++;
                    }
                }
                catch (HttpRequestException e)
                {
                    // Handle any errors that occurred during the request
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }


            BikeDistanceCalculator distanceCalculator = new BikeDistanceCalculator();
            distances = distanceCalculator.CalculateMatrix(stations, stationsById);
        }

        public void UpdateStationStatus(Dictionary<string, BikeStation> stationsById)
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
                        BikeStation s = stationsById[station.StationId];
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
    }
}

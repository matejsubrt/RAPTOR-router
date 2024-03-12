using RAPTOR_Router.GBFSParsing.Distances;
using RAPTOR_Router.GBFSParsing.GBFSStructures;
using RAPTOR_Router.Structures.Bike;
using System.Text.Json;

namespace RAPTOR_Router.GBFSParsing.DataSources
{
    public class NextbikeDataSource : IBikeDataSource
    {
        static string stationInfoUrl = "https://gbfs.nextbike.net/maps/gbfs/v2/nextbike_tg/cs/station_information.json";
        static string stationStatusUrl = "https://gbfs.nextbike.net/maps/gbfs/v2/nextbike_tg/cs/station_status.json";

        public List<BikeStation> Stations { get; private set; } = new();
        public Dictionary<string, BikeStation> StationsById { get; private set; } = new();
        public StationDistanceMatrix Distances { get; private set; } = new();


        public void LoadStationDistances()
        {
            BikeDistanceCalculator distanceCalculator = new BikeDistanceCalculator();
            Distances = distanceCalculator.CalculateMatrix(Stations, StationsById);
        }

        public void LoadStations()
        {

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
                        Stations.Add(newStation);
                        StationsById.Add(newStation.Id, newStation);
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
                        if (!StationsById.ContainsKey(station.StationId))
                        {
                            continue;
                        }
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
                catch(AggregateException e)
                {
                    Console.WriteLine("\nException Caught!");
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }
        }
    }
}

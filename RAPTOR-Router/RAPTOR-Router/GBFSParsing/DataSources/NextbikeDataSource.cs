using RAPTOR_Router.GBFSParsing.Distances;
using RAPTOR_Router.GBFSParsing.GBFSStructures;
using RAPTOR_Router.Structures.Bike;
using System.Text.Json;

namespace RAPTOR_Router.GBFSParsing.DataSources
{
    /// <summary>
    /// Data source for Nextbike bike sharing system
    /// </summary>
    public class NextbikeDataSource : IBikeDataSource
    {
        static string stationInfoUrl = "https://gbfs.nextbike.net/maps/gbfs/v2/nextbike_tg/cs/station_information.json";
        static string stationStatusUrl = "https://gbfs.nextbike.net/maps/gbfs/v2/nextbike_tg/cs/station_status.json";

        /// <summary>
        /// The list of all bike stations in the system
        /// </summary>
        public List<BikeStation> Stations { get; private set; } = new();
        /// <summary>
        /// A dictionary of bike stations indexed by their station id
        /// </summary>
        public Dictionary<string, BikeStation> StationsById { get; private set; } = new();
        /// <summary>
        /// The distance matrix between all bike stations
        /// </summary>
        public StationDistanceMatrix Distances { get; private set; } = new();

        /// <summary>
        /// Calculates all the distances between the bike stations and loads them into the distance matrix
        /// </summary>
        public void LoadStationDistances()
        {
            BikeDistanceCalculator distanceCalculator = new BikeDistanceCalculator();
            //Distances = distanceCalculator.CalculateMatrix(Stations, StationsById);
            Distances = distanceCalculator.GetDistanceMatrix(StationsById);
        }

        /// <summary>
        /// Loads all the static station data from the nextbike API
        /// </summary>
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

        /// <summary>
        /// Loads the current bike counts for all stations from the nextbike API
        /// </summary>
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

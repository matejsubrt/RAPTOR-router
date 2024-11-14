using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using Microsoft.Extensions.Configuration;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.Structures.Bike;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Route = Itinero.Route;

namespace RAPTOR_Router.GBFSParsing.Distances
{
    /// <summary>
    /// Represents the error, due to which a route between two bike stations could not be calculated
    /// </summary>
    public enum ErrorType
    {
        NO_ERROR,
        START_RESOLVE_ERROR,
        END_RESOLVE_ERROR,
        ROUTE_CALCULATION_ERROR
    }

    /// <summary>
    /// Class used for calculating the distances between bike stations
    /// </summary>
    public class BikeDistanceCalculator
    {
        private RouterDb routerDb;
        string osmLocation;
        string routerDbLocation;
        //string distanceFileLocation;
        //private string distanceDBFileLocation;
        //private BikeDistanceDatabase distanceDB;
        //StationDistanceMatrix distances = new StationDistanceMatrix();

        /// <summary>
        /// Loads the configuration info and creates a routerDb instance - either by creating a new one from the osm file, or by loading it from disk
        /// </summary>
        public BikeDistanceCalculator()
        {
            var basePath = Directory.GetCurrentDirectory();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                basePath = Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", ".."));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                basePath = Path.GetFullPath(Path.Combine(basePath, ".."));
            }
            var config = new ConfigurationBuilder()
                .SetBasePath(Path.GetFullPath(basePath))
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();
            osmLocation = config["osmFileLocation"];
            routerDbLocation = config["routerDbFileLocation"];
            //distanceFileLocation = config["bikeStationDistancesFileLocationNextbike"];
            //TODO: move to Nextbike file
            //distanceDBFileLocation = config["bikeStationDistancesDBFileLocationNextbike"];

            if (osmLocation == null)
            {
                // needs to be specified in the config file
                Console.WriteLine("No osm file found at the following location: " + config["gtfsLocation"]);
                Console.WriteLine("Change the osm file location in the config.json file, so that the path is correct");
                return;
            }
            if (routerDbLocation == null)
            {
                // does not need to be specified in the config file -> default location is used
                int lastIndexOfBackslash = osmLocation.LastIndexOf("\\");
                routerDbLocation = osmLocation.Substring(0, lastIndexOfBackslash) + "\\cz.routerdb";
            }

            //if (distanceDBFileLocation == null)
            //{
            //    Console.WriteLine("No distance DB file found at the following location: " + config["bikeStationDistancesDBFileLocationNextbike"]);
            //    Console.WriteLine("Change the DB distance file location in the config.json file, so that the path is correct");
            //    return;
            //}


            // load the routerdb from disk, if it exists, otherwise create it from the osm file
            if (File.Exists(routerDbLocation))
            {
                routerDb = RouterDb.Deserialize(File.OpenRead(routerDbLocation));
            }
            else
            {
                routerDb = new RouterDb();
                using (var stream = new FileInfo(osmLocation).OpenRead())
                {
                    routerDb.LoadOsmData(stream, Vehicle.Bicycle);
                }

                // write the routerdb to disk, so that it can be used later.
                using (var stream = new FileInfo(routerDbLocation).Open(FileMode.Create))
                {
                    routerDb.Serialize(stream);
                }
            }

            //if (File.Exists(distanceDBFileLocation))
            //{
            //    distanceDB = new BikeDistanceDatabase(distanceDBFileLocation);
            //}
            //else
            //{
            //    // TODO: shouldnt exceptions be thrown in these methods?
            //    Console.WriteLine("Error loading distances");
            //    return;
            //}
        }

        /// <summary>
        /// Calculates the distances between all the bike stations and loads them into the distance matrix
        /// </summary>
        /// <param name="stationsById">The dictionary to access the bike stations by their Ids</param>
        /// <returns>The distance matrix</returns>
        public StationDistanceMatrix GetDistanceMatrix(Dictionary<string, BikeStation> stationsById, string distanceDBFileLocation)
        {
            BikeDistanceDatabase db;

            // load the distance database from disk, if it exists, otherwise create it
            db = new BikeDistanceDatabase(distanceDBFileLocation);

            StationDistanceMatrix distances = db.GetDistanceMatrixAndRemoveNonExistentStations(stationsById);
            var profile = Vehicle.Bicycle.Shortest();


            Dictionary<string, int> unresolvableStations = new Dictionary<string, int>();

            foreach (var (id1, s1) in stationsById)
            {
                int alreadyLoadedCount = 0;
                int toLoadCount = 0;
                foreach (var (id2, s2) in stationsById)
                {
                    if (id1.CompareTo(id2) == 1) continue;


                    if (distances.HasDistance(s1, s2))
                    {
                        alreadyLoadedCount++;
                        continue;
                    }

                    toLoadCount++;

                    if (IsUnresolvable(s1, unresolvableStations) || IsUnresolvable(s2, unresolvableStations))
                    {
                        // One of the stations has been unresolvable too many times
                        AddDistance(s1, s2, -1);
                    }
                    //TODO: change to 3500
                    else if (DistanceExtensions.TooFarInOneDirection(s1, s2, 3000))
                    {
                        // The stations are too far from each other in a straight line
                        AddDistance(s1, s2, -1);
                    }
                    else if (!CheckConnectivity(s1))
                    {
                        // The start station is not connected to network
                        addUnresolvableStation(s1.Id, unresolvableStations);
                        AddDistance(s1, s2, -1);
                        Console.WriteLine("Station " + s1.Id + " is not connected");
                    }
                    else if (!CheckConnectivity(s2))
                    {
                        // The end station is not connected to network
                        addUnresolvableStation(s2.Id, unresolvableStations);
                        AddDistance(s1, s2, -1);
                        Console.WriteLine("Station " + s2.Id + " is not connected");
                    }
                    else
                    {
                        // Actually calculate the distance
                        ErrorType errorType;
                        int result = GetBikingDistance(s1, s2, out errorType);
                        if (result == 0)
                        {
                            result = 1;
                            // for stations that are too near
                        }
                        AddDistance(s1, s2, result);


                        if (errorType != ErrorType.NO_ERROR)
                        {
                            if (errorType == ErrorType.START_RESOLVE_ERROR)
                            {
                                addUnresolvableStation(s1.Id, unresolvableStations);
                                Console.WriteLine($"Start station resolve error from {s1.Id}: {s1.Name}");
                            }
                            else if (errorType == ErrorType.END_RESOLVE_ERROR)
                            {
                                addUnresolvableStation(s2.Id, unresolvableStations);
                                Console.WriteLine($"End station resolve error from {s2.Id}: {s2.Name}");
                            }
                            else if (errorType == ErrorType.ROUTE_CALCULATION_ERROR)
                            {
                                //addUnresolvableStation(Stations[i].Id);
                                addUnresolvableStation(s2.Id, unresolvableStations);
                                Console.WriteLine($"Route calculation error from {s1.Id}: {s1.Name} to {s2.Id}: {s2.Name}");
                            }
                        }
                    }
                }
            }

            return distances;


            bool IsUnresolvable(BikeStation s1, Dictionary<string, int> unresolvableStations)
            {
                return unresolvableStations.ContainsKey(s1.Id) && unresolvableStations[s1.Id] > 2;
            }
            void AddDistance(BikeStation from, BikeStation to, int distance)
            {
                distances.AddDistance(from, to, distance);
                //AppendDistanceToFile(from, to, distance);
                db.AddOrUpdateDistance(from.Id, to.Id, distance);

                Console.WriteLine("Didnt have distance from " + from.Id + " to " + to.Id + ": " + distance + "m");
            }
            void addUnresolvableStation(string id, Dictionary<string, int> unresolvableStations)
            {
                if (unresolvableStations.ContainsKey(id))
                {
                    unresolvableStations[id]++;
                }
                else
                {
                    unresolvableStations.Add(id, 1);
                }
            }
        }


       /*public StationDistanceMatrix CalculateMatrix(List<BikeStation> stations, Dictionary<string, BikeStation> stationsById)
        {
            //var router = new Router(routerDb);
            var profile = Vehicle.Bicycle.Shortest();


            if (File.Exists(distanceFileLocation))
            {
                LoadDistancesFromFile(stationsById);
            }


            Dictionary<string, int> unresolvableStations = new Dictionary<string, int>();
            Stopwatch stopwatch = new Stopwatch();
            double totalSeconds = 0.0;

            for (int i = 0; i < stations.Count; i++)
            {
                stopwatch.Start();
                //Console.WriteLine("CALCULATING STATION #" + i + "/" + stations.Count);



                int alreadyLoadedCount = 0;
                int toLoadCount = 0;
                BikeStation s1 = stations[i];

                for (int j = 0; j < stations.Count; j++)
                {
                    if (i > j) continue; // the matrix is symmetric -> skip the already calculated distances

                    BikeStation s2 = stations[j];


                    if (distances.HasDistance(s1, s2))
                    {
                        // The distance is already loaded from file -> skip it
                        alreadyLoadedCount++;
                        continue;
                    }

                    toLoadCount++;
                    if (IsUnresolvable(s1, unresolvableStations) || IsUnresolvable(s2, unresolvableStations))
                    {
                        // One of the stations has been unresolvable too many times
                        AddDistance(s1, s2, -1);
                    }
                    else if (DistanceExtensions.TooFarInOneDirection(s1, s2, 3000))
                    {
                        // The stations are too far from each other in a straight line
                        AddDistance(s1, s2, -1);
                    }
                    else if (!CheckConnectivity(s1))
                    {
                        // The start station is not connected to network
                        addUnresolvableStation(s1.Id, unresolvableStations);
                        AddDistance(s1, s2, -1);
                        Console.WriteLine("Station " + s1.Id + " is not connected");
                    }
                    else if (!CheckConnectivity(s2))
                    {
                        // The end station is not connected to network
                        addUnresolvableStation(s2.Id, unresolvableStations);
                        AddDistance(s1, s2, -1);
                        Console.WriteLine("Station " + s2.Id + " is not connected");
                    }
                    else
                    {
                        // Actually calculate the distance
                        ErrorType errorType;
                        int result = GetBikingDistance(s1, s2, out errorType);
                        if (result == 0)
                        {
                            result = 1;
                            // for stations that are too near
                        }
                        AddDistance(s1, s2, result);


                        if (errorType != ErrorType.NO_ERROR)
                        {
                            if (errorType == ErrorType.START_RESOLVE_ERROR)
                            {
                                addUnresolvableStation(stations[i].Id, unresolvableStations);
                                Console.WriteLine($"Start station resolve error from {stations[i].Id}: {stations[i].Name}");
                            }
                            else if (errorType == ErrorType.END_RESOLVE_ERROR)
                            {
                                addUnresolvableStation(stations[j].Id, unresolvableStations);
                                Console.WriteLine($"End station resolve error from {stations[j].Id}: {stations[j].Name}");
                            }
                            else if (errorType == ErrorType.ROUTE_CALCULATION_ERROR)
                            {
                                //addUnresolvableStation(Stations[i].Id);
                                addUnresolvableStation(stations[j].Id, unresolvableStations);
                                Console.WriteLine($"Route calculation error from {stations[i].Id}: {stations[i].Name} to {stations[j].Id}: {stations[j].Name}");
                            }
                        }
                    }
                }
                stopwatch.Stop();
                totalSeconds += stopwatch.Elapsed.TotalSeconds;
                //Console.WriteLine("Time elapsed: " + stopwatch.Elapsed + "s");

                //Console.WriteLine("Total time elapsed: " + totalSeconds + "s");
                if (i > 0)
                {
                    double ratio = (stations.Count - (double)i) / i; // remaining stations / stations processed
                    double estimatedRemainingTime = ratio * totalSeconds;
                    //Console.WriteLine("ESTIMATED REMAINING TIME: " + (int)(estimatedRemainingTime / 60) + ":" + (int)(estimatedRemainingTime % 60));
                }
                //Console.WriteLine("From File: " + alreadyLoadedCount + "/" + toLoadCount);
                //Console.WriteLine("---------------------------------------------------");

                stopwatch.Reset();

            }
            return distances;



            
            bool IsUnresolvable(BikeStation s1, Dictionary<string, int> unresolvableStations)
            {
                return unresolvableStations.ContainsKey(s1.Id) && unresolvableStations[s1.Id] > 2;
            }
            void AddDistance(BikeStation from, BikeStation to, int distance)
            {
                distances.AddDistance(from, to, distance);
                AppendDistanceToFile(from, to, distance);
            }
            void addUnresolvableStation(string id, Dictionary<string, int> unresolvableStations)
            {
                if (unresolvableStations.ContainsKey(id))
                {
                    unresolvableStations[id]++;
                }
                else
                {
                    unresolvableStations.Add(id, 1);
                }
            }
        }
        /// <summary>
        /// Loads the distances between bike stations from the configured file - if the file exists
        /// </summary>
        /// <param name="stationsById">The dictionary of bike stations indexed by their Ids</param>
        private void LoadDistancesFromFile(Dictionary<string, BikeStation> stationsById)
        {
            using (StreamReader reader = new StreamReader(distanceFileLocation))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split(',');

                    if (!stationsById.ContainsKey(parts[0]) || !stationsById.ContainsKey(parts[1]))
                    {
                        // The station is not in the list of stations -> skip it
                        continue;
                    }

                    BikeStation from = stationsById[parts[0]];
                    BikeStation to = stationsById[parts[1]];
                    int distance = int.Parse(parts[2]);

                    distances.AddDistance(from, to, distance);
                }
            }
        }
        /// <summary>
        /// Appends a new line to the distance file, containing the distance between two bike stations
        /// </summary>
        /// <param name="from">The source bikeStation</param>
        /// <param name="to">The destination bikeStation</param>
        /// <param name="distance">The distance in meters</param>
        private void AppendDistanceToFile(BikeStation from, BikeStation to, int distance)
        {
            using (StreamWriter writer = new StreamWriter(distanceFileLocation, true))
            {
                writer.WriteLine(from.Id + "," + to.Id + "," + distance);
                writer.Flush();
            }
        }*/
        /// <summary>
        /// Checks, whether the given coordinates are connected to the network - i.e. they do not lie on a "routing island"
        /// </summary>
        /// <param name="lat">Latitude</param>
        /// <param name="lon">Longitude</param>
        /// <returns>Bool specifying whether the point is connected to the rest of the network</returns>
        private bool CheckConnectivity(float lat, float lon)
        {
            var profile = Vehicle.Bicycle.Shortest();

            var router = new Router(routerDb);
            var point = router.TryResolve(profile, lat, lon);

            if (point.IsError)
            {
                return false;
            }
            else
            {
                return router.CheckConnectivity(profile, point.Value);
            }
        }
        /// <summary>
        /// Checks, whether the given bikeStation is connected to the rest of the network
        /// </summary>
        /// <param name="station">The bike station to check</param>
        /// <returns>Bool specifying whether the station is connected to the rest of the network</returns>
        private bool CheckConnectivity(BikeStation station)
        {
            return CheckConnectivity((float)station.Coords.Lat, (float)station.Coords.Lon);
        }
        /// <summary>
        /// Calculates the real-world biking distance between two points using the OSM router
        /// </summary>
        /// <param name="startLat">Starting point latitude</param>
        /// <param name="startLon">Starting point longitude</param>
        /// <param name="endLat">Destination point latitude</param>
        /// <param name="endLon">Destination point longitude</param>
        /// <param name="errorType">The error, due to which the distance could not be calculated</param>
        /// <returns>The biking distance between the points in meters</returns>
        private int GetBikingDistance(float startLat, float startLon, float endLat, float endLon, out ErrorType errorType)
        {
            var router = new Router(routerDb);

            var profile = Vehicle.Bicycle.Shortest();
            RouterPoint start;
            RouterPoint end;
            Route route;

            Result<RouterPoint> startResult = router.TryResolve(profile, startLat, startLon, 150f);
            Result<RouterPoint> endResult = router.TryResolve(profile, endLat, endLon, 150f);

            if (startResult.IsError)
            {
                errorType = ErrorType.START_RESOLVE_ERROR;
                return -1;
            }
            else if (endResult.IsError)
            {
                errorType = ErrorType.END_RESOLVE_ERROR;
                return -1;
            }
            else
            {
                start = startResult.Value;
                end = endResult.Value;
            }

            Result<Route> routeResult = router.TryCalculate(profile, start, end);
            if (routeResult.IsError)
            {
                errorType = ErrorType.ROUTE_CALCULATION_ERROR;
                return -1;
            }
            else
            {
                route = routeResult.Value;
            }

            errorType = ErrorType.NO_ERROR;
            return (int)route.TotalDistance;
        }
        /// <summary>
        /// Calculates the real-world biking distance between two stations using the OSM router
        /// </summary>
        /// <param name="start">The starting bike station</param>
        /// <param name="end">The destination bike station</param>
        /// <param name="errorType">The error, due to which the distance could not be calculated</param>
        /// <returns>The biking distance between the stations in meters</returns>
        private int GetBikingDistance(BikeStation start, BikeStation end, out ErrorType errorType)
        {
            return GetBikingDistance((float)start.Coords.Lat, (float)start.Coords.Lon, (float)end.Coords.Lat, (float)end.Coords.Lon, out errorType);
        }
    }
}

﻿using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Configuration;
using Route = Itinero.Route;

namespace RAPTOR_Router.GBFSParsing.Distances
{
    /// <summary>
    /// Represents the error, due to which a route between two bike stations could not be calculated
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// No error occurred
        /// </summary>
        NO_ERROR,
        /// <summary>
        /// The start point could not be resolved
        /// </summary>
        START_RESOLVE_ERROR,
        /// <summary>
        /// The end point could not be resolved
        /// </summary>
        END_RESOLVE_ERROR,
        /// <summary>
        /// The route between the two points could not be calculated
        /// </summary>
        ROUTE_CALCULATION_ERROR
    }

    /// <summary>
    /// Class used for calculating the distances between bike stations
    /// </summary>
    public class BikeDistanceCalculator
    {
        private RouterDb routerDb = new();

        /// <summary>
        /// Loads the configuration info and creates a routerDb instance - either by creating a new one from the osm file, or by loading it from disk
        /// </summary>
        public BikeDistanceCalculator()
        {
            string? osmLocation = Config.OsmFilePath;
            string? routerDbLocation = Config.RouterDbFilePath;
            
            ValidateOsmPath();
            SetRouterDbPathIfNull();
            
            LoadOrCreateRouterDbFile();

            

            void ValidateOsmPath()
            {
                if (osmLocation == null)
                {
                    // needs to be specified in the config file
                    Console.WriteLine("OSM file location not specified in the config file");
                    throw new InvalidDataException("OSM file location not specified");
                }
            }

            void SetRouterDbPathIfNull()
            {
                if (routerDbLocation == null)
                {
                    // does not need to be specified in the config file -> default location is used
                    int lastIndexOfBackslash = osmLocation!.LastIndexOf("\\");
                    routerDbLocation = osmLocation.Substring(0, lastIndexOfBackslash) + "\\cz.routerdb";
                }
            }

            void LoadOrCreateRouterDbFile()
            {
                // load the routerdb from disk, if it exists, otherwise create it from the osm file
                if (File.Exists(routerDbLocation))
                {
                    routerDb = RouterDb.Deserialize(File.OpenRead(routerDbLocation));
                }
                else
                {
                    routerDb = new RouterDb();
                    using (var stream = new FileInfo(osmLocation!).OpenRead())
                    {
                        routerDb.LoadOsmData(stream, Vehicle.Bicycle);
                    }

                    // write the routerdb to disk, so that it can be used later.
                    using (var stream = new FileInfo(routerDbLocation!).Open(FileMode.Create))
                    {
                        routerDb.Serialize(stream);
                    }
                }
            }
        }

        /// <summary>
        /// Calculates the distances between all the bike stations and loads them into the distance matrix
        /// </summary>
        /// <param name="stationsById">The dictionary to access the bike stations by their Ids</param>
        /// <param name="distanceDBFileLocation">Location of the file in which previously already computed distances are stored</param>
        /// <returns>The distance matrix</returns>
        public StationDistanceMatrix GetDistanceMatrix(Dictionary<string, BikeStation> stationsById, string distanceDBFileLocation)
        {
            // load the distance database from disk, if it exists, otherwise create it
            BikeDistanceDatabase db = new BikeDistanceDatabase(distanceDBFileLocation);


            StationDistanceMatrix distances = db.GetDistanceMatrixAndRemoveNonExistentStations(stationsById);


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
                        AddUnresolvableStation(s1.Id, unresolvableStations);
                        AddDistance(s1, s2, -1);
                        Console.WriteLine("Station " + s1.Id + " is not connected");
                    }
                    else if (!CheckConnectivity(s2))
                    {
                        // The end station is not connected to network
                        AddUnresolvableStation(s2.Id, unresolvableStations);
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
                                AddUnresolvableStation(s1.Id, unresolvableStations);
                                Console.WriteLine($"Start station resolve error from {s1.Id}: {s1.Name}");
                            }
                            else if (errorType == ErrorType.END_RESOLVE_ERROR)
                            {
                                AddUnresolvableStation(s2.Id, unresolvableStations);
                                Console.WriteLine($"End station resolve error from {s2.Id}: {s2.Name}");
                            }
                            else if (errorType == ErrorType.ROUTE_CALCULATION_ERROR)
                            {
                                //addUnresolvableStation(Stations[i].Id);
                                AddUnresolvableStation(s2.Id, unresolvableStations);
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
            void AddUnresolvableStation(string id, Dictionary<string, int> unresolvableStations)
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

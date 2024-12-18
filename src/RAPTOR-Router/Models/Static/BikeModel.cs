using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Interfaces;
using RAPTOR_Router.GBFSParsing.DataSources;
using RAPTOR_Router.GBFSParsing.Distances;
using RAPTOR_Router.Extensions;
using System.Timers;
using RAPTOR_Router.Structures.Generic;
using Timer = System.Timers.Timer;


namespace RAPTOR_Router.Models.Static
{
    /// <summary>
    /// Class holding all the information about all used shared bike systems and their stations.
    /// </summary>
    public class BikeModel
    {
        /// <summary>
        /// List of all the bike stations in all systems.
        /// </summary>
        public List<BikeStation> Stations { get; private set; }


        private Dictionary<string, BikeStation> StationsById;
        private StationDistanceMatrix Distances;
        private List<IBikeDataSource> bikeDataSources;
        private Timer statusUpdateTimer;

        /// <summary>
        /// Creates a new BikeModel, initiates its status update timer and sets up the data structures.
        /// </summary>
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

        /// <summary>
        /// Adds a new data source to the model, and merges its data with the existing data.
        /// </summary>
        /// <param name="source">The data source to add</param>
        public void AddDataSource(IBikeDataSource source)
        {
            source.LoadStations();
            source.LoadStationDistances();

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

        /// <summary>
        /// Sets the bike count for all stations and starts the timer to update the status of the stations periodically.
        /// </summary>
        public void StartUpdateTimer()
        {
            foreach (IBikeDataSource dataSource in bikeDataSources)
            {
                dataSource.UpdateStationStatus();
            }
            statusUpdateTimer.Start();
        }

        /// <summary>
        /// Updates the status (bike count) of all stations in all data sources.
        /// </summary>
        /// <remarks>Called by the status update timer</remarks>
        public void UpdateAllStationStatus(object? source, ElapsedEventArgs e)
        {
            foreach (IBikeDataSource dataSource in bikeDataSources)
            {
                dataSource.UpdateStationStatus();
            }
        }

        /// <summary>
        /// Gets the dictionary of distances from a given station to all other stations.
        /// </summary>
        /// <param name="station">The station from which to get the distances</param>
        /// <returns>The dictionary containing the distances</returns>
        public Dictionary<BikeStation, int> GetDistancesFromStation(BikeStation station)
        {
            return Distances.GetDistancesFromStation(station);
        }

        /// <summary>
        /// Gets the distance between the 2 bike stations in meters
        /// </summary>
        /// <param name="s1">The first station</param>
        /// <param name="s2">The second station</param>
        /// <returns>The distance in meters</returns>
        public int GetDistanceBetweenStations(BikeStation s1, BikeStation s2)
        {
            return Distances.GetDistance(s1, s2);
        }

        /// <summary>
        /// Gets all the bike stations within the given radius of the given coordinates.
        /// </summary>
        /// <param name="coords">The coordinates at which to get the near stations</param>
        /// <param name="radius">The maximum distance of the found bike station from the coordinates</param>
        /// <returns>The list of all near stations</returns>
        public List<BikeStation> GetNearStations(Coordinates coords, int radius)
        {
            List<BikeStation> nearStations = new List<BikeStation>();
            foreach (BikeStation s in Stations)
            {
                // Skip stations that are too far away in one direction to speed up the calculation
                if (DistanceExtensions.TooFarInOneDirection(coords, s.Coords, radius))
                {
                    continue;
                }
                if (DistanceExtensions.SimplifiedDistanceBetween(s.Coords, coords) < radius)
                {
                    nearStations.Add(s);
                }
            }
            return nearStations;
        }
        /// <summary>
        /// Gets all the bike stations within the given radius of the given RoutePoint.
        /// </summary>
        /// <param name="rp">The RoutePoint to get near stations for</param>
        /// <param name="radius">The maximum distance of the found bike station from the RoutePoint</param>
        /// <returns>The list of all near stations</returns>
        public List<BikeStation> GetNearStations(IRoutePoint rp, int radius)
        {
            return GetNearStations(rp.Coords, radius);
        }

        /// <summary>
        /// Finds out whether a bike station exists within the given radius of the given coordinates.
        /// </summary>
        /// <param name="coords">The coordinates for which to find out whether a near station exists</param>
        /// <param name="radius">The maximum distance of the found bike station from the coordinates</param>
        /// <returns>Bool specifying whether there is a station within the radius</returns>
        public bool NearStationExists(Coordinates coords, int radius)
        {
            foreach (BikeStation s in Stations)
            {
                if (DistanceExtensions.SimplifiedDistanceBetween(s.Coords, coords) < radius)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

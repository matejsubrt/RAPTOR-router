using RAPTOR_Router.GBFSParsing.Distances;
using RAPTOR_Router.Structures.Bike;

namespace RAPTOR_Router.GBFSParsing.DataSources
{
    /// <summary>
    /// Interface for a data source for one shared bikes system (i.e. nextbike/rekola/...)
    /// </summary>
    public interface IBikeDataSource
    {
        /// <summary>
        /// Loads the static station information from the data source - i.e. the stations' locations and capacities
        /// </summary>
        public void LoadStations();

        /// <summary>
        /// Creates and fills a matrix of real-world distances between all stations
        /// </summary>
        //public void LoadStationDistances();
        public void LoadStationDistances()
        {
            if (StationsById is null || DistancesDbFileLocation is null)
            {
                throw new InvalidOperationException("StationsById and DistancesDbFileLocation must be set before calling LoadStationDistances");
            }
            BikeDistanceCalculator distanceCalculator = new BikeDistanceCalculator();
            Distances = distanceCalculator.GetDistanceMatrix(StationsById, DistancesDbFileLocation);
        }
        /// <summary>
        /// Loads the dynamic station information from the data source - i.e. the number of available bikes at each station
        /// </summary>
        public void UpdateStationStatus();

        /// <summary>
        /// The list of all bike stations in the system
        /// </summary>
        public List<BikeStation> Stations { get; }
        /// <summary>
        /// The list of all bike stations in the system, indexed by their unique Id
        /// </summary>
        public Dictionary<string, BikeStation> StationsById { get; }
        /// <summary>
        /// The matrix of real-world distances between all stations
        /// </summary>
        public StationDistanceMatrix Distances { get; protected set; }

        public string DistancesDbFileLocation { get; set; }
    }
}

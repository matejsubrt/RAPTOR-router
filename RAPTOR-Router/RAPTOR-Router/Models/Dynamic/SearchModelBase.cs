using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Custom;
using RAPTOR_Router.Structures.Transit;

namespace RAPTOR_Router.Models.Dynamic
{
    /// <summary>
    /// Baase class serving as a common class for forward and backward models. Holds all the information that is independent of the search direction.
    /// </summary>
    public abstract class SearchModelBase
    {
        /// <summary>
        /// List of all the public transit stops considered as the source
        /// </summary>
        public List<Stop> sourceStops { get; set; }
        /// <summary>
        /// List of all the public transit stops considered as the destination
        /// </summary>
        public List<Stop> destinationStops { get; set; }
        /// <summary>
        /// List of all the bike stations considered as the source
        /// </summary>
        public List<BikeStation> sourceBikeStations { get; set; }
        /// <summary>
        /// List of all the bike stations considered as the destination
        /// </summary>
        public List<BikeStation> destinationBikeStations { get; set; }
        /// <summary>
        /// The custom route point from which the search is started (used for searching from coordinates instead of from stop names)
        /// </summary>
        public CustomRoutePoint? sourceCustomRoutePoint { get; set; }
        /// <summary>
        /// The custom route point to which the search is done (used for searching to coordinates instead of to stop names)
        /// </summary>
        public CustomRoutePoint? destinationCustomRoutePoint { get; set; }

        /// <summary>
        /// The settings being used for the search
        /// </summary>
        protected Settings settingsUsed;

        /// <summary>
        /// Creates the SearchModelBase
        /// </summary>
        /// <param name="sourceStops">The list of stops considered as the source stops - typically stops from one node sharing the same name</param>
        /// <param name="destinationStops">The list of stops considered as the destination stops - typically stops from one node sharing the same name</param>
        /// <param name="sourceBikeStations">The list of bikeStations considered as the source stations</param>
        /// <param name="destinationBikeStations">The list of bikeStations considered as the destination stations</param>
        /// <param name="settingsUsed">The settings used for the search</param>
        public SearchModelBase(List<Stop> sourceStops, List<Stop> destinationStops, List<BikeStation> sourceBikeStations, List<BikeStation> destinationBikeStations, Settings settingsUsed)
        {
            this.sourceStops = sourceStops;
            this.destinationStops = destinationStops;
            this.sourceBikeStations = sourceBikeStations;
            this.destinationBikeStations = destinationBikeStations;
            this.settingsUsed = settingsUsed;
        }
    }
}

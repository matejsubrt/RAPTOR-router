using RAPTOR_Router.Structures.Interfaces;
using RAPTOR_Router.Structures.Transit;

namespace RAPTOR_Router.Structures.Bike
{
    /// <summary>
    /// Base class for a bike transfer
    /// </summary>
    public abstract class BikeTransfer : ITransfer
    {
        /// <summary>
        /// The length of the transfer in meters
        /// </summary>
        public int Distance { get; protected set; }
        /// <summary>
        /// Gets the time it takes to walk the transfer at the given walking pace
        /// </summary>
        /// <param name="walkingPace">The pace to use for the calculation in min/km</param>
        /// <returns>The time it takes to walk in seconds</returns>
        public int GetTransferTime(int walkingPace)
        {
            return (int)(Distance / 1000.0 * walkingPace * 60);
        }
        /// <summary>
        /// Gets the source point of the transfer
        /// </summary>
        /// <returns>The source point</returns>
        public abstract IRoutePoint GetSrcRoutePoint();
        /// <summary>
        /// Gets the destination point of the transfer
        /// </summary>
        /// <returns>The destination point</returns>
        public abstract IRoutePoint GetDestRoutePoint();
        /// <summary>
        /// A reference to the same transfer in the opposite direction
        /// </summary>
        public BikeTransfer OppositeTransfer { get; set; }
    }

    /// <summary>
    /// Class representing a transfer from a bike station to a stop
    /// </summary>
    public class FromBikeTransfer : BikeTransfer
    {
        /// <summary>
        /// The source BikeStation
        /// </summary>
        public BikeStation From { get; }
        /// <summary>
        /// The destination stop
        /// </summary>
        public Stop To { get; }

        /// <summary>
        /// Creates anew FromBikeTransfer object
        /// </summary>
        /// <param name="bikeStation">The bike station (source)</param>
        /// <param name="stop">The stop (destination)</param>
        /// <param name="dist">The distance in meters</param>
        public FromBikeTransfer(BikeStation bikeStation, Stop stop, int dist)
        {
            From = bikeStation;
            To = stop;
            Distance = dist;
        }

        /// <summary>
        /// Returns a string representation of the bike transfer
        /// </summary>
        /// <returns>A string representation of the bike transfer</returns>
        public override string ToString()
        {
            return "Transfer from " + From.Name + " to " + To.Name;
        }
        /// <summary>
        /// Gets the source point of the transfer
        /// </summary>
        /// <returns>The source point</returns>
        public override IRoutePoint GetSrcRoutePoint()
        {
            return From;
        }
        /// <summary>
        /// Gets the destination point of the transfer
        /// </summary>
        /// <returns>The destination point</returns>
        public override IRoutePoint GetDestRoutePoint()
        {
            return To;
        }
    }

    /// <summary>
    /// Class representing a transfer from a stop to a bike station
    /// </summary>
    public class ToBikeTransfer : BikeTransfer
    {
        /// <summary>
        /// The source stop
        /// </summary>
        public Stop From { get; }
        /// <summary>
        /// The destination bike station
        /// </summary>
        public BikeStation To { get; }

        /// <summary>
        /// Creates anew ToBikeTransfer object
        /// </summary>
        /// <param name="stop">The stop (source)</param>
        /// <param name="bikeStation">The bike station (destination)</param>
        /// <param name="dist">The distance in meters</param>
        public ToBikeTransfer(Stop stop, BikeStation bikeStation, int dist)
        {
            From = stop;
            To = bikeStation;
            Distance = dist;
        }
        public override string ToString()
        {
            return "Transfer from " + From.Name + " to " + To.Name;
        }

        /// <summary>
        /// Gets the source point of the transfer
        /// </summary>
        /// <returns>The source point</returns>
        public override IRoutePoint GetSrcRoutePoint()
        {
            return From;
        }
        /// <summary>
        /// Gets the destination point of the transfer
        /// </summary>
        /// <returns>The destination point</returns>
        public override IRoutePoint GetDestRoutePoint()
        {
            return To;
        }
    }
}

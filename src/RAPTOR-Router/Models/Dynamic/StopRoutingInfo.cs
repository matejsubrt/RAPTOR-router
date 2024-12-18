using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Custom;
using RAPTOR_Router.Structures.Transit;

namespace RAPTOR_Router.Models.Dynamic
{
    /// <summary>
    /// A class representing the current routing information for a single RoutePoint
    /// </summary>
    public class StopRoutingInfo
    {
        /// <summary>
        /// Interface for a routing entry. An entry can either be an arrival entry or a departure entry, based on the search direction.
        /// </summary>
        public interface IRoutingEntry
        {
            /// <summary>
            /// The best time of the entry
            /// </summary>
            public DateTime Time { get; }
        }


        /// <summary>
        /// Class representing a reach of a stop by a trip
        /// </summary>
        public class TripReach : IRoutingEntry
        {
            /// <summary>
            /// The trip used to reach the stop
            /// </summary>
            public Trip Trip { get; protected set; }
            /// <summary>
            /// The date on which the trip starts
            /// </summary>
            public DateOnly TripStartDate { get; protected set; }
            /// <summary>
            /// The time at which the stop was reached
            /// </summary>
            public DateTime Time { get; protected set; }
            /// <summary>
            /// The stop from which this trip entry has been added (getOnStop or getOffStop)
            /// </summary>
            public Stop ReachedFromStop { get; protected set; }

            /// <summary>
            /// Creates a new TripReach object
            /// </summary>
            /// <param name="trip">The trip by which the stop was reached</param>
            /// <param name="otherEndStop">The stop on which the trip was boarded/deboarded</param>
            /// <param name="reachTime">The time at which the stop was reached</param>
            /// <param name="tripStartDate">The date at which the trip starts</param>
            internal TripReach(Trip trip, Stop otherEndStop, DateTime reachTime, DateOnly tripStartDate)
            {
                this.Trip = trip;
                this.ReachedFromStop = otherEndStop;
                this.Time = reachTime;
                TripStartDate = tripStartDate;
            }

            /// <summary>
            /// Returns a string representation of the TripReach object
            /// </summary>
            /// <returns>The string representation</returns>
            public override string ToString()
            {
                return "TripReach at " + Time.ToShortTimeString() + ": " + Trip.Route.ShortName + " to/from " + ReachedFromStop.Name;
            }
        }

        /// <summary>
        /// Class representing a reach of a stop by a transfer
        /// </summary>
        public class TransferReach : IRoutingEntry
        {
            /// <summary>
            /// The transfer used for the entry
            /// </summary>
            public Transfer Transfer { get; protected set; }
            /// <summary>
            /// The time at which the stop was reached
            /// </summary>
            public DateTime Time { get; protected set; }
            /// <summary>
            /// Creates a new TransferReach object
            /// </summary>
            /// <param name="transfer">The transfer by which the stop was reached</param>
            /// <param name="reachTime">The time at which the stop was reached</param>
            internal TransferReach(Transfer transfer, DateTime reachTime)
            {
                this.Transfer = transfer;
                this.Time = reachTime;
            }

            /// <summary>
            /// Returns a string representation of the TransferReach object
            /// </summary>
            /// <returns>The string representation</returns>
            public override string ToString()
            {
                return "TransferReach at " + Time.ToShortTimeString() + ": " + Transfer.From.Name + " to " + Transfer.To.Name;
            }
        }

        /// <summary>
        /// Class representing a reach of a stop by a bike transfer
        /// </summary>
        public class BikeTransferReach : IRoutingEntry
        {
            /// <summary>
            /// The bike transfer used for the entry
            /// </summary>
            public BikeTransfer Transfer { get; protected set; }
            /// <summary>
            /// The time at which the RoutePoint was reached
            /// </summary>
            public DateTime Time { get; protected set; }
            /// <summary>
            /// Creates a new BikeTransferReach object
            /// </summary>
            /// <param name="transfer">The bike transfer by which the RoutePoint was reached</param>
            /// <param name="reachTime">The time at which the RoutePoint was reached</param>
            internal BikeTransferReach(BikeTransfer transfer, DateTime reachTime)
            {
                this.Transfer = transfer;
                this.Time = reachTime;
            }

            /// <summary>
            /// Returns a string representation of the BikeTransferReach object
            /// </summary>
            /// <returns>The string representation</returns>
            public override string ToString()
            {
                return "BikeTransferReach at " + Time.ToShortTimeString() + ": " + Transfer.GetSrcRoutePoint().Name + " to " + Transfer.GetDestRoutePoint().Name;
            }
        }

        /// <summary>
        /// Class representing a reach of a stop by a custom transfer
        /// </summary>
        public class CustomTransferReach : IRoutingEntry
        {
            /// <summary>
            /// The custom transfer used for the entry
            /// </summary>
            public CustomTransfer Transfer { get; }
            /// <summary>
            /// The reach time
            /// </summary>
            public DateTime Time { get; }
            /// <summary>
            /// Creates a new CustomTransferReach object
            /// </summary>
            /// <param name="transfer">The custom transfer by which the RoutePoint was reached</param>
            /// <param name="reachTime">The time at which the RoutePoint was reached</param>
            internal CustomTransferReach(CustomTransfer transfer, DateTime reachTime)
            {
                Transfer = transfer;
                Time = reachTime;
            }

            /// <summary>
            /// Returns a string representation of the CustomTransferReach object
            /// </summary>
            /// <returns>The string representation</returns>
            public override string ToString()
            {
                return "CustomTransferReach at " + Time.ToShortTimeString() + ": " + Transfer.GetSrcRoutePoint().Name + " to " + Transfer.GetDestRoutePoint().Name;
            }
        }

        /// <summary>
        /// Class representing a reach of a stop by a bike trip
        /// </summary>
        public class BikeTripReach : IRoutingEntry
        {
            /// <summary>
            /// The bike station from which the bike trip was traversed
            /// </summary>
            public BikeStation From { get; protected set; }
            /// <summary>
            /// The bike station that was reached by the bike trip
            /// </summary>
            public BikeStation To { get; protected set; }
            /// <summary>
            /// The time at which the To bike station was reached
            /// </summary>
            public DateTime Time { get; protected set; }
            /// <summary>
            /// Creates a new BikeTripReach object
            /// </summary>
            /// <param name="from">The station from which the bike trip was traversed</param>
            /// <param name="to">The station that was reached by the trip</param>
            /// <param name="reachTime">The time at which the station was reached</param>
            internal BikeTripReach(BikeStation from, BikeStation to, DateTime reachTime)
            {
                this.From = from;
                this.To = to;
                this.Time = reachTime;
            }

            /// <summary>
            /// Returns a string representation of the BikeTripReach object
            /// </summary>
            /// <returns>The string representation</returns>
            public override string ToString()
            {
                return "BikeTripReach at " + Time.ToShortTimeString() + ": " + From.Name + " to " + To.Name;
            }
        }

        /// <summary>
        /// Class representing a reach of a stop by an implicit search start
        /// </summary>
        public class ImplicitSearchStartReach : IRoutingEntry
        {
            /// <summary>
            /// The time at which the RoutePoint was reached
            /// </summary>
            public DateTime Time { get; protected set; }
            /// <summary>
            /// Creates a new ImplicitSearchStartReach object
            /// </summary>
            /// <param name="reachTime">The time at which the RoutePoint is implicitly reached</param>
            public ImplicitSearchStartReach(DateTime reachTime)
            {
                this.Time = reachTime;
            }

            /// <summary>
            /// Returns a string representation of the ImplicitSearchStartReach object
            /// </summary>
            /// <returns>The string representation</returns>
            public override string ToString()
            {
                return "ImplicitSearchStartReach at " + Time.ToShortTimeString();
            }
        }

        

        /// <summary>
        /// The current earliest best time at which the RoutePoint was reached
        /// </summary>
        public DateTime BestTime { get; set; }

        /// <summary>
        /// Array of entries holding the best arrival/departure for a RoutePoint for every round of the search.
        /// </summary>
        internal IRoutingEntry?[] Entries { get; set; }

        
        /// <summary>
        /// Creates a new StopRoutingInfo object
        /// </summary>
        /// <param name="forward">Whether the search is in the forward direction</param>
        public StopRoutingInfo(bool forward)
        {
            BestTime = forward ? DateTime.MaxValue : DateTime.MinValue;

            Entries = new IRoutingEntry[Settings.ROUNDS + 1];
            for (int i = 0; i < Entries.Count(); i++)
            {
                Entries[i] = null;
            }
        }

        /// <summary>
        /// Gets the current best reach time at the RoutePoint
        /// </summary>
        public DateTime BestReachTime
        {
            get => BestTime;
            set => BestTime = value;
        }

        /// <summary>
        /// Gets the reaches at the RoutePoint
        /// </summary>
        public IRoutingEntry?[] Reaches
        {
            get => Entries;
            set => Entries = value;
        }

    }
}

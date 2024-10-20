using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Custom;
using RAPTOR_Router.Structures.Transit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.Models.Dynamic
{

    public class StopRoutingInfo
    {
        /// <summary>
        /// Class representing a reach of a stop by a trip
        /// </summary>
        public class TripReach : IEntry
        {
            /// <summary>
            /// The trip used to reach the stop
            /// </summary>
            public Trip Trip { get; protected set; }
            /// <summary>
            /// The time at which the stop was reached
            /// </summary>
            public DateTime Time { get; protected set; }
            /// <summary>
            /// The stop from which this trip entry has been added (getOnStop or getOffStop)
            /// </summary>
            public Stop ReachedFromStop { get; protected set; }

            internal TripReach(Trip trip, Stop otherEndStop, DateTime reachTime)
            {
                this.Trip = trip;
                this.ReachedFromStop = otherEndStop;
                this.Time = reachTime;
            }

            public override string ToString()
            {
                return "TripReach at " + Time.ToShortTimeString() + ": " + Trip.Route.ShortName + " to/from " + ReachedFromStop.Name;
            }
        }

        /// <summary>
        /// Class representing a reach of a stop by a transfer
        /// </summary>
        public class TransferReach : IEntry
        {
            /// <summary>
            /// The transfer used for the entry
            /// </summary>
            public Transfer Transfer { get; protected set; }
            /// <summary>
            /// The time at which the stop was reached
            /// </summary>
            public DateTime Time { get; protected set; }
            internal TransferReach(Transfer transfer, DateTime reachTime)
            {
                this.Transfer = transfer;
                this.Time = reachTime;
            }

            public override string ToString()
            {
                return "TransferReach at " + Time.ToShortTimeString() + ": " + Transfer.From.Name + " to " + Transfer.To.Name;
            }
        }

        /// <summary>
        /// Class representing a reach of a stop by a bike transfer
        /// </summary>
        public class BikeTransferReach : IEntry
        {
            /// <summary>
            /// The bike transfer used for the entry
            /// </summary>
            public BikeTransfer Transfer { get; protected set; }
            /// <summary>
            /// The time at which the RoutePoint was reached
            /// </summary>
            public DateTime Time { get; protected set; }
            internal BikeTransferReach(BikeTransfer transfer, DateTime reachTime)
            {
                this.Transfer = transfer;
                this.Time = reachTime;
            }

            public override string ToString()
            {
                return "BikeTransferReach at " + Time.ToShortTimeString() + ": " + Transfer.GetSrcRoutePoint().Name + " to " + Transfer.GetDestRoutePoint().Name;
            }
        }

        /// <summary>
        /// Class representing a reach of a stop by a custom transfer
        /// </summary>
        public class CustomTransferReach : IEntry
        {
            /// <summary>
            /// The custom transfer used for the entry
            /// </summary>
            public CustomTransfer Transfer { get; }
            /// <summary>
            /// The reach time
            /// </summary>
            public DateTime Time { get; }
            internal CustomTransferReach(CustomTransfer transfer, DateTime reachTime)
            {
                Transfer = transfer;
                Time = reachTime;
            }

            public override string ToString()
            {
                return "CustomTransferReach at " + Time.ToShortTimeString() + ": " + Transfer.GetSrcRoutePoint().Name + " to " + Transfer.GetDestRoutePoint().Name;
            }
        }

        /// <summary>
        /// Class representing a reach of a stop by a bike trip
        /// </summary>
        public class BikeTripReach : IEntry
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
            internal BikeTripReach(BikeStation from, BikeStation to, DateTime reachTime)
            {
                this.From = from;
                this.To = to;
                this.Time = reachTime;
            }

            public override string ToString()
            {
                return "BikeTripReach at " + Time.ToShortTimeString() + ": " + From.Name + " to " + To.Name;
            }
        }

        /// <summary>
        /// Class representing a reach of a stop by an implicit search start
        /// </summary>
        public class ImplicitSearchStartReach : IEntry
        {
            /// <summary>
            /// The time at which the RoutePoint was reached
            /// </summary>
            public DateTime Time { get; protected set; }
            public ImplicitSearchStartReach(DateTime reachTime)
            {
                this.Time = reachTime;
            }

            public override string ToString()
            {
                return "ImplicitSearchStartReach at " + Time.ToShortTimeString();
            }
        }

        /// <summary>
        /// Interface for an entry. An entry can either be an arrival entry or a departure entry, based on the search direction.
        /// </summary>
        public interface IEntry
        {
            /// <summary>
            /// The best time of the entry
            /// </summary>
            public DateTime Time { get; }
        }

        /// <summary>
        /// The current earliest best time at which the RoutePoint was reached
        /// </summary>
        public DateTime BestTime { get; set; }

        /// <summary>
        /// Array of entries holding the best arrival/departure for a RoutePoint for every round of the search.
        /// </summary>
        internal IEntry[] Entries { get; set; }

        
        /// <summary>
        /// Creates a new StopRoutingInfo object
        /// </summary>
        /// <param name="forward">Whether the search is in the forward direction</param>
        public StopRoutingInfo(bool forward)
        {
            BestTime = forward ? DateTime.MaxValue : DateTime.MinValue;

            Entries = new IEntry[Settings.ROUNDS + 1];
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
        public IEntry[] Reaches
        {
            get => Entries;
            set => Entries = value;
        }

    }
}

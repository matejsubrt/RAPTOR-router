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
    /// <summary>
    /// Base class used to hold routing information at a single RoutePoint.
    /// </summary>
    /// <remarks>The derived forward and backward classes do not have any additional fields or properties, they only serve to access the properties with a different (better understandable) name in their respective contexts.</remarks>
    public abstract class StopRoutingInfoBase
    {
        /// <summary>
        /// The current earliest best time at which the RoutePoint was reached
        /// </summary>
        public DateTime BestTime { get; set; }

        /// <summary>
        /// Array of entries holding the best arrival/departure for a RoutePoint for every round of the search.
        /// </summary>
        internal IEntry[] Entries { get; set; }
        
        /// <summary>
        /// Interface for a entry. An entry can either be an arrival entry or a departure entry, based on the search direction.
        /// </summary>
        public interface IEntry
        {
            /// <summary>
            /// The best time of the entry
            /// </summary>
            public DateTime Time { get; }
        }

        /// <summary>
        /// Base class for a trip entry
        /// </summary>
        public abstract class TripEntry : IEntry
        {
            /// <summary>
            /// The trip used to reached the stop
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
        }

        /// <summary>
        /// Class representing an arrival by trip to a stop
        /// </summary>
        public class TripArrival : TripEntry
        {
            /// <summary>
            /// The stop at which the trip was boarded to get to current stop.
            /// </summary>
            public Stop GetOnStop
            {
                get => ReachedFromStop;
            }
            /// <summary>
            /// The arrival time at current stop using this trip arrival
            /// </summary>
            public DateTime ArrivalTime
            {
                get => Time;
            }
            /// <summary>
            /// Creates a new TripArrival object
            /// </summary>
            /// <param name="trip">The trip to reach the stop</param>
            /// <param name="getOnStop">The stop at which the trip was boarded to get to the current stop</param>
            /// <param name="arrivalTime">The time at which the trip arrives at the current stop</param>
            internal TripArrival(Trip trip, Stop getOnStop, DateTime arrivalTime)
            {
                this.Trip = trip;
                this.ReachedFromStop = getOnStop;
                this.Time = arrivalTime;
            }
            public override string ToString()
            {
                return "TripArrival at " + Time.ToShortTimeString() + ": " + Trip.Route.ShortName + " from " + GetOnStop.Name;
            }
        }

        /// <summary>
        /// Class representing a departure by trip from a stop
        /// </summary>
        public class TripDeparture : TripEntry
        {
            /// <summary>
            /// The stop, at which the trip will be exited to get to destination (i.e. the stop from which we got to current stop during search)
            /// </summary>
            public Stop GetOffStop
            {
                get => ReachedFromStop;
            }
            /// <summary>
            /// Creates a new TripDeparture object
            /// </summary>
            /// <param name="trip">The trip to reach the stop</param>
            /// <param name="getOffStop">The stop at which the trip should be exited to get to the destination</param>
            /// <param name="departureTime">The time at which the trip departs from the current stop</param>
            internal TripDeparture(Trip trip, Stop getOffStop, DateTime departureTime)
            {
                this.Trip = trip;
                this.ReachedFromStop = getOffStop;
                this.Time = departureTime;
            }
            public override string ToString()
            {
                return "TripDeparture at " + Time.ToShortTimeString() + ": " + Trip.Route.ShortName + " to " + GetOffStop.Name;
            }
        }

        /// <summary>
        /// Base class for a transfer entry
        /// </summary>
        public abstract class TransferEntry : IEntry
        {
            /// <summary>
            /// The transfer used for the entry
            /// </summary>
            public Transfer Transfer { get; protected set; }
            /// <summary>
            /// The time at which the stop was reached
            /// </summary>
            public DateTime Time { get; protected set; }
        }

        /// <summary>
        /// Class representing an arrival by transfer to a stop
        /// </summary>
        public class TransferArrival : TransferEntry
        {
            /// <summary>
            /// Creates a new TransferArrival object
            /// </summary>
            /// <param name="transfer">The transfer used to reach the stop</param>
            /// <param name="arrivalTime">The time at which the stop was reached using the transfer</param>
            internal TransferArrival(Transfer transfer, DateTime arrivalTime)
            {
                this.Transfer = transfer;
                this.Time = arrivalTime;
            }
            public override string ToString()
            {
                return "TransferArrival at " + Time.ToShortTimeString() + ": " + Transfer.From.Name + " to >" + Transfer.To.Name + "<";
            }
        }


        /// <summary>
        /// Class representing a departure by transfer from current stop
        /// </summary>
        public class TransferDeparture : TransferEntry
        {
            /// <summary>
            /// Creates a new TransferDeparture object
            /// </summary>
            /// <param name="transfer">Transfer used to "reach" the stop (i.e. transfer to be used to get from current stop nearer to destination)</param>
            /// <param name="departureTime">The time at the stop has to be departued from to get to destination on time</param>
            internal TransferDeparture(Transfer transfer, DateTime departureTime)
            {
                this.Transfer = transfer;
                this.Time = departureTime;
            }
            public override string ToString()
            {
                return "TransferDeparture at " + Time.ToShortTimeString() + ": >" + Transfer.From.Name + "< to " + Transfer.To.Name;
            }
        }

        /// <summary>
        /// Base class for a bike transfer entry
        /// </summary>
        public abstract class BikeTransferEntry : IEntry
        {
            /// <summary>
            /// The bike transfer used for the entry
            /// </summary>
            public BikeTransfer Transfer { get; protected set; }
            /// <summary>
            /// The time at which the RoutePoint was reached
            /// </summary>
            public DateTime Time { get; protected set; }
        }

        /// <summary>
        /// Class representing an arrival by bike transfer to a RoutePoint
        /// </summary>
        public class BikeTransferArrival : BikeTransferEntry
        {
            /// <summary>
            /// Creates a new BikeTransferARrival object
            /// </summary>
            /// <param name="transfer">The transfer used to reach the RoutePoint</param>
            /// <param name="arrivalTime">The time at which the RoutePoint was reached</param>
            internal BikeTransferArrival(BikeTransfer transfer, DateTime arrivalTime)
            {
                this.Transfer = transfer;
                this.Time = arrivalTime;
            }
            public override string ToString()
            {
                return "BikeTransferArrival at " + Time.ToShortTimeString() + ": " + Transfer.GetSrcRoutePoint().Name + " to >" + Transfer.GetDestRoutePoint().Name + "<";
            }
        }

        /// <summary>
        /// Class representing a departure by bike transfer from a RoutePoint
        /// </summary>
        public class BikeTransferDeparture : BikeTransferEntry
        {
            /// <summary>
            /// Creates a new BikeTransferDeparture object
            /// </summary>
            /// <param name="transfer">Transfer used to "reach" the RoutePoint (i.e. transfer to be used to get from current RoutePoint nearer to destination)</param>
            /// <param name="departureTime">The time at the RoutePoint has to be departued from to get to destination on time</param>
            internal BikeTransferDeparture(BikeTransfer transfer, DateTime departureTime)
            {
                this.Transfer = transfer;
                this.Time = departureTime;
            }
            public override string ToString()
            {
                return "BikeTransferDeparture at " + Time.ToShortTimeString() + ": >" + Transfer.GetSrcRoutePoint().Name + "< to " + Transfer.GetDestRoutePoint().Name;
            }
        }

        /// <summary>
        /// Base class representing a custom transfer entry
        /// </summary>
        public abstract class CustomTransferEntry : IEntry
        {
            /// <summary>
            /// The custom transfer used for the entry
            /// </summary>
            public CustomTransfer Transfer { get; protected set; }
            /// <summary>
            /// The time at which the RoutePoint was reached
            /// </summary>
            public DateTime Time { get; protected set; }
        }

        /// <summary>
        /// Class representing an arrival by custom transfer (from/to a custom RoutePoint)
        /// </summary>
        public class CustomTransferArrival : CustomTransferEntry
        {
            /// <summary>
            /// Creates a new CustomTransferArrival object
            /// </summary>
            /// <param name="transfer">Transfer used to reach the RoutePoint</param>
            /// <param name="departureTime">The time at which the RoutePoint was reached</param>
            internal CustomTransferArrival(CustomTransfer transfer, DateTime arrivalTime)
            {
                this.Transfer = transfer;
                this.Time = arrivalTime;
            }
            public override string ToString()
            {
                return "CustomTransferArrival at " + Time.ToShortTimeString() + ": " + Transfer.GetSrcRoutePoint().Name + " to >" + Transfer.GetDestRoutePoint().Name + "<";
            }
        }

        /// <summary>
        /// Class representing a departure by custom transfer (from/to a custom RoutePoint)
        /// </summary>
        public class CustomTransferDeparture : CustomTransferEntry
        {
            /// <summary>
            /// Creates a new CustomTransferDeparture object
            /// </summary>
            /// <param name="transfer">Transfer used to "reach" the RoutePoint (i.e. transfer to be used to get from current RoutePoint nearer to destination)</param>
            /// <param name="departureTime">The time at the RoutePoint has to be departued from to get to destination on time</param>
            internal CustomTransferDeparture(CustomTransfer transfer, DateTime departureTime)
            {
                this.Transfer = transfer;
                this.Time = departureTime;
            }
            public override string ToString()
            {
                return "CustomTransferDeparture at " + Time.ToShortTimeString() + ": >" + Transfer.GetSrcRoutePoint().Name + "< to " + Transfer.GetDestRoutePoint().Name;
            }
        }

        /// <summary>
        /// Base class for a bike trip entry
        /// </summary>
        public abstract class BikeTripEntry : IEntry
        {
            /// <summary>
            /// The source bike station of the bike trip
            /// </summary>
            public BikeStation From { get; protected set; }
            /// <summary>
            /// The destination bike station of the bike trip (i.e. the current station)
            /// </summary>
            public BikeStation To { get; protected set; }
            /// <summary>
            /// The time at which the current bike station was reached
            /// </summary>
            public DateTime Time { get; protected set; }
        }

        /// <summary>
        /// Class representing an arrival by bike trip
        /// </summary>
        public class BikeTripArrival : BikeTripEntry
        {
            /// <summary>
            /// Creates a new BikeTripArrival object
            /// </summary>
            /// <param name="from">The source station of the trip</param>
            /// <param name="to">The destination station of the trip (i.e. the current station)</param>
            /// <param name="arrivalTime">The time at which the destination station was reached</param>
            internal BikeTripArrival(BikeStation from, BikeStation to, DateTime arrivalTime)
            {
                this.From = from;
                this.To = to;
                this.Time = arrivalTime;
            }
            public override string ToString()
            {
                return "BikeTripArrival at " + Time.ToShortTimeString() + ": " + From.Name + " to >" + To.Name + "<";
            }
        }

        /// <summary>
        /// Class representing a depparture by bike trip
        /// </summary>
        public class BikeTripDeparture : BikeTripEntry
        {
            /// <summary>
            /// Creates a new BikeTripDeparture object
            /// </summary>
            /// <param name="from">The source station of the trip (i.e. the current station)</param>
            /// <param name="to">The destination station of the trip</param>
            /// <param name="departureTime">The time at which the station has to be departed to reach the destination on time</param>
            internal BikeTripDeparture(BikeStation from, BikeStation to, DateTime departureTime)
            {
                this.From = from;
                this.To = to;
                this.Time = departureTime;
            }
            public override string ToString()
            {
                return "BikeTripDeparture at " + Time.ToShortTimeString() + ": >" + From.Name + "< to " + To.Name;
            }
        }

        /// <summary>
        /// Base class for an implicit entry (used for the source/destination stations)
        /// </summary>
        public abstract class ImplicitEntry : IEntry
        {
            /// <summary>
            /// The time at which the RoutePoint was reached
            /// </summary>
            public DateTime Time { get; protected set; }
        }

        /// <summary>
        /// Class representing an implicit start departure -> the earliest time of departure
        /// </summary>
        /// <remarks>For forward searches</remarks>
        public class ImplicitStartDeparture : ImplicitEntry
        {
            /// <summary>
            /// Creates a new ImplicitStartDeparture object
            /// </summary>
            /// <param name="arrivalTime">The earliest possible departure time from the current (source) RoutePoint</param>
            public ImplicitStartDeparture(DateTime arrivalTime)
            {
                this.Time = arrivalTime;
            }
            public override string ToString()
            {
                return "ImplicitStartDeparture at " + Time.ToShortTimeString();
            }
        }

        /// <summary>
        /// Class representing an implicit end arrival -> the latest time of arrival to the destination
        /// </summary>
        /// <remarks>For backward searches</remarks>
        public class ImplicitEndArrival : ImplicitEntry
        {
            /// <summary>
            /// Creates a new ImplicitEndArrival object
            /// </summary>
            /// <param name="departureTime">The latest possible arrival time to the current (destination) RoutePoint</param>
            public ImplicitEndArrival(DateTime departureTime)
            {
                this.Time = departureTime;
            }
            public override string ToString()
            {
                return "ImplicitEndArrival at " + Time.ToShortTimeString();
            }
        }
    }

    /// <summary>
    /// Class used to hold routing information at a single RoutePoint during a forward search
    /// </summary>
    public class ForwardStopRoutingInfo : StopRoutingInfoBase
    {
        /// <summary>
        /// Creates a new ForwardStopRoutingInfo object
        /// </summary>
        public ForwardStopRoutingInfo()
        {
            BestTime = DateTime.MaxValue;

            Entries = new IEntry[Settings.ROUNDS + 1];
            for (int i = 0; i < Entries.Count(); i++)
            {
                Entries[i] = null;
            }
        }
        /// <summary>
        /// Earliest arrival at the RoutePoint -> just renaming the BestTime property for better readability
        /// </summary>
        public DateTime EarliestArrival
        {
            get => BestTime;
            set => BestTime = value;
        }
        /// <summary>
        /// Earliest arrivals at the RoutePoint by rounds -> just renaming the Entries property for better readability
        /// </summary>
        public IEntry[] Arrivals
        {
            get => Entries;
            set => Entries = value;
        }
    }

    /// <summary>
    /// Class used to hold routing information at a single RoutePoint during a backward search
    /// </summary>
    public class BackwardStopRoutingInfo : StopRoutingInfoBase
    {
        /// <summary>
        /// Creates a new BackwardStopRoutingInfo object
        /// </summary>
        public BackwardStopRoutingInfo()
        {
            BestTime = DateTime.MinValue;

            Entries = new IEntry[Settings.ROUNDS + 1];
            for (int i = 0; i < Entries.Count(); i++)
            {
                Entries[i] = null;
            }
        }
        /// <summary>
        /// Latest departure at the RouterPoint to get to destination on time -> just renaming the BestTime property for better readability
        /// </summary>
        public DateTime LatestDeparture
        {
            get => BestTime;
            set => BestTime = value;
        }
        /// <summary>
        /// Latest departures at the RoutePoint to get to destination on tyme by rounds -> just renaming the Entries property for better readability
        /// </summary>
        public IEntry[] Departures
        {
            get => Entries;
            set => Entries = value;
        }
    }
}

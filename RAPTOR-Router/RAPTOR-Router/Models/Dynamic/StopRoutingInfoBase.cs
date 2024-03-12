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
    public abstract class StopRoutingInfoBase
    {
        /// <summary>
        /// The current earliest possible arrival time at the stop
        /// </summary>
        public DateTime BestTime { get; set; }

        internal IEntry[] Entries { get; set; }
        /// <summary>
        /// Creates a new StopRoutingInfo object with all the arrivalTimes set to the maxValue
        /// </summary>
        

        public interface IEntry
        {
            public DateTime Time { get; }
        }


        public abstract class TripEntry : IEntry
        {
            public Trip Trip { get; protected set; }
            public DateTime Time { get; protected set; }
            public Stop Stop { get; protected set; }
        }
        public class TripArrival : TripEntry
        {
            public Stop GetOnStop
            {
                get => Stop;
            }
            public DateTime ArrivalTime
            {
                get => Time;
            }
            internal TripArrival(Trip trip, Stop getOnStop, DateTime arrivalTime)
            {
                this.Trip = trip;
                this.Stop = getOnStop;
                this.Time = arrivalTime;
            }
            public override string ToString()
            {
                return "TripArrival at " + Time.ToShortTimeString() + ": " + Trip.Route.ShortName + " from " + GetOnStop.Name;
            }
        }
        public class TripDeparture : TripEntry
        {
            public Stop GetOffStop
            {
                get => Stop;
            }
            internal TripDeparture(Trip trip, Stop getOffStop, DateTime departureTime)
            {
                this.Trip = trip;
                this.Stop = getOffStop;
                this.Time = departureTime;
            }
            public override string ToString()
            {
                return "TripDeparture at " + Time.ToShortTimeString() + ": " + Trip.Route.ShortName + " to " + GetOffStop.Name;
            }
        }


        public abstract class TransferEntry : IEntry
        {
            public Transfer Transfer { get; protected set; }
            public DateTime Time { get; protected set; }
        }
        public class TransferArrival : TransferEntry
        {
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
        public class TransferDeparture : TransferEntry
        {
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


        public abstract class BikeTransferEntry : IEntry
        {
            public BikeTransfer Transfer { get; protected set; }
            public DateTime Time { get; protected set; }
        }
        public class BikeTransferArrival : BikeTransferEntry
        {
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
        public class BikeTransferDeparture : BikeTransferEntry
        {
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


        public abstract class CustomTransferEntry : IEntry
        {
            public CustomTransfer Transfer { get; protected set; }
            public DateTime Time { get; protected set; }
        }
        public class CustomTransferArrival : CustomTransferEntry
        {
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
        public class CustomTransferDeparture : CustomTransferEntry
        {
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


        public abstract class BikeTripEntry : IEntry
        {
            public BikeStation From { get; protected set; }
            public BikeStation To { get; protected set; }
            public DateTime Time { get; protected set; }
        }
        public class BikeTripArrival : BikeTripEntry
        {
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
        public class BikeTripDeparture : BikeTripEntry
        {
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

        public abstract class  ImplicitEntry : IEntry
        {
            public DateTime Time { get; protected set; }
        }
        public class ImplicitStartDeparture : ImplicitEntry
        {
            public ImplicitStartDeparture(DateTime arrivalTime)
            {
                this.Time = arrivalTime;
            }
            public override string ToString()
            {
                return "ImplicitStartDeparture at " + Time.ToShortTimeString();
            }
        }

        public class ImplicitEndArrival : ImplicitEntry
        {
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

    public class ForwardStopRoutingInfo : StopRoutingInfoBase
    {
        public ForwardStopRoutingInfo()
        {
            BestTime = DateTime.MaxValue;

            Entries = new IEntry[Settings.ROUNDS + 1];
            for (int i = 0; i < Entries.Count(); i++)
            {
                Entries[i] = null;
            }
        }
        public DateTime EarliestArrival
        {
            get => BestTime;
            set => BestTime = value;
        }
        public IEntry[] Arrivals
        {
            get => Entries;
            set => Entries = value;
        }
    }
    public class BackwardStopRoutingInfo : StopRoutingInfoBase
    {
        public BackwardStopRoutingInfo()
        {
            BestTime = DateTime.MinValue;

            Entries = new IEntry[Settings.ROUNDS + 1];
            for (int i = 0; i < Entries.Count(); i++)
            {
                Entries[i] = null;
            }
        }
        public DateTime LatestDeparture
        {
            get => BestTime;
            set => BestTime = value;
        }
        public IEntry[] Departures
        {
            get => Entries;
            set => Entries = value;
        }
    }
}

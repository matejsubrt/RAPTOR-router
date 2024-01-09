using RAPTOR_Router.SearchModels;
using RAPTOR_Router.Routers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#pragma warning disable 1591
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RAPTOR_Router.RAPTORStructures
{
	/// <summary>
	/// Class representing a result of a connection search
	/// </summary>
	public class SearchResult
	{
		private Settings usedSettings;
		/// <summary>
		/// The trips used during the best found connection
		/// </summary>
		public List<UsedTrip> UsedTrips { get; private set; } = new List<UsedTrip>();
		/// <summary>
		/// The transfers used during the best found connection
		/// </summary>
		public List<UsedTransfer> UsedTransfers { get; private set; } = new List<UsedTransfer>();

		public List<UsedSegment> UsedSegments { get; private set; } = new List<UsedSegment>();
		public int TransferCount { get; private set; }
		public int TripCount { get; private set; }

		//TODO: add time support
		public DateTime DepartureDateTime { get; set; }
		public DateTime ArrivalDateTime { get; set; }


		internal SearchResult(Settings settings)
		{
			this.usedSettings = settings;
		}

		/// <summary>
		/// Creates an used trip from the provided arguments, pushes it TO THE START of UsedSegments
		/// </summary>
		/// <param name="trip">The trip to add</param>
		/// <param name="getOnStop">The get on stop of this segment</param>
		/// <param name="getOffStop">The get off stop of this segment</param>
		internal void AddUsedTrip(Trip trip, Stop getOnStop, Stop getOffStop, DateTime destArrivalTime)
		{
			if(UsedSegments.Count == 0)
			{
                ArrivalDateTime = destArrivalTime;
            }

			

			UsedTrip usedTrip = new UsedTrip();

			usedTrip.segmentIndex = 0; // TODO: remove this
			usedTrip.getOnStopIndex = trip.Route.RouteStops.IndexOf(getOnStop);
			usedTrip.getOffStopIndex = trip.Route.RouteStops.IndexOf(getOffStop);
			usedTrip.stops = (from stop in trip.Route.RouteStops select stop.Name).ToList();
            usedTrip.stopIds = (from stop in trip.Route.RouteStops select stop.Id).ToList();
            usedTrip.getOnTime = trip.StopTimes[trip.Route.RouteStops.IndexOf(getOnStop)].DepartureTime;
			usedTrip.getOffTime = trip.StopTimes[trip.Route.RouteStops.IndexOf(getOffStop)].ArrivalTime;
			usedTrip.routeName = trip.Route.ShortName;
			usedTrip.Color = trip.Route.Color;

			usedTrip.getOffDateTime = destArrivalTime;

			// trip goes over midnight
			if(usedTrip.getOnTime > usedTrip.getOffTime)
			{
				var modifiedGetOffTime = usedTrip.getOffDateTime.AddDays(-1);

                DateOnly getOnDate = new DateOnly(modifiedGetOffTime.Year, modifiedGetOffTime.Month, modifiedGetOffTime.Day);
				usedTrip.getOnDateTime = new DateTime(getOnDate.Year, getOnDate.Month, getOnDate.Day, usedTrip.getOnTime.Hour, usedTrip.getOnTime.Minute, usedTrip.getOnTime.Second);
			}
			else
			{
				usedTrip.getOnDateTime = new DateTime(usedTrip.getOffDateTime.Year, usedTrip.getOffDateTime.Month, usedTrip.getOffDateTime.Day, usedTrip.getOnTime.Hour, usedTrip.getOnTime.Minute, usedTrip.getOnTime.Second);
			}

            DepartureDateTime = usedTrip.getOnDateTime;

            UsedSegments.Insert(0, usedTrip);
			UsedTrips.Insert(0, usedTrip);
			TripCount++;
        }
		/// <summary>
		/// Creates an used transfer from the provided transfer, pushes it TO THE START of UsedSegments
		/// </summary>
		/// <param name="transfer">The transfer to add</param>
		internal void AddUsedTransfer(Transfer transfer, DateTime destArrivalTime)
		{
            if (UsedSegments.Count == 0)
            {
                ArrivalDateTime = destArrivalTime;
            }

            UsedTransfer usedTransfer = new UsedTransfer();

			usedTransfer.segmentIndex = 0; //TODO: remove
			usedTransfer.srcStopName = transfer.From.Name;
			usedTransfer.destStopName = transfer.To.Name;
			usedTransfer.srcStopId = transfer.From.Id;
			usedTransfer.destStopId = transfer.To.Id;
			usedTransfer.distance = transfer.Distance;
			usedTransfer.time = transfer.GetTransferTime(usedSettings.WalkingPace);

			//UsedSegments.Add(usedTransfer);
			UsedSegments.Insert(0, usedTransfer);
			UsedTransfers.Insert(0, usedTransfer);
			TransferCount++;
		}

		public override string ToString()
		{
			StringBuilder sb = new();
			foreach(UsedSegment s in UsedSegments)
			{
				switch (s.segmentType)
				{
					case SegmentType.Transfer:
						UsedTransfer t = (UsedTransfer)s;
						sb.AppendLine(("Transfer from " + t.srcStopName + " to " + t.destStopName + ", length: " + t.time + "s + reserve " + (usedSettings.GetMovingTransferLengthMultiplier() - 1.0) * t.time + "s = " + t.distance + "m"));
						break;
					case SegmentType.Trip:
						UsedTrip tr = (UsedTrip)s;
						sb.AppendLine(tr.getOnTime.ToLongTimeString() + " - " + tr.getOffTime.ToLongTimeString() + ": Line " + tr.routeName + " from " + tr.stops[tr.getOnStopIndex] + " to " + tr.stops[tr.getOffStopIndex]);
						break;
					default:
						sb.AppendLine("INVALID SEGMENT TYPE");
						break;
				}
			}
			return sb.ToString();
		}

		public string ToStringOld()
		{
			if(UsedTrips.Count == 1 && UsedTransfers.Count == 0)
			{
				return UsedTrips[0].getOnTime.ToLongTimeString() + " - " + UsedTrips[0].getOffTime.ToLongTimeString() + ": Line " + UsedTrips[0].routeName + " from " + UsedTrips[0].stops[UsedTrips[0].getOnStopIndex] + " to " + UsedTrips[0].stops[UsedTrips[0].getOffStopIndex];
			}
			else if(UsedTrips.Count == 0 && UsedTransfers.Count == 1)
			{
				return "Transfer from " + UsedTransfers[0].srcStopName + " to " + UsedTransfers[0].destStopName + ", length: " + UsedTransfers[0].time + "s + reserve " + (usedSettings.GetMovingTransferLengthMultiplier() - 1.0) * UsedTransfers[0].time + "s = " + UsedTransfers[0].distance + "m";
			}


			StringBuilder sb = new StringBuilder();

			int segmentIndex = 0;
			int tripIndex = 0;
			int transferIndex = 0;
			TimeOnly departureTime;
			TimeOnly arrivalTime;
			if (UsedTrips[0].segmentIndex == 0)
			{
				departureTime = UsedTrips[0].getOnTime;
			}
			else
			{
				departureTime = UsedTrips[0].getOnTime.AddSeconds(-UsedTransfers[0].time);
			}

			if (UsedTrips[UsedTrips.Count -1].segmentIndex > UsedTransfers[UsedTransfers.Count - 1].segmentIndex)
			{
				arrivalTime = UsedTrips[UsedTrips.Count - 1].getOffTime;
			}
			else
			{
				arrivalTime = UsedTrips[UsedTrips.Count - 1].getOffTime.AddSeconds(UsedTransfers[UsedTransfers.Count - 1].time);
			}

			while(segmentIndex <= Math.Max(UsedTrips[UsedTrips.Count-1].segmentIndex, UsedTransfers[UsedTransfers.Count - 1].segmentIndex))
			{
				if (tripIndex < UsedTrips.Count && UsedTrips[tripIndex].segmentIndex == segmentIndex)
				{
					sb.AppendLine(UsedTrips[tripIndex].getOnTime.ToLongTimeString() + " - " + UsedTrips[tripIndex].getOffTime.ToLongTimeString() + ": Line " + UsedTrips[tripIndex].routeName + " from " + UsedTrips[tripIndex].stops[UsedTrips[tripIndex].getOnStopIndex] + " to " + UsedTrips[tripIndex].stops[UsedTrips[tripIndex].getOffStopIndex]);

					tripIndex++;
					segmentIndex++;
				}
				else if (transferIndex <UsedTransfers.Count && UsedTransfers[transferIndex].segmentIndex == segmentIndex)
				{
					sb.AppendLine("Transfer from " + UsedTransfers[transferIndex].srcStopName + " to " + UsedTransfers[transferIndex].destStopName + ", length: " + UsedTransfers[transferIndex].time + "s + reserve " + (usedSettings.GetMovingTransferLengthMultiplier() - 1.0) * UsedTransfers[transferIndex].time + "s = " + UsedTransfers[transferIndex].distance + "m");
					transferIndex++;
					segmentIndex++;
				}
			}

			TimeSpan length = new TimeSpan(arrivalTime.Ticks - departureTime.Ticks);
			sb.AppendLine("Total time: " + length);
			sb.AppendLine("Penalty seconds: " + UsedTransfers.Count*usedSettings.GetTransferPenaltySeconds());
			sb.AppendLine("Total time w penalties: " + length.Add(new TimeSpan((long)UsedTransfers.Count * usedSettings.GetTransferPenaltySeconds() * 10_000_000)));
			return sb.ToString();
		}

		public enum SegmentType
		{
			Transfer = 0,
			Trip = 1,
			Bike = 2
		}

		public interface UsedSegment
		{
			public string? ToString();
			public SegmentType segmentType { get; }
			public string GetStartStopName();
			public string GetEndStopName();
		}

		/// <summary>
		/// Class representing a trip used in a found connection
		/// </summary>
		public class UsedTrip : UsedSegment
		{
            public SegmentType segmentType { get; private set; }
            public UsedTrip()
            {
                this.segmentType = SegmentType.Trip;
            }
            /// <summary>
            /// The index of this trip in the list of links on the connection (including the transfers)
            /// </summary>
            public int segmentIndex { get; set; }
			/// <summary>
			/// The list of stop names the trip passes through
			/// </summary>
			public List<string> stops { get; set; } = new List<string>();
			public List<string> stopIds { get; set; } = new List<string>();
			/// <summary>
			/// The index of the stop where the trip is boarded
			/// </summary>
			public int getOnStopIndex { get; set; }
			/// <summary>
			/// The index of the stop where the trip is gotten out of
			/// </summary>
			public int getOffStopIndex { get; set; }
			/// <summary>
			/// The time when the trip is boarded
			/// </summary>
			public TimeOnly getOnTime { get; set; }
			/// <summary>
			/// The timr when the trip is gotten out of
			/// </summary>
			public TimeOnly getOffTime { get; set; }

			public DateTime getOnDateTime { get; set; }
			public DateTime getOffDateTime { get; set; }
			/// <summary>
			/// The name (i.e. the headsign) of the route of the trip
			/// </summary>
			public string routeName { get; set; }
			public Color Color { get; set; }

			public string GetStartStopName()
			{
				return stops[getOnStopIndex];
			}
			public string GetEndStopName()
			{
				return stops[getOffStopIndex];
			}
		}
		/// <summary>
		/// Class representing a used transfer in a found connection
		/// </summary>
		public class UsedTransfer : UsedSegment
		{
			public SegmentType segmentType { get; private set; }
			public UsedTransfer()
			{
                this.segmentType = SegmentType.Transfer;
            }
			/// <summary>
			/// The index of this trip in the list of links on the connection (including the transfers)
			/// </summary>
			public int segmentIndex { get; set; }
			/// <summary>
			/// The name of the stop where the transfer begins
			/// </summary>
			public string srcStopName { get; set; }
			/// <summary>
			/// The name of the stop where the transfer ends
			/// </summary>
			public string destStopName { get; set; }
			public string srcStopId { get; set; }
			public string destStopId { get; set; }
			/// <summary>
			/// The approximate number of seconds it takes to walk this transfer
			/// </summary>
			public int time { get; set; }
			/// <summary>
			/// The straight line distance between the 2 stops in the transfer in meters
			/// </summary>
			public int distance { get; set; }

			public string GetStartStopName()
			{
				return srcStopName;
			}
			public string GetEndStopName()
			{
				return destStopName;
			}
		}
	}
}

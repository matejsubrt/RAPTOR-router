using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.Models.Dynamic;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Custom;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Transit;

namespace UnitTests
{
	[TestClass]
	public class SearchResultTests
	{
		private SearchResult globalResult;

		public void ResetGlobalResult()
		{
			globalResult = new();

			Stop s1 = new Stop("stopId1", "stopName1", 0.0, 0.0);
			Stop s2 = new Stop("stopId2", "stopName2", 0.0, 0.0);

			BikeStation bs1 = new BikeStation("bsId1", "bsName1", 0.0, 0.0, 0, 1);
			BikeStation bs2 = new BikeStation("bsId2", "bsName2", 0.0, 0.0, 0, 2);

			

			List<GTFSStopTime> stopTimes = new List<GTFSStopTime>()
            {
                new GTFSStopTime { TripId = "tripId1", ArrivalTime = new TimeOnly(0, 0, 1), DepartureTime = new TimeOnly(0, 0, 2), StopId = "stopId1" },
				new GTFSStopTime { TripId = "tripId2", ArrivalTime = new TimeOnly(0, 0, 3), DepartureTime = new TimeOnly(0, 0, 4), StopId = "stopId2" }
            };

			GTFSRoute gtfsRoute1 = new GTFSRoute { Color = "ABABAB", Id = "gtfsRouteId1", LongName = "LN1", ShortName = "SN1", Type = 1 };
			Route route1 = new("routeId1", gtfsRoute1);
			route1.RouteStops.Add(s1);
			route1.RouteStops.Add(s2);

			Trip trip1 = new(stopTimes, route1, "tripId1");

			

			globalResult.AddUsedTrip(trip1, new DateOnly(), s1, s2, false, 1, 2, true);
            //globalResult.UsedTrips[0].getOnStopIndex = 0;
			//globalResult.UsedTrips[0].getOffStopIndex = 1;

			globalResult.AddUsedTransfer(new Transfer(s1, s2, 100), true);

			globalResult.AddUsedBikeTrip(bs1, bs2, 100, true);
		}

        public void ResetGlobalResult2()
        {
            globalResult = new();

            Stop s1 = new Stop("stopId1", "stopName1", 0.0, 0.0);
            Stop s2 = new Stop("stopId2", "stopName2", 0.0, 0.0);
			Stop s3 = new Stop("stopId3", "stopName3", 0.0, 0.0);




            List<GTFSStopTime> stopTimes1 = new List<GTFSStopTime>()
            {
                new GTFSStopTime { TripId = "tripId1", ArrivalTime = new TimeOnly(0, 0, 1), DepartureTime = new TimeOnly(0, 0, 2), StopId = "stopId1" },
                new GTFSStopTime { TripId = "tripId2", ArrivalTime = new TimeOnly(0, 0, 3), DepartureTime = new TimeOnly(0, 0, 4), StopId = "stopId2" }
            };

			List<GTFSStopTime> stopTimes2 = new List<GTFSStopTime>()
            {
				   new GTFSStopTime { TripId = "tripId2", ArrivalTime = new TimeOnly(0, 10, 1), DepartureTime = new TimeOnly(0, 10, 2), StopId = "stopId2" },
                   new GTFSStopTime { TripId = "tripId2", ArrivalTime = new TimeOnly(0, 10, 3), DepartureTime = new TimeOnly(0, 10, 4), StopId = "stopId3" }
            };

            GTFSRoute gtfsRoute1 = new GTFSRoute { Color = "ABABAB", Id = "gtfsRouteId1", LongName = "LN1", ShortName = "SN1", Type = 1 };
            Route route1 = new("routeId1", gtfsRoute1);
            route1.RouteStops.Add(s1);
            route1.RouteStops.Add(s2);

            Trip trip1 = new(stopTimes1, route1, "tripId1");

			GTFSRoute gtfsRoute2 = new GTFSRoute { Color = "ABABAB", Id = "gtfsRouteId2", LongName = "LN2", ShortName = "SN2", Type = 1 };
			Route route2 = new("routeId2", gtfsRoute2);
			route2.RouteStops.Add(s2);
			route2.RouteStops.Add(s3);

			Trip trip2 = new(stopTimes2, route2, "tripId2");



            globalResult.AddUsedTrip(trip1, new DateOnly(), s1, s2, false, 1, 2, true);
            //globalResult.UsedTrips[0].getOnStopIndex = 0;
            //globalResult.UsedTrips[0].getOffStopIndex = 1;

            globalResult.AddUsedTransfer(new Transfer(s2, s2, 0), true);

			globalResult.AddUsedTrip(trip2, new DateOnly(), s2, s3, true, 1, 2, true);

        }

        [TestMethod]
		public void AddUsedTripTest()
		{
			List<GTFSStopTime> stopTimes = new List<GTFSStopTime>()
            {
                new GTFSStopTime { TripId = "", ArrivalTime = new TimeOnly(), DepartureTime = new TimeOnly(), StopId = "stopId1" },
				new GTFSStopTime { TripId = "", ArrivalTime = new TimeOnly(), DepartureTime = new TimeOnly(), StopId = "stopId2" }
            };

			SearchResult result = new();
			Trip trip1 = new(stopTimes, new Route("routeId1", new GTFSRoute { Color = "ABABAB", Id = "gtfsRouteId1", LongName = "LN1", ShortName = "SN1", Type = 1 }), "tripId1");
			Trip trip2 = new(stopTimes, new Route("routeId2", new GTFSRoute { Color = "ABABAB", Id = "gtfsRouteId2", LongName = "LN2", ShortName = "SN2", Type = 1 }), "tripId2");
			Trip trip3 = new(stopTimes, new Route("routeId3", new GTFSRoute { Color = "ABABAB", Id = "gtfsRouteId3", LongName = "LN3", ShortName = "SN3", Type = 1 }), "tripId3");
			
			Assert.IsTrue(
				result.TripCount == 0 &&
				result.UsedSegmentTypes.Count == 0 &&
				result.UsedTrips.Count == 0 &&
				result.UsedTripAlternatives.Count == 0
				);

			Stop s1 = new Stop("stopId1", "stopName1", 0.0, 0.0);
			Stop s2 = new Stop("stopId2", "stopName2", 0.0, 0.0);

			result.AddUsedTrip(trip1, new DateOnly(), s1, s2, false, 0, 0, true);

			Assert.IsTrue(result.UsedTrips.Count == 1);
			Assert.AreEqual(trip1.Id, result.UsedTrips[0].tripId);

			Assert.IsTrue(result.UsedSegmentTypes.Count == 1 && result.TripCount == 1);
			Assert.AreEqual(SearchResult.SegmentType.Trip, result.UsedSegmentTypes[0]);

			result.AddUsedTrip(trip2, new DateOnly(), s1, s2, false, 0, 0, true);

			Assert.IsTrue(result.UsedTrips.Count == 2);
			Assert.AreEqual(trip2.Id, result.UsedTrips[1].tripId);

			Assert.IsTrue(result.UsedSegmentTypes.Count == 2 && result.TripCount == 2);
			Assert.AreEqual(SearchResult.SegmentType.Trip, result.UsedSegmentTypes[1]);

			result.AddUsedTrip(trip3, new DateOnly(), s1, s2, false, 0, 0, false);

			Assert.IsTrue(result.UsedTrips.Count == 3 && result.TripCount == 3);
			Assert.AreEqual(trip3.Id, result.UsedTrips[0].tripId);

			Assert.IsTrue(result.UsedSegmentTypes.Count == 3);
			Assert.AreEqual(SearchResult.SegmentType.Trip, result.UsedSegmentTypes[2]);
		}

		[TestMethod]
		public void AddUsedBikeTripTest()
		{
			SearchResult result = new();

			BikeStation bs1 = new BikeStation("bsId1", "bsName1", 0.0, 0.0, 0, 1);
			BikeStation bs2 = new BikeStation("bsId2", "bsName2", 0.0, 0.0, 0, 2);

			Assert.IsTrue(result.BikeTripCount == 0 && result.UsedBikeTrips.Count == 0);

			result.AddUsedBikeTrip(bs1, bs2, 100, true);

			Assert.IsTrue(result.BikeTripCount == 1 && result.UsedBikeTrips.Count == 1);
			Assert.AreEqual(100, result.UsedBikeTrips[0].distance);

			result.AddUsedBikeTrip(bs1, bs2, 200, true);

			Assert.IsTrue(result.BikeTripCount == 2 && result.UsedBikeTrips.Count == 2);
			Assert.AreEqual(200, result.UsedBikeTrips[1].distance);

			result.AddUsedBikeTrip(bs1, bs2, 300, false);

			Assert.IsTrue(result.BikeTripCount == 3 && result.UsedBikeTrips.Count == 3);
			Assert.AreEqual(300, result.UsedBikeTrips[0].distance);
		}

		[TestMethod]
		public void AddUsedTransferTest()
		{
			SearchResult result = new();

			Stop s1 = new Stop("stopId1", "stopName1", 0.0, 0.0);
			Stop s2 = new Stop("stopId2", "stopName2", 0.0, 0.0);
			BikeStation bs1 = new BikeStation("bsId1", "bsName1", 0.0, 0.0, 0, 1);
			CustomRoutePoint rp1 = new CustomRoutePoint("rpId1", "rpName1", new Coordinates(0.0, 0.0));

			Transfer tr1 = new Transfer(s1, s2, 100);
			ToBikeTransfer tr2 = new ToBikeTransfer(s1, bs1, 200);
			FromBikeTransfer tr3 = new FromBikeTransfer(bs1, s2, 300);
			CustomTransfer tr4 = new CustomTransfer(rp1, s2, 400);

			Assert.IsTrue(result.TransferCount == 0 && result.UsedTransfers.Count == 0);

			result.AddUsedTransfer(tr1, true);

			Assert.IsTrue(result.TransferCount == 1 && result.UsedTransfers.Count == 1);
			Assert.AreEqual(100, result.UsedTransfers[0].distance);

			result.AddUsedTransfer(tr2, true);

			Assert.IsTrue(result.TransferCount == 2 && result.UsedTransfers.Count == 2);
			Assert.AreEqual(200, result.UsedTransfers[1].distance);

			result.AddUsedTransfer(tr3, false);

			Assert.IsTrue(result.TransferCount == 3 && result.UsedTransfers.Count == 3);
			Assert.AreEqual(300, result.UsedTransfers[0].distance);

			result.AddUsedTransfer(tr4, false);

			Assert.IsTrue(result.TransferCount == 4 && result.UsedTransfers.Count == 4);
			Assert.AreEqual(400, result.UsedTransfers[0].distance);
		}

		[TestMethod]
		public void TestAllAddsTest()
		{
			ResetGlobalResult();

			Assert.IsTrue(
				//globalResult.TripCount == 1 &&
				globalResult.UsedTrips.Count == 1 &&
				globalResult.UsedSegmentTypes.Count == 3 &&
				//globalResult.TransferCount == 1 &&
				globalResult.UsedTransfers.Count == 1 &&
				//globalResult.BikeTripCount == 1 &&
			    globalResult.UsedBikeTrips.Count == 1
			);

			var firstTrip = globalResult.UsedTrips[0];
			var firstTransfer = globalResult.UsedTransfers[0];
			var firstBikeTrip = globalResult.UsedBikeTrips[0];


			Assert.AreEqual("tripId1", firstTrip.tripId);
            Assert.AreEqual(2, firstTrip.currentDelay);
			Assert.AreEqual(1, firstTrip.delayWhenBoarded);
			Assert.AreEqual("SN1", firstTrip.routeName);
            Assert.AreEqual((VehicleType)1, firstTrip.vehicleType);

			Assert.AreEqual(100, firstTransfer.distance);
			Assert.AreEqual("stopId1", firstTransfer.srcStopInfo.Id);
			Assert.AreEqual("stopId2", firstTransfer.destStopInfo.Id);

			Assert.AreEqual(100, firstBikeTrip.distance);
			Assert.AreEqual("bsId1", firstBikeTrip.srcStopInfo.Id);
			Assert.AreEqual("bsId2", firstBikeTrip.destStopInfo.Id);
        }

		[TestMethod]
		public void InitializeAlternativesTest()
		{
			ResetGlobalResult();

			globalResult.InitializeAlternatives();

			Assert.IsTrue(
				globalResult.UsedTripAlternatives.Count == 1 &&
				globalResult.UsedTripAlternatives[0].Alternatives.Count == 1 &&
				globalResult.UsedTripAlternatives[0].Count == 1 &&
				globalResult.UsedTripAlternatives[0].CurrIndex == 0
				);

			Assert.AreEqual("tripId1", globalResult.UsedTripAlternatives[0].Alternatives[0].tripId);
		}

        [TestMethod]
        public void TotalSecondsBeforeAfterTest()
        {
			ResetGlobalResult();

            int bikeTripSeconds = globalResult.UsedBikeTrips[0].time;
			int transferSeconds = globalResult.UsedTransfers[0].time;
			int totalSeconds = bikeTripSeconds + transferSeconds;

			var totalSecondsAfter = globalResult.GetTotalSecondsAfterLastTrip();
			var totalSecondsBefore = globalResult.GetTotalSecondsBeforeFirstTrip();

			Assert.AreEqual(totalSeconds, totalSecondsAfter);
			Assert.AreEqual(0, totalSecondsBefore);
        }

        [TestMethod]
        public void SetDepArrTimesTest()
        {
			ResetGlobalResult();

			globalResult.SetDepartureAndArrivalTimesByEarliestDeparture(new DateTime(2020, 1, 1, 0, 0, 0));

			Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 2).AddSeconds(1)/*delay*/, globalResult.DepartureDateTime);

			var secondsAfterLastTrip = globalResult.GetTotalSecondsAfterLastTrip();

			Assert.AreEqual(new DateTime(1, 1, 1, 0, 0, 3).AddSeconds(2)/*delay*/.AddSeconds(secondsAfterLastTrip), globalResult.ArrivalDateTime);
        }

        [TestMethod]
        public void HasLongWaitingTest()
        {
			ResetGlobalResult();

			Assert.IsFalse(globalResult.HasLongWaiting());

			ResetGlobalResult2();

			Assert.IsTrue(globalResult.HasLongWaiting());
        }
	}


}

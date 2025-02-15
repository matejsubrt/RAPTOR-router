﻿// AI has been used to create/modify some of the tests in this file
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.Structures.Custom;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Transit;

namespace UnitTests
{
    [TestClass]
    public class TripTests
    {
        private Trip? t1;
        private Trip? t2;
        [TestInitialize]
        public void Initialize()
        {
            t1 = null;
            t2 = null;

            Stop s1 = new Stop("s1", "s1", 0.0, 0.0);
            Stop s2 = new Stop("s2", "s2", 0.0, 0.0);

            GTFSRoute gr1 = new GTFSRoute
            {
                Color = "ABABAB",
                Id = "gr1",
                LongName = "gr1",
                ShortName = "gr1",
                Type = 1
            };

            Route r1 = new Route("r1", gr1);

            List<GTFSStopTime> stopTimes1 = new List<GTFSStopTime>()
            {
                new GTFSStopTime
                {
                    ArrivalTime = new TimeOnly(0, 0, 1),
                    DepartureTime = new TimeOnly(0, 0, 2),
                    StopId = "s1",
                    TripId = "t1"
                },
                new GTFSStopTime
                {
                    ArrivalTime = new TimeOnly(0, 0, 3),
                    DepartureTime = new TimeOnly(0, 0, 4),
                    StopId = "s2",
                    TripId = "t1"
                }
            };

            List<GTFSStopTime> stopTimes2 = new List<GTFSStopTime>()
            {
                new GTFSStopTime
                {
                    ArrivalTime = new TimeOnly(0, 10, 1),
                    DepartureTime = new TimeOnly(0, 10, 2),
                    StopId = "s1",
                    TripId = "t2"
                },
                new GTFSStopTime
                {
                    ArrivalTime = new TimeOnly(0, 10, 3),
                    DepartureTime = new TimeOnly(0, 10, 4),
                    StopId = "s2",
                    TripId = "t2"
                }
            };


            t1 = new Trip(stopTimes1, r1, "t1");
            t2 = new Trip(stopTimes2, r1, "t2");
        }

        [TestMethod]
        public void CompareTripsTest()
        {
            Assert.IsTrue(Trip.CompareTrips(t1!, t2!) < 0);
            Assert.IsTrue(Trip.CompareTrips(t1!, t1!) == 0);
            Assert.IsTrue(Trip.CompareTrips(t2!, t1!) > 0);
        }

        [TestMethod]
        public void GetArrDepDateTimeTest()
        {
            var t1arr = t1!.GetArrivalDateTime(0, new DateOnly(2020, 1, 1));
            var t1dep = t1.GetDepartureDateTime(1, new DateOnly(2020, 2, 1));

            Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 1), t1arr);
            Assert.AreEqual(new DateTime(2020, 2, 1, 0, 0, 4), t1dep);
        }
    }

    [TestClass]
    public class RouteTests
    {

        private Route CreateTestRoute()
        {
            var route = new Route("1", "GTFS1", "ShortName", "LongName");
            var stop1 = new Stop("Stop1", "Stop 1", 0, 0);
            var stop2 = new Stop("Stop2", "Stop 2", 1, 1);
            route.RouteStops.Add(stop1);
            route.RouteStops.Add(stop2);

            var gtfsStopTimes1 = new List<GTFSStopTime>
    {
        new GTFSStopTime { ArrivalTime = new TimeOnly(8, 0), DepartureTime = new TimeOnly(8, 5), TripId = "Trip1", StopId = "Stop1" },
        new GTFSStopTime { ArrivalTime = new TimeOnly(8, 10), DepartureTime = new TimeOnly(8, 15), TripId = "Trip1", StopId = "Stop2" }
    };

            var gtfsStopTimes2 = new List<GTFSStopTime>
    {
        new GTFSStopTime { ArrivalTime = new TimeOnly(9, 0), DepartureTime = new TimeOnly(9, 5), TripId = "Trip2", StopId = "Stop1" },
        new GTFSStopTime { ArrivalTime = new TimeOnly(9, 10), DepartureTime = new TimeOnly(9, 15), TripId = "Trip2", StopId = "Stop2" }
    };

            var trip1 = new Trip(gtfsStopTimes1, route, "Trip1");
            var trip2 = new Trip(gtfsStopTimes2, route, "Trip2");

            route.RouteTrips[DateOnly.FromDateTime(DateTime.Now)] = new List<Trip> { trip1, trip2 };

            return route;
        }

        [TestMethod]
        public void TestGetFirstStopIndex()
        {
            var route = CreateTestRoute();
            var stop = route.RouteStops[0];
            int index = route.GetFirstStopIndex(stop);
            Assert.AreEqual(0, index);
        }

        [TestMethod]
        public void TestGetLastStopIndex()
        {
            var route = CreateTestRoute();
            var stop = route.RouteStops[1];
            int index = route.GetLastStopIndex(stop);
            Assert.AreEqual(1, index);
        }

        [TestMethod]
        public void TestGetFirstNTripTimesAtStop()
        {
            var route = CreateTestRoute();
            var stop = route.RouteStops[0];

            var dateTime1 = DateTime.Now.Date.AddHours(7).AddMinutes(30);
            var times = route.GetFirstNTripTimesAtStop(stop, dateTime1, 2, true);

            var dateTime2 = dateTime1.AddHours(1).AddSeconds(1);
            var times2 = route.GetFirstNTripTimesAtStop(stop, dateTime2, 2, true);

            Assert.AreEqual(2, times.Count);
            Assert.AreEqual(dateTime1.Date.AddHours(8).AddMinutes(5), times[0]);
            Assert.AreEqual(dateTime1.Date.AddHours(9).AddMinutes(5), times[1]);

            Assert.AreEqual(1, times2.Count);
            Assert.AreEqual(dateTime1.Date.AddHours(9).AddMinutes(5), times2[0]);
        }

        [TestMethod]
        public void TestGetTripTimesAtStopWithinRange()
        {
            var route = CreateTestRoute();
            var stop = route.RouteStops[0];
            var rangeStart = DateTime.Now.Date.AddHours(7).AddMinutes(30);
            var rangeEnd = rangeStart.AddHours(3);
            var times = route.GetTripTimesAtStopWithinRange(stop, rangeStart, rangeEnd, true);
            Assert.IsTrue(times.Count > 0);
            Assert.AreEqual(rangeStart.Date.AddHours(8).AddMinutes(5), times[0]);
            Assert.AreEqual(rangeStart.Date.AddHours(9).AddMinutes(5), times[1]);
        }

        [TestMethod]
        public void TestGetFirstTransferableTripAtStopByReachTime()
        {
            var route = CreateTestRoute();
            var stop = route.RouteStops[0];
            var dateTime = DateTime.Now.AddHours(-DateTime.Now.Hour + 7);
            var delayModel = new MockDelayModel();
            var trip = route.GetFirstTransferableTripAtStopByReachTime(true, stop, dateTime, delayModel, out DateOnly tripStartDate);
            Assert.IsNotNull(trip);
            Assert.AreEqual("Trip1", trip.Id);
            Assert.AreEqual(DateOnly.FromDateTime(dateTime), tripStartDate);
        }

    }

    [TestClass]
    public class StopTimeTests
    {
        [TestMethod]
        public void GetArrDepDateTimeTest()
        {
            StopTime st1 = new StopTime
            {
                ArrivalTime = new TimeOnly(0, 0, 1),
                DepartureTime = new TimeOnly(0, 0, 2),
                DaysAfterTripStartArrival = 0,
                DaysAfterTripStartDeparture = 0
            };

            StopTime st2 = new StopTime
            {
                ArrivalTime = new TimeOnly(0, 0, 1),
                DepartureTime = new TimeOnly(0, 0, 2),
                DaysAfterTripStartArrival = 0,
                DaysAfterTripStartDeparture = 1
            };

            StopTime st4 = new StopTime
            {
                ArrivalTime = new TimeOnly(0, 0, 1),
                DepartureTime = new TimeOnly(0, 0, 2),
                DaysAfterTripStartArrival = 1,
                DaysAfterTripStartDeparture = 1
            };

            var st1arr = st1.GetArrivalDateTime(new DateOnly(2020, 1, 1));
            var st1dep = st1.GetDepartureDateTime(new DateOnly(2020, 1, 1));

            var st2arr = st2.GetArrivalDateTime(new DateOnly(2020, 1, 1));
            var st2dep = st2.GetDepartureDateTime(new DateOnly(2020, 1, 1));

            var st3arr = st4.GetArrivalDateTime(new DateOnly(2020, 1, 1));
            var st3dep = st4.GetDepartureDateTime(new DateOnly(2020, 1, 1));

            DateTime correctArrDay0 = new DateTime(2020, 1, 1, 0, 0, 1);
            DateTime correctDepDay0 = new DateTime(2020, 1, 1, 0, 0, 2);

            DateTime correctArrDay1 = new DateTime(2020, 1, 2, 0, 0, 1);
            DateTime correctDepDay1 = new DateTime(2020, 1, 2, 0, 0, 2);

            Assert.AreEqual(correctArrDay0, st1arr);
            Assert.AreEqual(correctDepDay0, st1dep);

            Assert.AreEqual(correctArrDay0, st2arr);
            Assert.AreEqual(correctDepDay1, st2dep);

            Assert.AreEqual(correctArrDay1, st3arr);
            Assert.AreEqual(correctDepDay1, st3dep);
        }
    }

    [TestClass]
    public class ColorTests
    {
        [TestMethod]
        public void ColorParseTest()
        {
            Color c1 = new Color("01ABCD");
            Assert.AreEqual(1, c1.R);
            Assert.AreEqual(171, c1.G);
            Assert.AreEqual(205, c1.B);
        }
    }

    [TestClass]
    public class CustomRoutePointTests
    {
        [TestMethod]
        public void AddTransferToRoutePointTest()
        {
            var coords = new Coordinates(50.0930856, 14.3350747);
            var customRoutePoint = new CustomRoutePoint("id1", "name1", coords);

            var stop = new Stop("id2", "name2", 50.0697328, 14.4603875);

            customRoutePoint.AddTransferToRoutePoint(stop);

            Assert.AreEqual(1, customRoutePoint.possibleTransfers.Count);
            Assert.IsTrue(customRoutePoint.transferDistances.ContainsKey(stop));
        }

        [TestMethod]
        public void AddTransferFromRoutePointTest()
        {
            var coords = new Coordinates(50.0930856, 14.3350747);
            var customRoutePoint = new CustomRoutePoint("id1", "name1", coords);

            var stop = new Stop("id2", "name2", 50.0697328, 14.4603875);

            customRoutePoint.AddTransferFromRoutePoint(stop);

            Assert.AreEqual(1, customRoutePoint.possibleTransfers.Count);
            Assert.IsTrue(customRoutePoint.transferDistances.ContainsKey(stop));
        }

        [TestMethod]
        public void GetTransferWithNormalRPTest()
        {
            var coords = new Coordinates(50.0930856, 14.3350747);
            var customRoutePoint = new CustomRoutePoint("id1", "name1", coords);

            var stop = new Stop("id2", "name2", 50.0697328, 14.4603875);
            var transfer = new CustomTransfer(customRoutePoint, stop, 100);
            customRoutePoint.possibleTransfers.Add(transfer);

            var result = customRoutePoint.GetTransferWithNormalRP(stop);

            Assert.IsNotNull(result);
            Assert.AreEqual(transfer, result);
        }
    }
}

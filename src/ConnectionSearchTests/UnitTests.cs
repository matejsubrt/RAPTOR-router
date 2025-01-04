using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Itinero;
using RAPTOR_Router.Configuration;
using RAPTOR_Router.Extensions;
using RAPTOR_Router.GBFSParsing.Distances;
using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.Models.Dynamic;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Custom;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Interfaces;
using RAPTOR_Router.Structures.Transit;

namespace UnitTests
{
    public class MockDelayModel : IDelayModel
    {
        public bool TryGetDelay(DateOnly date, string tripId, int stopIndex, out int arrivalDelay, out int departureDelay)
        {
            arrivalDelay = 0;
            departureDelay = 0;
            return false;
        }

        public void AddDelay(DateOnly date, string str, int i, int j)
        {
        }

        public TripStopDelays GetTripStopDelaysUnsafe(DateOnly date, string str)
        {
            return new TripStopDelays();
        }

        public bool TripHasDelayData(DateOnly date, string str)
        {
            return false;
        }
    }


    [TestClass]
    public class ComparatorTests
    {
        [TestMethod]
        public void TimeComparatorTest()
        {
            var forwardComparator = new TimeComparator(true);
            var backwardComparator = new TimeComparator(false);

            var time1 = new DateTime(2024, 11, 1, 07, 07, 00);
            var time2 = new DateTime(2024, 11, 1, 07, 07, 01);

            Assert.IsTrue(forwardComparator.ImprovesTime(time1, time2));
            Assert.IsFalse(forwardComparator.ImprovesTime(time2, time1));

            Assert.IsFalse(backwardComparator.ImprovesTime(time1, time2));
            Assert.IsTrue(backwardComparator.ImprovesTime(time2, time1));

            Assert.IsTrue(forwardComparator.ImprovesOrEqualsTime(time1, time2));
            Assert.IsFalse(forwardComparator.ImprovesOrEqualsTime(time2, time1));

            Assert.IsFalse(backwardComparator.ImprovesOrEqualsTime(time1, time2));
            Assert.IsTrue(backwardComparator.ImprovesOrEqualsTime(time2, time1));

            Assert.IsTrue(forwardComparator.ImprovesOrEqualsTime(time1, time1));
            Assert.IsTrue(backwardComparator.ImprovesOrEqualsTime(time1, time1));
        }

        [TestMethod]
        public void IndexComparatorTest()
        {
            var forwardComparator = new IndexComparator(true);
            var backwardComparator = new IndexComparator(false);

            Assert.IsTrue(forwardComparator.PrecedesInSearchDirection(0, 1));
            Assert.IsFalse(forwardComparator.PrecedesInSearchDirection(1, 0));

            Assert.IsFalse(backwardComparator.PrecedesInSearchDirection(0, 1));
            Assert.IsTrue(backwardComparator.PrecedesInSearchDirection(1, 0));
        }
    }

    [TestClass]
    public class TimeExtensionsTests
    {
        [TestMethod]
        public void TimeExtensionsTest()
        {
            var time = new TimeOnly(07, 07, 00);
            var secondsToAdd = new List<int> { 0, 10, -10, 24 * 60 * 60 };
            var expectedResults = new List<TimeOnly>
                { new TimeOnly(07, 07, 00), new TimeOnly(07, 07, 10), new TimeOnly(07, 06, 50), new TimeOnly(07, 07, 00) };

            for (int i = 0; i < secondsToAdd.Count; i++)
            {
                Assert.AreEqual(time.AddSeconds(secondsToAdd[i]), expectedResults[i]);
            }
        }
    }

    [TestClass]
    public class DistanceExtensionTests
    {
        [DataTestMethod]
        [DataRow(50.0930856, 14.3350747, 50.0697328, 14.4603875, 9338)]
        [DataRow(50.0960039, 14.4819311, 50.0943522, 14.4831328, 203)]
        [DataRow(37.8849189, 13.2115511, 37.8784153, 13.2637361, 4647)]
        [DataRow(50.123456, 14.123456, 50.123456, 14.123456, 0)]
        public void DistanceBetweenTest(double lat1, double lon1, double lat2, double lon2, int expected)
        {
            var result = DistanceExtensions.DistanceBetween(lat1, lon1, lat2, lon2);
            var tolerance = expected / 100;
            Assert.IsTrue(expected - tolerance <= result && result <= expected + tolerance);
        }

        [DataTestMethod]
        [DataRow(50.0930856, 14.3350747, 50.0697328, 14.4603875, 9338)]
        [DataRow(50.0960039, 14.4819311, 50.0943522, 14.4831328, 203)]
        [DataRow(50.123456, 14.123456, 50.123456, 14.123456, 0)]
        public void SimplifiedDistanceBetweenTest(double lat1, double lon1, double lat2, double lon2, int expected)
        {
            var result = DistanceExtensions.SimplifiedDistanceBetween(lat1, lon1, lat2, lon2);
            var tolerance = expected / 100;
            Assert.IsTrue(expected - tolerance <= result && result <= expected + tolerance);
        }

        [DataTestMethod]
        [DataRow(50.0930856, 14.3350747, 50.0697328, 14.4603875, 8000, true)]
        [DataRow(50.0930856, 14.3350747, 50.0697328, 14.4603875, 10000, false)]
        [DataRow(50.0960039, 14.4819311, 50.0943522, 14.4831328, 150, true)]
        [DataRow(50.0960039, 14.4819311, 50.0943522, 14.4831328, 300, false)]
        [DataRow(50.123456, 14.123456, 50.123456, 14.123456, 1, false)]
        public void TooFarInOneDirectionTest(double lat1, double lon1, double lat2, double lon2, int maxMeters, bool tooFar)
        {
            var result = DistanceExtensions.TooFarInOneDirection(lat1, lon1, lat2, lon2, maxMeters);
            Assert.AreEqual(tooFar, result);
        }
    }

    [TestClass]
    public class OtherExtensionsTests
    {
        [TestMethod]
        public void GetGtfsStopIdsTest()
        {
            GTFSStopTime time1 = new GTFSStopTime
            {
                ArrivalTime = new TimeOnly(07, 07, 00),
                DepartureTime = new TimeOnly(07, 07, 00),
                StopId = "id1",
                TripId = "trip1"
            };
            GTFSStopTime time2 = new GTFSStopTime
            {
                ArrivalTime = new TimeOnly(07, 07, 00),
                DepartureTime = new TimeOnly(07, 07, 00),
                StopId = "id2",
                TripId = "trip1"
            };
            GTFSStopTime time3 = new GTFSStopTime
            {
                ArrivalTime = new TimeOnly(07, 07, 00),
                DepartureTime = new TimeOnly(07, 07, 00),
                StopId = "id3",
                TripId = "trip2"
            };

            var stopTimes = new List<GTFSStopTime> { time1, time2, time3 };
            var expected = new List<string> { "id1", "id2", "id3" };
            var result = stopTimes.GetStopIds();

            Assert.IsTrue(expected.SequenceEqual(result));
        }

        [TestMethod]
        public void ForbiddingTransfersTest()
        {
            Coordinates coords1 = new Coordinates(50.0930856, 14.3350747);
            Coordinates coords2 = new Coordinates(50.0697328, 14.4603875);
            ForbiddenCrossingPoint p1 = new ForbiddenCrossingPoint(coords1, 0);
            ForbiddenCrossingPoint p2 = new ForbiddenCrossingPoint(coords2, 1);

            Coordinates rpCoords1 = new Coordinates(50.0656242, 14.3858403);
            Coordinates rpCoords2 = new Coordinates(50.1016431, 14.4183700);
            Coordinates rpCoords3 = new Coordinates(50.0629794, 14.4448061);
            Coordinates rpCoords4 = rpCoords1;

            CustomRoutePoint rp1 = new CustomRoutePoint("id1", "name1", rpCoords1);
            CustomRoutePoint rp2 = new CustomRoutePoint("id2", "name2", rpCoords2);
            CustomRoutePoint rp3 = new CustomRoutePoint("id3", "name3", rpCoords3);
            CustomRoutePoint rp4 = new CustomRoutePoint("id4", "name4", rpCoords4);

            ForbiddenCrossingLine line = new ForbiddenCrossingLine(p1, p2, 0, "comment");

            List<ForbiddenCrossingLine> lines = new List<ForbiddenCrossingLine> { line };

            Assert.IsTrue(lines.ForbidsTransferBetween(rp1, rp2));
            Assert.IsFalse(lines.ForbidsTransferBetween(rp1, rp3));
            Assert.IsFalse(lines.ForbidsTransferBetween(rp1, rp4));
        }
    }

    [TestClass]
    public class StationDistanceMatrixTests
    {
        BikeStation s1 = new BikeStation("id1", "name1", 0.0, 0.0, 10, 1);
        BikeStation s2 = new BikeStation("id2", "name2", 0.0, 0.0, 10, 1);
        BikeStation s3 = new BikeStation("id3", "name3", 0.0, 0.0, 10, 1);
        BikeStation s4 = new BikeStation("id4", "name4", 0.0, 0.0, 10, 1);

        [TestMethod]
        public void AddStationTest()
        {
            StationDistanceMatrix matrix = new();

            Assert.IsFalse(matrix.HasDistance(s1, s2));

            matrix.AddDistance(s1, s2, 10);
            matrix.AddDistance(s1, s3, 20);

            Assert.IsTrue(matrix.HasDistance(s1, s2));
            Assert.IsTrue(matrix.HasDistance(s1, s3));
            Assert.IsTrue(matrix.HasDistance(s2, s1));
            Assert.IsTrue(matrix.HasDistance(s3, s1));

            Assert.AreEqual(10, matrix.GetDistance(s1, s2));
            Assert.AreEqual(20, matrix.GetDistance(s1, s3));
            Assert.AreEqual(10, matrix.GetDistance(s2, s1));
            Assert.AreEqual(20, matrix.GetDistance(s3, s1));

            var distancesFromS1 = matrix.GetDistancesFromStation(s1);

            Assert.IsTrue(distancesFromS1.Count == 2 && 
                          distancesFromS1.ContainsKey(s2) && distancesFromS1[s2] == 10 &&
                          distancesFromS1.ContainsKey(s3) && distancesFromS1[s3] == 20);
        }

        [TestMethod]
        public void MergeMatricesTest()
        {
            StationDistanceMatrix matrix1 = new();
            StationDistanceMatrix matrix2 = new();

            matrix1.AddDistance(s1, s2, 10);
            matrix1.AddDistance(s1, s3, 20);

            matrix2.AddDistance(s1, s4, 30);
            matrix2.AddDistance(s2, s3, 40);

            matrix1.MergeNewDistances(matrix2);

            Assert.IsTrue(matrix1.HasDistance(s1, s2));
            Assert.IsTrue(matrix1.HasDistance(s1, s3));
            Assert.IsTrue(matrix1.HasDistance(s1, s4));
            Assert.IsTrue(matrix1.HasDistance(s2, s1));
            Assert.IsTrue(matrix1.HasDistance(s3, s1));
            Assert.IsTrue(matrix1.HasDistance(s4, s1));
            Assert.IsTrue(matrix1.HasDistance(s2, s3));
            Assert.IsTrue(matrix1.HasDistance(s3, s2));

            Assert.AreEqual(10, matrix1.GetDistance(s1, s2));
            Assert.AreEqual(20, matrix1.GetDistance(s1, s3));
            Assert.AreEqual(30, matrix1.GetDistance(s1, s4));
            Assert.AreEqual(10, matrix1.GetDistance(s2, s1));
            Assert.AreEqual(20, matrix1.GetDistance(s3, s1));
            Assert.AreEqual(30, matrix1.GetDistance(s4, s1));
            Assert.AreEqual(40, matrix1.GetDistance(s2, s3));
            Assert.AreEqual(40, matrix1.GetDistance(s3, s2));
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

    [TestClass]
    public class CoordinatesTests
    {
        [TestMethod]
        public void EqualsTest()
        {
            var coords1 = new Coordinates(50.0930856, 14.3350747);
            var coords2 = new Coordinates(50.0930856, 14.3350747);
            var coords3 = new Coordinates(50.0697328, 14.4603875);

            Assert.IsTrue(coords1.Equals(coords2));
            Assert.IsFalse(coords1.Equals(coords3));
        }

        [TestMethod]
        public void GetHashCodeTest()
        {
            var coords1 = new Coordinates(50.0930856, 14.3350747);
            var coords2 = new Coordinates(50.0930856, 14.3350747);

            Assert.AreEqual(coords1.GetHashCode(), coords2.GetHashCode());
        }

        [TestMethod]
        public void ValidateValueTest()
        {
            var validCoords1 = new Coordinates(50.0930856, 14.3350747);
            var validCoords2 = new Coordinates(-50.0930856, -14.3350747);
            var validCoords3 = new Coordinates(0.0, 0.0);
            var validCoords4 = new Coordinates(90.0, 180.0);
            var validCoords5 = new Coordinates(-90.0, -180.0);

            var invalidCoords1 = new Coordinates(100.0, 200.0); // Invalid latitude and longitude
            var invalidCoords2 = new Coordinates(-100.0, -200.0); // Invalid latitude and longitude
            var invalidCoords3 = new Coordinates(91.0, 0.0); // Invalid latitude
            var invalidCoords4 = new Coordinates(0.0, 181.0); // Invalid longitude
            var invalidCoords5 = new Coordinates(-91.0, 0.0); // Invalid latitude
            var invalidCoords6 = new Coordinates(0.0, -181.0); // Invalid longitude

            Assert.IsTrue(validCoords1.ValidateValue());
            Assert.IsTrue(validCoords2.ValidateValue());
            Assert.IsTrue(validCoords3.ValidateValue());
            Assert.IsTrue(validCoords4.ValidateValue());
            Assert.IsTrue(validCoords5.ValidateValue());

            Assert.IsFalse(invalidCoords1.ValidateValue());
            Assert.IsFalse(invalidCoords2.ValidateValue());
            Assert.IsFalse(invalidCoords3.ValidateValue());
            Assert.IsFalse(invalidCoords4.ValidateValue());
            Assert.IsFalse(invalidCoords5.ValidateValue());
            Assert.IsFalse(invalidCoords6.ValidateValue());
        }

        [TestMethod]
        public void OperatorEqualityTest()
        {
            var coords1 = new Coordinates(50.0930856, 14.3350747);
            var coords2 = new Coordinates(50.0930856, 14.3350747);
            var coords3 = new Coordinates(50.0697328, 14.4603875);

            Assert.IsTrue(coords1 == coords2);
            Assert.IsFalse(coords1 == coords3);
        }

        [TestMethod]
        public void OperatorInequalityTest()
        {
            var coords1 = new Coordinates(50.0930856, 14.3350747);
            var coords2 = new Coordinates(50.0930856, 14.3350747);
            var coords3 = new Coordinates(50.0697328, 14.4603875);

            Assert.IsFalse(coords1 != coords2);
            Assert.IsTrue(coords1 != coords3);
        }
    }

    [TestClass]
    public class ForbiddenCrossingTests
    {
        [TestMethod]
        public void IsCrossingForbiddenTest()
        {
            var pointA = new ForbiddenCrossingPoint(new Coordinates(50.096, 14.377), 1);
            var pointB = new ForbiddenCrossingPoint(new Coordinates(50.073, 14.453), 2);
            var forbiddenLine = new ForbiddenCrossingLine(pointA, pointB, 1, "Test Line");

            var testPoint1 = new CustomRoutePoint("id3", "testPoint1", new Coordinates(50.071, 14.384));
            var testPoint2 = new CustomRoutePoint("id4", "testPoint2", new Coordinates(50.100, 14.455));

            Assert.IsTrue(forbiddenLine.IsCrossingForbidden(testPoint1, testPoint2));
        }

        [TestMethod]
        public void IsCrossingForbiddenOutsideLineTest()
        {
            var pointA = new ForbiddenCrossingPoint(new Coordinates(50.096, 14.377), 1);
            var pointB = new ForbiddenCrossingPoint(new Coordinates(50.100, 14.455), 2);
            var forbiddenLine = new ForbiddenCrossingLine(pointA, pointB, 1, "Test Line");

            var testPointOutside1 = new CustomRoutePoint("id3", "testPointOutside1", new Coordinates(50.071, 14.384));
            var testPointOutside2 = new CustomRoutePoint("id4", "testPointOutside2", new Coordinates(50.073, 14.453));

            Assert.IsFalse(forbiddenLine.IsCrossingForbidden(testPointOutside1, testPointOutside2));
        }
    }

    [TestClass]
    public class BikeDistanceDatabaseTests
    {
        private static string? dbPath;
        private static BikeDistanceDatabase? db;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            dbPath = Config.NextbikeDbTestPath.Remove(Config.NextbikeDbTestPath.Length - 3) + "_new.db";
            if (dbPath == null)
            {
                Assert.Fail("Test database path is not configured.");
            }

            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            db = new BikeDistanceDatabase(dbPath);
        }

        [TestMethod]
        public void AddOrUpdateDistanceTest()
        {
            db?.AddOrUpdateDistance("StationA", "StationB", 1000);
            var distance = db?.GetDistance("StationA", "StationB");

            Assert.AreEqual(1000, distance);
        }

        [TestMethod]
        public void GetDistanceTest()
        {
            db?.AddOrUpdateDistance("StationA", "StationB", 1000);
            var distance = db?.GetDistance("StationA", "StationB");

            Assert.AreEqual(1000, distance);

            var reverseDistance = db?.GetDistance("StationB", "StationA");
            Assert.AreEqual(1000, reverseDistance);
        }

        [TestMethod]
        public void GetDistanceNonExistentTest()
        {
            var distance = db?.GetDistance("NonExistentStationA", "NonExistentStationB");
            Assert.AreEqual(-1, distance);
        }

        [TestMethod]
        public void GetDistanceMatrixAndRemoveNonExistentStationsTest()
        {
            var stationA = new BikeStation("StationA", "Station A", 50.0, 14.0, 10, 1);
            var stationB = new BikeStation("StationB", "Station B", 50.1, 14.1, 15, 2);
            var stationC = new BikeStation("StationC", "Station C", 50.2, 14.2, 20, 3);

            var stationsById = new Dictionary<string, BikeStation>
            {
                { "StationA", stationA },
                { "StationB", stationB }
            };

            db?.AddOrUpdateDistance("StationA", "StationB", 1000);
            db?.AddOrUpdateDistance("StationA", "StationC", 2000);

            var matrix = db?.GetDistanceMatrixAndRemoveNonExistentStations(stationsById);

            Assert.AreEqual(1000, matrix?.GetDistance(stationA, stationB));
            Assert.AreEqual(-1, matrix?.GetDistance(stationA, stationC));
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


            var dateTime1 = DateTime.Now.AddHours(-DateTime.Now.Hour + 7);
            var times = route.GetFirstNTripTimesAtStop(stop, dateTime1, 2, true);

            var dateTime2 = dateTime1.AddHours(1).AddSeconds(1);
            var times2 = route.GetFirstNTripTimesAtStop(stop, dateTime2, 2, true);

            Assert.AreEqual(2, times.Count);
            Assert.AreEqual(1, times2.Count);
        }

        [TestMethod]
        public void TestGetTripTimesAtStopWithinRange()
        {
            var route = CreateTestRoute();
            var stop = route.RouteStops[0];
            var rangeStart = DateTime.Now.AddHours(-DateTime.Now.Hour + 7);
            var rangeEnd = rangeStart.AddHours(3);
            var times = route.GetTripTimesAtStopWithinRange(stop, rangeStart, rangeEnd, true);
            Assert.IsTrue(times.Count > 0);
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
        }
    }

    [TestClass]
    public class DelayModelTests
    {
        private DelayModel delayModel;

        [TestInitialize]
        public void TestInitialize()
        {
            delayModel = new DelayModel();
        }

        [TestMethod]
        public void TestAddDelay()
        {
            var tripStartDate = DateOnly.FromDateTime(DateTime.Now);
            var tripId = "Trip1";
            int arrivalDelay = 5;
            int departureDelay = 10;

            delayModel.AddDelay(tripStartDate, tripId, arrivalDelay, departureDelay);

            Assert.IsTrue(delayModel.TripHasDelayData(tripStartDate, tripId));
        }

        [TestMethod]
        public void TestTryGetDelay()
        {
            var tripStartDate = DateOnly.FromDateTime(DateTime.Now);
            var tripId = "Trip1";
            int arrivalDelay = 5;
            int departureDelay = 10;

            delayModel.AddDelay(tripStartDate, tripId, arrivalDelay, departureDelay);

            bool result = delayModel.TryGetDelay(tripStartDate, tripId, 0, out int retrievedArrivalDelay, out int retrievedDepartureDelay);

            Assert.IsTrue(result);
            Assert.AreEqual(arrivalDelay, retrievedArrivalDelay);
            Assert.AreEqual(departureDelay, retrievedDepartureDelay);
        }

        [TestMethod]
        public void TestTripHasDelayData()
        {
            var tripStartDate = DateOnly.FromDateTime(DateTime.Now);
            var tripId = "Trip1";

            Assert.IsFalse(delayModel.TripHasDelayData(tripStartDate, tripId));

            delayModel.AddDelay(tripStartDate, tripId, 5, 10);

            Assert.IsTrue(delayModel.TripHasDelayData(tripStartDate, tripId));
        }

        [TestMethod]
        public void TestGetTripStopDelaysUnsafe()
        {
            var tripStartDate = DateOnly.FromDateTime(DateTime.Now);
            var tripId = "Trip1";
            int arrivalDelay = 5;
            int departureDelay = 10;

            delayModel.AddDelay(tripStartDate, tripId, arrivalDelay, departureDelay);

            var tripStopDelays = delayModel.GetTripStopDelaysUnsafe(tripStartDate, tripId);

            Assert.IsNotNull(tripStopDelays);
            Assert.AreEqual(1, tripStopDelays.Count);
        }

        [TestMethod]
        public void TestTryGetDelay_NoData()
        {
            var tripStartDate = DateOnly.FromDateTime(DateTime.Now);
            var tripId = "Trip1";

            bool result = delayModel.TryGetDelay(tripStartDate, tripId, 0, out int arrivalDelay, out int departureDelay);

            Assert.IsFalse(result);
            Assert.AreEqual(0, arrivalDelay);
            Assert.AreEqual(0, departureDelay);
        }

        [TestMethod]
        public void TestGetTripStopDelaysUnsafe_NoData()
        {
            var tripStartDate = DateOnly.FromDateTime(DateTime.Now);
            var tripId = "Trip1";

            Assert.ThrowsException<KeyNotFoundException>(() => delayModel.GetTripStopDelaysUnsafe(tripStartDate, tripId));
        }
    }

    [TestClass]
    public class TransitModelTests
    {
        private TransitModel transitModel;
        private GTFS gtfs;

        [TestInitialize]
        public void TestInitialize()
        {
            gtfs = CreateTestGTFS();
            transitModel = new TransitModel(gtfs, new List<ForbiddenCrossingLine>());
        }

        private GTFS CreateTestGTFS()
        {
            var gtfs = new GTFS
            {
                Agencies = new Dictionary<string, GTFSAgency>
                {
                    { "Agency1", new GTFSAgency { Id = "Agency1", Name = "Test Agency" } }
                },
                Calendars = new Dictionary<string, GTFSCalendar>
                {
                    { "Service1", new GTFSCalendar
                    {
                        ServiceId = "Service1", 
                        Monday = true, 
                        Tuesday = true,
                        Wednesday = true,
                        Thursday = true,
                        Friday = true,
                        Saturday = true,
                        Sunday = true,
                        StartDate = DateOnly.FromDateTime(DateTime.Now), 
                        EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30))
                    } }
                },
                CalendarDates = new Dictionary<string, List<GTFSCalendarDate>>
                {
                    { "Service1", new List<GTFSCalendarDate> { new GTFSCalendarDate { ServiceId = "Service1", Date = DateOnly.FromDateTime(DateTime.Now), ExceptionType = 1 } } }
                },
                Routes = new Dictionary<string, GTFSRoute>
                {
                    { "Route1", new GTFSRoute { Id = "Route1", ShortName = "R1", LongName = "Route 1", Type = 1, Color = "FFFFFF" } }
                },
                Stops = new Dictionary<string, GTFSStop>
                {
                    { "Stop1", new GTFSStop { Id = "Stop1", Name = "Stop 1", Lat = 50.096, Lon = 14.377, ZoneId = "Zone1", LocationType = 0 } },
                    { "Stop2", new GTFSStop { Id = "Stop2", Name = "Stop 2", Lat = 50.100, Lon = 14.455, ZoneId = "Zone1", LocationType = 0 } }
                },
                StopTimes = new Dictionary<string, List<GTFSStopTime>>
                {
                    { "Trip1", new List<GTFSStopTime> { new GTFSStopTime { TripId = "Trip1", ArrivalTime = new TimeOnly(8, 0), DepartureTime = new TimeOnly(8, 5), StopId = "Stop1" } } }
                },
                Trips = new Dictionary<string, GTFSTrip>
                {
                    { "Trip1", new GTFSTrip { RouteId = "Route1", ServiceId = "Service1", Id = "Trip1" } }
                }
            };

            return gtfs;
        }

        [TestMethod]
        public void TestGetStopsByName()
        {
            var stops = transitModel.GetStopsByName("Stop 1");
            Assert.AreEqual(1, stops.Count);
            Assert.AreEqual("Stop1", stops[0].Id);
        }

        [TestMethod]
        public void TestGetStopsByLocation()
        {
            var coords = new Coordinates(50.103, 14.455);
            var stops = transitModel.GetStopsByLocation(coords, 500);
            Assert.AreEqual(1, stops.Count);
            Assert.AreEqual("Stop2", stops[0].Id);
        }

        [TestMethod]
        public void TestGetStopsWithDistancesByLocation()
        {
            var coords = new Coordinates(50.103, 14.455);
            var stopsWithDistances = transitModel.GetStopsWithDistancesByLocation(coords, 500);
            Assert.AreEqual(1, stopsWithDistances.Count);
            Assert.IsTrue(stopsWithDistances.ContainsKey(transitModel.stops["Stop2"]));
        }

        [TestMethod]
        public void TestNearStopExists()
        {
            var coords = new Coordinates(50.103, 14.455);
            var exists = transitModel.NearStopExists(coords, 500);
            Assert.IsTrue(exists);
        }
    }


    [TestClass]
    public class SearchModelTests
    {
        private Stop stop1 = new Stop("Stop1", "Test Stop 1", 0, 0);
        private Stop stop2 = new Stop("Stop2", "Test Stop 2", 1, 1);
        private Stop stop3 = new Stop("Stop3", "Test Stop 3", 2, 2);
        private Stop stop4 = new Stop("Stop4", "Test Stop 4", 3, 3);

        private BikeStation bikeStation1 = new BikeStation("BikeStation1", "Test Bike Station 1", 0, 0, 10, 1);
        private BikeStation bikeStation2 = new BikeStation("BikeStation2", "Test Bike Station 2", 1, 1, 10, 1);
        private BikeStation bikeStation3 = new BikeStation("BikeStation3", "Test Bike Station 3", 2, 2, 5, 2);
        private BikeStation bikeStation4 = new BikeStation("BikeStation4", "Test Bike Station 4", 3, 3, 5, 2);


        private SearchModel searchModel;
        private BikeModel bikeModel;
        private IDelayModel delayModel;
        private Settings settings;
        private DateTime searchBeginTime;
        private List<Stop> searchBeginStops;
        private List<Stop> searchEndStops;
        private List<BikeStation> searchBeginBikeStations;
        private List<BikeStation> searchEndBikeStations;
        private bool forward;

        [TestInitialize]
        public void TestInitialize()
        {
            searchBeginTime = DateTime.Now;
            forward = true;
            settings = new Settings();
            delayModel = new MockDelayModel();
            searchBeginStops = new List<Stop>
        {
            stop1,
            stop3
        };
            searchEndStops = new List<Stop>
        {
            stop2,
            stop4
        };
            searchBeginBikeStations = new List<BikeStation>
        {
            bikeStation1,
            bikeStation3
        };
            searchEndBikeStations = new List<BikeStation>
        {
            bikeStation2,
            bikeStation4
        };

            searchModel = new SearchModel(forward, searchBeginStops, searchEndStops, searchBeginBikeStations, searchEndBikeStations, searchBeginTime, settings, delayModel);
            bikeModel = new BikeModel();
        }


        [TestMethod]
        public void TestGetSearchBeginTime()
        {
            var beginTime = searchModel.GetSearchBeginTime();
            Assert.AreEqual(searchBeginTime, beginTime);
        }

        [TestMethod]
        public void TestGetCurrentBestSearchEndTime()
        {
            var endTime = searchModel.GetCurrentBestSearchEndTime();
            Assert.AreEqual(searchModel.GetCurrentBestSearchEndTime(), endTime);
        }

        [TestMethod]
        public void TestGetBestReachTime()
        {
            var reachTime1 = searchModel.GetBestReachTime(stop1);
            var reachTime2 = searchModel.GetBestReachTime(stop2);
            Assert.AreEqual(DateTime.MaxValue, reachTime1);
            Assert.AreEqual(DateTime.MaxValue, reachTime2);
        }

        [TestMethod]
        public void TestSetCurrentBestSearchEndTime()
        {
            var newEndTime = DateTime.Now.AddHours(1);
            searchModel.SetCurrentBestSearchEndTime(newEndTime);
            Assert.AreEqual(newEndTime, searchModel.GetCurrentBestSearchEndTime());

            var anotherEndTime = DateTime.Now.AddHours(2);
            searchModel.SetCurrentBestSearchEndTime(anotherEndTime);
            Assert.AreEqual(anotherEndTime, searchModel.GetCurrentBestSearchEndTime());
        }

        [TestMethod]
        public void TestSetBestReachTime()
        {
            var newReachTime1 = DateTime.Now.AddMinutes(30);
            var newReachTime2 = DateTime.Now.AddMinutes(45);

            searchModel.SetBestReachTime(stop1, newReachTime1);
            searchModel.SetBestReachTime(stop2, newReachTime2);

            Assert.AreEqual(newReachTime1, searchModel.GetBestReachTime(stop1));
            Assert.AreEqual(newReachTime2, searchModel.GetBestReachTime(stop2));
        }

        [TestMethod]
        public void TestSetSearchBeginStopsReachTime()
        {
            searchModel.SetSearchBeginStopsReachTime();
            foreach (var stop in searchBeginStops)
            {
                Assert.AreEqual(searchBeginTime, searchModel.GetBestReachTime(stop));
            }
        }

        [TestMethod]
        public void TestSetSearchBeginBikeStationsReachTime()
        {
            searchModel.SetSearchBeginBikeStationsReachTime();
            foreach (var bikeStation in searchBeginBikeStations)
            {
                Assert.AreEqual(searchBeginTime, searchModel.GetBestReachTime(bikeStation));
            }
        }

        [TestMethod]
        public void TestTryImproveReachTimeByTrip_ImprovesReachTime()
        {
            var dateTime = DateTime.Now;
            var reachTime = dateTime.AddMinutes(10);
            var gtfsTripStopTimes = new List<GTFSStopTime>
            {
                new GTFSStopTime { TripId = "Trip1", ArrivalTime = new TimeOnly(8, 0, 0), DepartureTime = new TimeOnly(8, 5, 0), StopId = "Stop1" },
                new GTFSStopTime { TripId = "Trip1", ArrivalTime = new TimeOnly(8, 15, 0), DepartureTime = new TimeOnly(8, 20, 0), StopId = "Stop2" }
            };

            var route = new Route("Route1", "GtfsRoute1", "1", "Route 1");

            var trip = new Trip(gtfsTripStopTimes, route, "Trip1");
            var tripDate = DateOnly.FromDateTime(dateTime);
            var reachedFromStop = new Stop("Stop2", "Test Stop 2", 1, 1);
            var round = 1;

            var result = searchModel.TryImproveReachTimeByTrip(stop1, reachTime, trip, tripDate, reachedFromStop, round);

            Assert.IsTrue(result);
            Assert.IsTrue(searchModel.RoutePointIsReachedByTripInRound(stop1, round));
            Assert.AreEqual(reachTime, searchModel.GetBestReachTime(stop1));
            Assert.AreEqual(reachTime, searchModel.GetBestReachTimeInRound(stop1, round));
        }

        [TestMethod]
        public void TestTryImproveReachTimeByTrip_DoesNotImproveReachTime()
        {
            var dateTime = DateTime.Now;
            var reachTime = dateTime.AddMinutes(10);
            var gtfsTripStopTimes = new List<GTFSStopTime>
            {
                new GTFSStopTime { TripId = "Trip1", ArrivalTime = new TimeOnly(8, 0, 0), DepartureTime = new TimeOnly(8, 5, 0), StopId = "Stop1" },
                new GTFSStopTime { TripId = "Trip1", ArrivalTime = new TimeOnly(8, 15, 0), DepartureTime = new TimeOnly(8, 20, 0), StopId = "Stop2" }
            };

            var route = new Route("Route1", "GtfsRoute1", "1", "Route 1");

            var trip = new Trip(gtfsTripStopTimes, route, "Trip1");
            var tripDate = DateOnly.FromDateTime(dateTime);
            var reachedFromStop = new Stop("Stop2", "Test Stop 2", 1, 1);
            var round = 1;

            searchModel.SetBestReachTime(stop1, dateTime.AddMinutes(5));

            var result = searchModel.TryImproveReachTimeByTrip(stop1, reachTime, trip, tripDate, reachedFromStop, round);

            Assert.IsFalse(result);
            Assert.AreEqual(dateTime.AddMinutes(5), searchModel.GetBestReachTime(stop1));
            Assert.AreEqual(DateTime.MaxValue, searchModel.GetBestReachTimeInRound(stop1, round));
        }

        [TestMethod]
        public void TestTryImproveReachTimeByTrip_MultipleRounds()
        {
            var dateTime = DateTime.Now;
            var reachTimeRound1 = dateTime.AddMinutes(10);
            var reachTimeRound2 = dateTime.AddMinutes(5);
            var gtfsTripStopTimes = new List<GTFSStopTime>
            {
                new GTFSStopTime { TripId = "Trip1", ArrivalTime = new TimeOnly(8, 0, 0), DepartureTime = new TimeOnly(8, 5, 0), StopId = "Stop1" },
                new GTFSStopTime { TripId = "Trip1", ArrivalTime = new TimeOnly(8, 15, 0), DepartureTime = new TimeOnly(8, 20, 0), StopId = "Stop2" }
            };

            var route = new Route("Route1", "GtfsRoute1", "1", "Route 1");

            var trip = new Trip(gtfsTripStopTimes, route, "Trip1");
            var tripDate = DateOnly.FromDateTime(dateTime);
            var reachedFromStop = new Stop("Stop2", "Test Stop 2", 1, 1);

            var resultRound1 = searchModel.TryImproveReachTimeByTrip(stop1, reachTimeRound1, trip, tripDate, reachedFromStop, 1);
            Assert.IsTrue(resultRound1);
            Assert.AreEqual(reachTimeRound1, searchModel.GetBestReachTime(stop1));
            Assert.AreEqual(reachTimeRound1, searchModel.GetBestReachTimeInRound(stop1, 1));

            var resultRound2 = searchModel.TryImproveReachTimeByTrip(stop1, reachTimeRound2, trip, tripDate, reachedFromStop, 2);
            Assert.IsTrue(resultRound2);
            Assert.AreEqual(reachTimeRound2, searchModel.GetBestReachTime(stop1));
            Assert.AreEqual(reachTimeRound2, searchModel.GetBestReachTimeInRound(stop1, 2));
        }

        [TestMethod]
        public void TestTryImproveReachTimeByBikeTrip_ImprovesReachTime()
        {
            var dateTime = DateTime.Now;
            var reachTime = dateTime.AddMinutes(10);
            var round = 1;

            var result = searchModel.TryImproveReachTimeByBikeTrip(bikeStation1, bikeStation2, reachTime, round);

            Assert.IsTrue(result);
            Assert.IsTrue(searchModel.RoutePointIsReachedByBikeInRound(bikeStation2, round));
            Assert.AreEqual(reachTime, searchModel.GetBestReachTime(bikeStation2));
            Assert.AreEqual(reachTime, searchModel.GetBestReachTimeInRound(bikeStation2, round));
        }

        [TestMethod]
        public void TestTryImproveReachTimeByBikeTrip_DoesNotImproveReachTime()
        {
            var dateTime = DateTime.Now;
            var reachTime = dateTime.AddMinutes(10);
            var round = 1;

            searchModel.SetBestReachTime(bikeStation2, dateTime.AddMinutes(5));

            var result = searchModel.TryImproveReachTimeByBikeTrip(bikeStation1, bikeStation2, reachTime, round);

            Assert.IsFalse(result);
            Assert.AreEqual(dateTime.AddMinutes(5), searchModel.GetBestReachTime(bikeStation2));
            Assert.AreEqual(DateTime.MaxValue, searchModel.GetBestReachTimeInRound(bikeStation2, round));
        }

        [TestMethod]
        public void TestTryImproveReachTimeByTransfer_ImprovesReachTime()
        {
            var dateTime = DateTime.Now;
            var transfer = new Transfer(stop1, stop2, 100);
            var round = 0;

            var time = settings.GetAdjustedWalkingTransferTime(transfer.Distance);
            var reachTime = dateTime.AddSeconds(time);

            searchModel.SetSearchBeginStopsReachTime();

            var result = searchModel.TryImproveReachTimeByTransfer(transfer, false, round);

            Assert.IsTrue(result);
            Assert.IsTrue(searchModel.RoutePointIsReachedByTransferInRound(stop2, round));
            Assert.IsTrue((reachTime - searchModel.GetBestReachTime(stop2)).Ticks < 1000000);
            Assert.IsTrue((reachTime - searchModel.GetBestReachTimeInRound(stop2, round)).Ticks < 1000000);
        }

        [TestMethod]
        public void TestTryImproveReachTimeByTransfer_DoesNotImproveReachTime()
        {
            var dateTime = DateTime.Now;
            var reachTime = dateTime.AddMinutes(10);
            var transfer = new Transfer(stop1, stop2, 100);
            var round = 0;

            searchModel.SetSearchBeginStopsReachTime();

            searchModel.SetBestReachTime(stop2, dateTime.AddMinutes(1));

            var result = searchModel.TryImproveReachTimeByTransfer(transfer, false, round);

            Assert.IsFalse(result);
            Assert.AreEqual(dateTime.AddMinutes(1), searchModel.GetBestReachTime(stop2));
            Assert.AreEqual(DateTime.MaxValue, searchModel.GetBestReachTimeInRound(stop2, round));
        }



    }
}

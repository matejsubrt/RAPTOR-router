// AI has been used to create/modify some of the tests in this file
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class BikeDistanceDatabaseTests
    {
        private static string? dbPath;
        private static BikeDistanceDatabase? db;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            dbPath = Config.NextbikeDbTestPath!.Remove(Config.NextbikeDbTestPath.Length - 3) + "_new.db";
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
}

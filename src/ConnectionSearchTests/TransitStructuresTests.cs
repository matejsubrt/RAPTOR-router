using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.GTFSParsing;
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
            Assert.IsTrue(Trip.CompareTrips(t1, t2) < 0);
            Assert.IsTrue(Trip.CompareTrips(t1, t1) == 0);
            Assert.IsTrue(Trip.CompareTrips(t2, t1) > 0);
        }

        [TestMethod]
        public void GetArrDepDateTimeTest()
        {
            var t1arr = t1.GetArrivalDateTime(0, new DateOnly(2020, 1, 1));
            var t1dep = t1.GetDepartureDateTime(1, new DateOnly(2020, 2, 1));

            Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 1), t1arr);
            Assert.AreEqual(new DateTime(2020, 2, 1, 0, 0, 4), t1dep);
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
}

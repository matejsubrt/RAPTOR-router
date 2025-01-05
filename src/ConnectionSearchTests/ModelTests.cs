// AI has been used to create/modify some of the tests in this file
#pragma warning  disable CS8618
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RAPTOR_Router.GBFSParsing.DataSources;
using RAPTOR_Router.GBFSParsing.Distances;
using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.Models.Dynamic;
using RAPTOR_Router.Models.Static;
using RAPTOR_Router.Structures.Bike;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Generic;
using RAPTOR_Router.Structures.Transit;

namespace UnitTests
{
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

            var tripStopDelays = delayModel.GetTripStopDelaysUnsafe(tripStartDate, tripId);
            Assert.AreEqual(1, tripStopDelays.Count);
            Assert.IsTrue(tripStopDelays.TryGetStopDelay(0, out int retrievedArrivalDelay, out int retrievedDepartureDelay));
            Assert.AreEqual(arrivalDelay, retrievedArrivalDelay);
            Assert.AreEqual(departureDelay, retrievedDepartureDelay);
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
            Assert.IsTrue(tripStopDelays.TryGetStopDelay(0, out int retrievedArrivalDelay, out int retrievedDepartureDelay));
            Assert.AreEqual(arrivalDelay, retrievedArrivalDelay);
            Assert.AreEqual(departureDelay, retrievedDepartureDelay);
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
            Assert.AreEqual("Stop 1", stops[0].Name);
            Assert.AreEqual(50.096, stops[0].Coords.Lat);
            Assert.AreEqual(14.377, stops[0].Coords.Lon);
        }

        [TestMethod]
        public void TestGetStopsByLocation()
        {
            var coords = new Coordinates(50.103, 14.455);
            var stops = transitModel.GetStopsByLocation(coords, 500);
            Assert.AreEqual(1, stops.Count);
            Assert.AreEqual("Stop2", stops[0].Id);
            Assert.AreEqual("Stop 2", stops[0].Name);
            Assert.AreEqual(50.100, stops[0].Coords.Lat);
            Assert.AreEqual(14.455, stops[0].Coords.Lon);
        }

        [TestMethod]
        public void TestGetStopsWithDistancesByLocation()
        {
            var coords = new Coordinates(50.103, 14.455);
            var stopsWithDistances = transitModel.GetStopsWithDistancesByLocation(coords, 500);
            Assert.AreEqual(1, stopsWithDistances.Count);
            var stop = transitModel.stops["Stop2"];
            Assert.IsTrue(stopsWithDistances.ContainsKey(stop));
            Assert.AreEqual(50.100, stop.Coords.Lat);
            Assert.AreEqual(14.455, stop.Coords.Lon);
            Assert.IsTrue(stopsWithDistances[stop] > 0);
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
#pragma warning restore CS8618
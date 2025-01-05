#pragma warning disable CS8614
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RAPTOR_Router.Configuration;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.RouteFinders;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Requests;
using static RAPTOR_Router.Models.Results.SearchResult;

namespace ConnectionSearchTests
{
    public class StopsDataSourceAttribute : Attribute, ITestDataSource
    {
        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            string? dataLocation = Config.ConnectionTestDataFilePath;

            if (dataLocation is null)
            {
                throw new InvalidOperationException("Test data file path not set in config.json");
            }


            IEnumerable<string> lines = File.ReadLines(dataLocation);

            foreach (string line in lines)
            {
                if (line == "" || line[0] == ' ' || (line[0] == '/' && line[1] == '/')) continue; // Skip empty lines and comments
                string[] parts = line.Split(";");
                yield return new object[] { parts[0], parts[1] };
            }
        }

        public string? GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            if (data != null)
                return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));

            return null;
        }
    }

    public class AltTripsDataSourceAttribute : Attribute, ITestDataSource
    {
        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            string? dataLocation = Config.AltTripTestDataFilePath;

            if (dataLocation is null)
            {
                throw new InvalidOperationException("Test data file path not set in config.json");
            }

            IEnumerable<string> lines = File.ReadLines(dataLocation);

            foreach (string line in lines)
            {
                if (line == "" || line[0] == ' ' || (line[0] == '/' && line[1] == '/')) continue; // Skip empty lines and comments
                string[] parts = line.Split(";");
                yield return new object[] { parts[0], parts[1], parts[2] };
            }
        }

        public string? GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            if (data != null)
                return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));

            return null;
        }
    }


    [TestClass]
    public static class TestInitClass
    {

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var projectDirectory = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", ".."));
            Environment.SetEnvironmentVariable("TestWorkingDirectory", projectDirectory);

            Directory.SetCurrentDirectory(projectDirectory);

            string? gtfsZipArchiveLocation = Config.TestGTFSArchivePath;

            if (gtfsZipArchiveLocation is null)
            {
                throw new InternalTestFailureException("Invalid gtfs zip archive path. Check the testConfig.json file");
            }

            RouteFinderBuilder.LoadAllData(gtfsZipArchiveLocation, true);

            RouteFinderBuilder.SetDelayModel(new MockDelayModel());
        }
    }

    public static class ResultIntegrityChecker
    {
        public static void AssertEnoughTimeBetweenTrips(SearchResult result, int bikeLockUnlockTime = 0)
        {
            var segmentTypes = result.UsedSegmentTypes;
            int secondsSinceLastTrip = 0;
            DateTime lastTripDisembarkTime = new();

            int tripIndex = 0;
            int transferIndex = 0;
            int bikeTripIndex = 0;
            for (int i = 0; i < segmentTypes.Count; i++)
            {
                var segType = segmentTypes[i];
                switch (segType)
                {
                    case SegmentType.Transfer:
                        secondsSinceLastTrip += result.UsedTransfers[transferIndex++].time;
                        break;
                    case SegmentType.Bike:
                        secondsSinceLastTrip += result.UsedBikeTrips[bikeTripIndex++].time;
                        secondsSinceLastTrip += bikeLockUnlockTime;
                        break;
                    case SegmentType.Trip:
                        var trip = result.UsedTrips[tripIndex];
                        if (tripIndex == 0)
                        {
                            lastTripDisembarkTime = trip.stopPasses[trip.getOffStopIndex].ArrivalTime.AddSeconds(trip.currentDelay);
                        }
                        else
                        {
                            var tripBoardingTime = trip.stopPasses[trip.getOnStopIndex].DepartureTime.AddSeconds(trip.delayWhenBoarded);
                            var tripDisembarkTime = trip.stopPasses[trip.getOffStopIndex].ArrivalTime.AddSeconds(trip.currentDelay);

                            var arrivalAtBoardingStop = lastTripDisembarkTime.AddSeconds(secondsSinceLastTrip);

                            Assert.IsTrue(arrivalAtBoardingStop <= tripBoardingTime);
                            Assert.IsTrue(tripBoardingTime <= tripDisembarkTime);

                            
                            lastTripDisembarkTime = tripDisembarkTime;
                        }

                        secondsSinceLastTrip = 0;
                        tripIndex++;
                        break;
                    default:
                        Assert.Fail("Invalid segment type");
                        break;
                }
            }
        }

        private static void AssertResultArrDepTimeValid(SearchResult result, bool byEarliestDeparture, DateTime dateTime, bool range = false)
        {
            int secondsBeforeFirstTrip = result.GetTotalSecondsBeforeFirstTrip();
            int secondsAfterLastTrip = result.GetTotalSecondsAfterLastTrip();
            if (result.UsedTrips.Count > 0)
            {
                if (byEarliestDeparture)
                {
                    var firstTripBoardingTime = result.UsedTrips[0].stopPasses[result.UsedTrips[0].getOnStopIndex].DepartureTime.AddSeconds(result.UsedTrips[0].delayWhenBoarded);
                    var startTime = firstTripBoardingTime.AddSeconds(-secondsBeforeFirstTrip);
                    Assert.IsTrue(dateTime <= startTime);
                    Assert.AreEqual(startTime, result.DepartureDateTime);
                }
                else
                {
                    var lastTripDisembarkTime = result.UsedTrips[^1].stopPasses[result.UsedTrips[^1].getOffStopIndex].ArrivalTime.AddSeconds(result.UsedTrips[^1].currentDelay);
                    var endTime = lastTripDisembarkTime.AddSeconds(secondsAfterLastTrip);
                    Assert.IsTrue(dateTime >= endTime);
                    Assert.AreEqual(endTime, result.ArrivalDateTime);
                }
            }
            else if(!range)
            {
                if (byEarliestDeparture)
                {
                    var totalLength = secondsBeforeFirstTrip;
                    var endTime = dateTime.AddSeconds(totalLength);
                    Assert.AreEqual(endTime, result.ArrivalDateTime);
                }
                else
                {
                    var totalLength = secondsBeforeFirstTrip;
                    var startTime = dateTime.AddSeconds(-totalLength);
                    Assert.AreEqual(startTime, result.DepartureDateTime);
                }
            }
        }

        private static void AssertCorrectStartEndStops(SearchResult result, string srcStopName, string destStopName, SegmentType srcSegType, SegmentType destSegType)
        {
            switch (srcSegType)
            {
                case SearchResult.SegmentType.Transfer:
                    Assert.IsTrue(result.UsedTransfers[0].GetStartStopName() == srcStopName);
                    break;
                case SearchResult.SegmentType.Trip:
                    Assert.IsTrue(result.UsedTrips[0].GetStartStopName() == srcStopName);
                    break;
                default:
                    Assert.Fail("First segment type is not a transfer or trip");
                    break;
            }
            switch (destSegType)
            {
                case SearchResult.SegmentType.Transfer:
                    Assert.IsTrue(result.UsedTransfers[^1].GetEndStopName() == destStopName);
                    break;
                case SearchResult.SegmentType.Trip:
                    Assert.IsTrue(result.UsedTrips[^1].GetEndStopName() == destStopName);
                    break;
                default:
                    Assert.Fail("Last segment type is not a transfer or trip");
                    break;
            }
        }

        private static void AssertNoTripRepeats(SearchResult result)
        {
            HashSet<string> tripIds = new();

            foreach (var trip in result.UsedTrips)
            {
                Assert.IsFalse(tripIds.Contains(trip.tripId));
                tripIds.Add(trip.tripId);
            }
        }

        private static void AssertValidGetOnOffStops(SearchResult result)
        {
            foreach (var trip in result.UsedTrips)
            {
                Assert.IsTrue(trip.getOnStopIndex < trip.getOffStopIndex);
            }
        }

        private static void AssertSegmentsContinuous(SearchResult result)
        {
            for (int i = 0; i < result.UsedSegmentTypes.Count - 1; i++)
            {
                var thisSegType = result.UsedSegmentTypes[i];
                var nextSegType = result.UsedSegmentTypes[i + 1];

                var thisTypeIndex = result.GetTypeIndex(i);
                var nextTypeIndex = result.GetTypeIndex(i + 1);

                string thisSegmentDestId;
                string nextSegmentSrcId;
                switch (thisSegType)
                {
                    case SegmentType.Transfer:
                        thisSegmentDestId = result.UsedTransfers[thisTypeIndex].destStopInfo.Id;
                        break;
                    case SegmentType.Trip:
                        var trip = result.UsedTrips[thisTypeIndex];
                        thisSegmentDestId = trip.stopPasses[trip.getOffStopIndex].Id;
                        break;
                    case SegmentType.Bike:
                        thisSegmentDestId = result.UsedBikeTrips[thisTypeIndex].destStopInfo.Id;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid segment type");
                }
                switch (nextSegType)
                {
                    case SegmentType.Transfer:
                        nextSegmentSrcId = result.UsedTransfers[nextTypeIndex].srcStopInfo.Id;
                        break;
                    case SegmentType.Trip:
                        var trip = result.UsedTrips[nextTypeIndex];
                        nextSegmentSrcId = trip.stopPasses[trip.getOnStopIndex].Id;
                        break;
                    case SegmentType.Bike:
                        nextSegmentSrcId = result.UsedBikeTrips[nextTypeIndex].srcStopInfo.Id;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid segment type");
                }

                Assert.AreEqual(thisSegmentDestId, nextSegmentSrcId);
            }
        }

        public static void AssertResultValid(SearchResult result, string srcStopName, string destStopName, bool byEarliestDeparture, DateTime dateTime, bool range = false)
        {
            // Result was found
            Assert.IsNotNull(result);

            // Result does not start or end with a bike trip
            var segmentTypes = result.UsedSegmentTypes;
            Assert.IsFalse(segmentTypes[0] == SearchResult.SegmentType.Bike || segmentTypes[^1] == SearchResult.SegmentType.Bike);

            // Result starts and ends at the correct stops
            AssertCorrectStartEndStops(result, srcStopName, destStopName, segmentTypes[0], segmentTypes[^1]);

            // Result is valid - the sum of transfer and bike trip times between any two consecutive trips fits in the time window
            AssertEnoughTimeBetweenTrips(result);

            // Result's segments are continuous -> the i+1st segment starts where the ith segment ends
            AssertSegmentsContinuous(result);

            // Result has valid arrival and departure times
            AssertResultArrDepTimeValid(result, byEarliestDeparture, dateTime, range);

            // No trip repeats
            AssertNoTripRepeats(result);

            // Every trip's usage is valid -> getOnStopIndex < getOffStopIndex
            AssertValidGetOnOffStops(result);
        }

        public static void AssertResultValid(ConnectionApiResponseResult result, string srcStopName, string destStopName, bool byEarliestDeparture, DateTime dateTime)
        {
            // Result was found
            Assert.IsNotNull(result);
            Assert.AreEqual(ConnectionSearchError.NoError, result.Error);

            // SINGLE result was found
            Assert.IsNotNull(result.Results);
            Assert.IsTrue(result.Results.Count == 1);
            var firstResult = result.Results[0];

            // Result does not start or end with a bike trip
            var resultList = result.Results;
            var segmentTypes = firstResult.UsedSegmentTypes;
            Assert.IsFalse(segmentTypes[0] == SearchResult.SegmentType.Bike || segmentTypes[^1] == SearchResult.SegmentType.Bike);

            // Result starts and ends at the correct stops
            AssertCorrectStartEndStops(firstResult, srcStopName, destStopName, segmentTypes[0], segmentTypes[^1]);

            // Result is valid - the sum of transfer and bike trip times between any two consecutive trips fits in the time window
            AssertEnoughTimeBetweenTrips(firstResult);

            // Result's segments are continuous -> the i+1st segment starts where the ith segment ends
            AssertSegmentsContinuous(firstResult);

            // Result has valid arrival and departure times
            AssertResultArrDepTimeValid(firstResult, byEarliestDeparture, dateTime);

            // No trip repeats
            AssertNoTripRepeats(firstResult);

            // Every trip's usage is valid -> getOnStopIndex < getOffStopIndex
            AssertValidGetOnOffStops(firstResult);
        }
    }



    [TestClass]
    public class ResultIntegrityTests
    {
        [DataTestMethod]
        [StopsDataSource]
        public void ResultIntegrityDayForwardTest(string srcStopName, string destStopName)
        {
            Settings settings = Settings.DEFAULT;

            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);
            var request = new ConnectionRequest
            {
                settings = settings,
                srcStopName = srcStopName,
                destStopName = destStopName,
                dateTime = dateTime,
                byEarliestDeparture = true,
                range = false,
                srcByCoords = false,
                destByCoords = false,
                srcLat = 0.0,
                destLat = 0.0,
                srcLon = 0.0,
                destLon = 0.0
            };

            ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);

            var result = router.FindConnection(request);

            ResultIntegrityChecker.AssertResultValid(result, srcStopName, destStopName, true, dateTime);
        }

        [DataTestMethod]
        [StopsDataSource]
        public void ResultIntegrityDayBackwardTest(string srcStopName, string destStopName)
        {
            Settings settings = Settings.DEFAULT;

            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);
            var request = new ConnectionRequest
            {
                settings = settings,
                srcStopName = srcStopName,
                destStopName = destStopName,
                dateTime = dateTime,
                byEarliestDeparture = false,
                range = false,
                srcByCoords = false,
                destByCoords = false,
                srcLat = 0.0,
                destLat = 0.0,
                srcLon = 0.0,
                destLon = 0.0
            };

            ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);

            var result = router.FindConnection(request);

            ResultIntegrityChecker.AssertResultValid(result, srcStopName, destStopName, false, dateTime);
        }

        [DataTestMethod]
        [StopsDataSource]
        public void ResultIntegrityOverMidnightForwardTest(string srcStopName, string destStopName)
        {
            Settings settings = Settings.DEFAULT;

            var dateTime = new DateTime(2024, 12, 22, 23, 45, 00);
            var request = new ConnectionRequest
            {
                settings = settings,
                srcStopName = srcStopName,
                destStopName = destStopName,
                dateTime = dateTime,
                byEarliestDeparture = true,
                range = false,
                srcByCoords = false,
                destByCoords = false,
                srcLat = 0.0,
                destLat = 0.0,
                srcLon = 0.0,
                destLon = 0.0
            };

            ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);

            var result = router.FindConnection(request);

            ResultIntegrityChecker.AssertResultValid(result, srcStopName, destStopName, true, dateTime);
        }

        [DataTestMethod]
        [StopsDataSource]
        public void ResultIntegrityOverMidnightBackwardTest(string srcStopName, string DestStopName)
        {
            Settings settings = Settings.DEFAULT;

            var dateTime = new DateTime(2024, 12, 23, 00, 15, 00);
            var request = new ConnectionRequest
            {
                settings = settings,
                srcStopName = srcStopName,
                destStopName = DestStopName,
                dateTime = dateTime,
                byEarliestDeparture = false,
                range = false,
                srcByCoords = false,
                destByCoords = false,
                srcLat = 0.0,
                destLat = 0.0,
                srcLon = 0.0,
                destLon = 0.0
            };

            ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);

            var result = router.FindConnection(request);

            ResultIntegrityChecker.AssertResultValid(result, srcStopName, DestStopName, false, dateTime);
        }
    }

    [TestClass]
    public class SettingsUsageTests
    {
        [DataTestMethod]
        [StopsDataSource]
        public void ComfortBalanceTest(string srcStopName, string destStopName)
        {
            var comfortBalanceValues = new List<ComfortBalance>
            {
                ComfortBalance.ShortestTimeAbsolute, 
                ComfortBalance.ShortestTime, 
                ComfortBalance.Balanced,
                ComfortBalance.LeastTransfers
            };
            var results = new List<ConnectionApiResponseResult>();


            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);
            foreach (var comfortBalance in comfortBalanceValues)
            {
                Settings settings = Settings.DEFAULT;
                settings.ComfortBalance = comfortBalance;
                
                var request = new ConnectionRequest
                {
                    settings = settings,
                    srcStopName = srcStopName,
                    destStopName = destStopName,
                    dateTime = dateTime,
                    byEarliestDeparture = true,
                    range = false,
                    srcByCoords = false,
                    destByCoords = false,
                    srcLat = 0.0,
                    destLat = 0.0,
                    srcLon = 0.0,
                    destLon = 0.0
                };

                ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);

                var result = router.FindConnection(request);
                results.Add(result);
            }

            for (int i = 0; i < results.Count - 1; i++)
            {
                var thisResult = results[i].Results![0];
                var nextResult = results[i + 1].Results![0];

                var thisResultTripCount = thisResult.UsedTrips.Count + thisResult.UsedBikeTrips.Count;
                var nextResultTripCount = nextResult.UsedTrips.Count + nextResult.UsedBikeTrips.Count;

                var thisResultArrivalTime = thisResult.ArrivalDateTime;
                var nextResultArrivalTime = nextResult.ArrivalDateTime;

                Assert.IsTrue(thisResultTripCount >= nextResultTripCount); // This result has less comfort -> more trips
                Assert.IsTrue(thisResultArrivalTime <= nextResultArrivalTime); // This result has less comfort -> earlier arrival
            }
        }

        [DataTestMethod]
        [StopsDataSource]
        public void WalkingPreferenceTest(string srcStopName, string destStopName)
        {
            var walkingPreferenceValues = new List<WalkingPreference>
            {
                WalkingPreference.High,
                WalkingPreference.Normal,
                WalkingPreference.Low
            };
            var maxTransferLengths = new List<int>
            {
                Settings.MAX_WALK_DISTANCE_HIGH,
                Settings.MAX_WALK_DISTANCE_NORMAL,
                Settings.MAX_WALK_DISTANCE_LOW
            };
            var results = new List<ConnectionApiResponseResult>();

            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);
            foreach (var walkingPreference in walkingPreferenceValues)
            {
                Settings settings = Settings.DEFAULT;
                settings.WalkingPreference = walkingPreference;

                var request = new ConnectionRequest
                {
                    settings = settings,
                    srcStopName = srcStopName,
                    destStopName = destStopName,
                    dateTime = dateTime,
                    byEarliestDeparture = true,
                    range = false,
                    srcByCoords = false,
                    destByCoords = false,
                    srcLat = 0.0,
                    destLat = 0.0,
                    srcLon = 0.0,
                    destLon = 0.0
                };

                ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);

                var result = router.FindConnection(request);
                results.Add(result);
            }

            for(int i = 0; i < walkingPreferenceValues.Count; i++)
            {
                var searchResult = results[i].Results![0];
                var maxLength = maxTransferLengths[i];

                foreach (var transfer in searchResult.UsedTransfers)
                {
                    var length = transfer.distance;
                    Assert.IsTrue(length <= maxLength || transfer.srcStopInfo.Name == transfer.destStopInfo.Name);
                }
            }
        }

        [DataTestMethod]
        [StopsDataSource]
        public void TransferBufferTest(string srcStopName, string destStopName)
        {
            var transferBufferValues = new List<TransferBuffer>
            {
                TransferBuffer.None,
                TransferBuffer.Short,
                TransferBuffer.Normal,
                TransferBuffer.Long
            };
            var results = new List<ConnectionApiResponseResult>();

            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);
            foreach (var transferBuffer in transferBufferValues)
            {
                Settings settings = Settings.DEFAULT;
                settings.TransferBuffer = transferBuffer;

                var request = new ConnectionRequest
                {
                    settings = settings,
                    srcStopName = srcStopName,
                    destStopName = destStopName,
                    dateTime = dateTime,
                    byEarliestDeparture = true,
                    range = false,
                    srcByCoords = false,
                    destByCoords = false,
                    srcLat = 0.0,
                    destLat = 0.0,
                    srcLon = 0.0,
                    destLon = 0.0
                };

                ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);

                var result = router.FindConnection(request);
                results.Add(result);
            }

            for (int i = 0; i < transferBufferValues.Count; i++)
            {
                var searchResult = results[i].Results![0];
                var buffer = transferBufferValues[i];

                AssertTransferTimesAdjusted(searchResult, buffer);
            }

            void AssertTransferTimesAdjusted(SearchResult result, TransferBuffer buffer)
            {
                Settings settings = Settings.DEFAULT;
                settings.TransferBuffer = buffer;

                double movingTransferMpl;
                switch (buffer)
                {
                    case TransferBuffer.None:
                        movingTransferMpl = Settings.MOVING_TRANSFER_MPL_NONE;
                        break;
                    case TransferBuffer.Short:
                        movingTransferMpl = Settings.MOVING_TRANSFER_MPL_SHORT;
                        break;
                    case TransferBuffer.Normal:
                        movingTransferMpl = Settings.MOVING_TRANSFER_MPL_NORMAL;
                        break;
                    case TransferBuffer.Long:
                        movingTransferMpl = Settings.MOVING_TRANSFER_MPL_LONG;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid transfer buffer");
                }
                int stationaryTransferMinSeconds = settings.GetStationaryTransferMinimumSeconds();

                for (int i = 0; i < result.UsedTransfers.Count; i++)
                {
                    var transfer = result.UsedTransfers[i];

                    int timeIfMoving = (int)((transfer.distance / 1000.0) * 60 * settings.WalkingPace * movingTransferMpl);

                    var expectedTransferTime =
                        Math.Max(stationaryTransferMinSeconds, timeIfMoving);

                    Assert.IsTrue(Math.Abs(expectedTransferTime - transfer.time) <= 1);
                }
            }
        }

        [DataTestMethod]
        [StopsDataSource]
        public void BikeTripBufferTest(string srcStopName, string destStopName)
        {
            var bikeTripBufferValues = new List<BikeTripBuffer>
            {
                BikeTripBuffer.None,
                BikeTripBuffer.Short,
                BikeTripBuffer.Medium,
                BikeTripBuffer.Long
            };
            var results = new List<ConnectionApiResponseResult>();

            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);
            foreach (var bikeTripBuffer in bikeTripBufferValues)
            {
                Settings settings = Settings.DEFAULT;
                settings.BikeTripBuffer = bikeTripBuffer;

                var request = new ConnectionRequest
                {
                    settings = settings,
                    srcStopName = srcStopName,
                    destStopName = destStopName,
                    dateTime = dateTime,
                    byEarliestDeparture = true,
                    range = false,
                    srcByCoords = false,
                    destByCoords = false,
                    srcLat = 0.0,
                    destLat = 0.0,
                    srcLon = 0.0,
                    destLon = 0.0
                };

                ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);

                var result = router.FindConnection(request);
                results.Add(result);
            }

            for (int i = 0; i < bikeTripBufferValues.Count; i++)
            {
                var searchResult = results[i].Results![0];
                var buffer = bikeTripBufferValues[i];

                AssertTransferTimesAdjusted(searchResult, buffer);
            }

            void AssertTransferTimesAdjusted(SearchResult result, BikeTripBuffer buffer)
            {
                Settings settings = Settings.DEFAULT;
                settings.BikeTripBuffer = buffer;

                double bikeTripMultiplier;
                switch (buffer)
                {
                    case BikeTripBuffer.None:
                        bikeTripMultiplier = Settings.BIKE_TRIP_MPL_NONE;
                        break;
                    case BikeTripBuffer.Short:
                        bikeTripMultiplier = Settings.BIKE_TRIP_MPL_SHORT;
                        break;
                    case BikeTripBuffer.Medium:
                        bikeTripMultiplier = Settings.BIKE_TRIP_MPL_NORMAL;
                        break;
                    case BikeTripBuffer.Long:
                        bikeTripMultiplier = Settings.BIKE_TRIP_MPL_LONG;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid bike trip buffer");
                }

                for (int i = 0; i < result.UsedBikeTrips.Count; i++)
                {
                    var bikeTrip = result.UsedBikeTrips[i];

                    int expectedTime = (int)((bikeTrip.distance / 1000.0) * 60 * settings.CyclingPace * bikeTripMultiplier);

                    Assert.AreEqual(expectedTime, bikeTrip.time);
                }
            }
        }

        [DataTestMethod]
        [StopsDataSource]
        public void WalkingPaceTest(string srcStopName, string destStopName)
        {
            var paceValues = new List<int>
            {
                5,
                12,
                30
            };
            var results = new List<ConnectionApiResponseResult>();

            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);
            foreach (var pace in paceValues)
            {
                Settings settings = Settings.DEFAULT;
                settings.WalkingPace = pace;

                var request = new ConnectionRequest
                {
                    settings = settings,
                    srcStopName = srcStopName,
                    destStopName = destStopName,
                    dateTime = dateTime,
                    byEarliestDeparture = true,
                    range = false,
                    srcByCoords = false,
                    destByCoords = false,
                    srcLat = 0.0,
                    destLat = 0.0,
                    srcLon = 0.0,
                    destLon = 0.0
                };

                ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);

                var result = router.FindConnection(request);
                results.Add(result);
            }

            for (int i = 0; i < paceValues.Count; i++)
            {
                var searchResult = results[i].Results![0];
                var pace = paceValues[i];

                ResultIntegrityChecker.AssertResultValid(results[i], srcStopName, destStopName, true, dateTime);
                AssertTransferTimesAdjusted(searchResult, pace);
            }

            void AssertTransferTimesAdjusted(SearchResult result, int pace)
            {
                foreach (var transfer in result.UsedTransfers)
                {
                    var expectedDurationMoving = (int)((transfer.distance / 1000.0) * 60 * pace * Settings.MOVING_TRANSFER_MPL_NORMAL);
                    var expectedDurationStationary = Settings.STATIONARY_TRANSFER_MIN_NORMAL;
                    var expectedDuration = Math.Max(expectedDurationMoving, expectedDurationStationary);

                    Assert.AreEqual(expectedDuration, transfer.time);
                }
            }
        }

        [DataTestMethod]
        [StopsDataSource]
        public void CyclingPaceTest(string srcStopName, string destStopName)
        {
            var paceValues = new List<int>
            {
                2,
                8,
                30
            };
            var results = new List<ConnectionApiResponseResult>();

            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);
            foreach (var pace in paceValues)
            {
                Settings settings = Settings.DEFAULT;
                settings.CyclingPace = pace;

                var request = new ConnectionRequest
                {
                    settings = settings,
                    srcStopName = srcStopName,
                    destStopName = destStopName,
                    dateTime = dateTime,
                    byEarliestDeparture = true,
                    range = false,
                    srcByCoords = false,
                    destByCoords = false,
                    srcLat = 0.0,
                    destLat = 0.0,
                    srcLon = 0.0,
                    destLon = 0.0
                };

                ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);

                var result = router.FindConnection(request);
                results.Add(result);
            }

            for (int i = 0; i < paceValues.Count; i++)
            {
                var searchResult = results[i].Results![0];
                var pace = paceValues[i];

                ResultIntegrityChecker.AssertResultValid(results[i], srcStopName, destStopName, true, dateTime);
                AssertTransferTimesAdjusted(searchResult, pace);
            }

            void AssertTransferTimesAdjusted(SearchResult result, int pace)
            {
                foreach (var bikeTrip in result.UsedBikeTrips)
                {
                    var expectedDuration = (int)((bikeTrip.distance / 1000.0) * 60 * pace * Settings.BIKE_TRIP_MPL_NORMAL);

                    Assert.AreEqual(expectedDuration, bikeTrip.time);
                }
            }
        }

        [DataTestMethod]
        [StopsDataSource]
        public void BikeLockUnlockTimeTest(string srcStopName, string destStopName)
        {
            var lockUnlockTimes = new List<int>
            {
                0,
                30,
                60
            };
            var results = new List<ConnectionApiResponseResult>();

            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);
            foreach (var lockUnlockTime in lockUnlockTimes)
            {
                Settings settings = Settings.DEFAULT;
                settings.BikeLockTime = lockUnlockTime;
                settings.BikeUnlockTime = lockUnlockTime;

                var request = new ConnectionRequest
                {
                    settings = settings,
                    srcStopName = srcStopName,
                    destStopName = destStopName,
                    dateTime = dateTime,
                    byEarliestDeparture = true,
                    range = false,
                    srcByCoords = false,
                    destByCoords = false,
                    srcLat = 0.0,
                    destLat = 0.0,
                    srcLon = 0.0,
                    destLon = 0.0
                };

                ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);

                var result = router.FindConnection(request);
                results.Add(result);
            }

            for (int i = 0; i < lockUnlockTimes.Count; i++)
            {
                var searchResult = results[i].Results![0];
                var lockUnlockTime = lockUnlockTimes[i];

                ResultIntegrityChecker.AssertEnoughTimeBetweenTrips(searchResult, lockUnlockTime * 2);
            }
        }

        [DataTestMethod]
        [StopsDataSource]
        public void BikeMax15MinTest(string srcStopName, string destStopName)
        {
            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);
            Settings settings = Settings.DEFAULT;

            var request = new ConnectionRequest
            {
                settings = settings,
                srcStopName = srcStopName,
                destStopName = destStopName,
                dateTime = dateTime,
                byEarliestDeparture = true,
                range = false,
                srcByCoords = false,
                destByCoords = false,
                srcLat = 0.0,
                destLat = 0.0,
                srcLon = 0.0,
                destLon = 0.0
            };

            ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);

            var result = router.FindConnection(request);

            ResultIntegrityChecker.AssertResultValid(result, srcStopName, destStopName, true, dateTime);
            foreach (var bikeTrip in result.Results![0].UsedBikeTrips)
            {
                Assert.IsTrue(bikeTrip.time <= 15 * 60);
            }
        }
    }

    [TestClass]
    public class RangeRouteFinderTests
    {
        [DataTestMethod]
        [StopsDataSource]
        public void RangeRouteFinderDayForwardTest(string srcStopName, string destStopName)
        {
            Settings settings = Settings.DEFAULT;

            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);
            var request = new ConnectionRequest
            {
                settings = settings,
                srcStopName = srcStopName,
                destStopName = destStopName,
                dateTime = dateTime,
                byEarliestDeparture = true,
                range = true,
                srcByCoords = false,
                destByCoords = false,
                srcLat = 0.0,
                destLat = 0.0,
                srcLon = 0.0,
                destLon = 0.0
            };

            IRangeRouteFinder router = RouteFinderBuilder.CreateRangeRouteFinder(request.byEarliestDeparture, request.settings);

            var result = router.FindConnectionsAsync(request).Result;

            Assert.IsNotNull(result);
            Assert.AreEqual(ConnectionSearchError.NoError, result.Error);

            DateTime lastResultDepTime = result.Results![0].DepartureDateTime;
            foreach (var searchResult in result.Results)
            {
                Assert.IsTrue(lastResultDepTime <= searchResult.DepartureDateTime);
                lastResultDepTime = searchResult.DepartureDateTime;

                ResultIntegrityChecker.AssertResultValid(searchResult, srcStopName, destStopName, true, dateTime, true);
            }
        }

        [DataTestMethod]
        [StopsDataSource]
        public void RangeRouteFinderDayBackwardTest(string srcStopName, string destStopName)
        {
            Settings settings = Settings.DEFAULT;

            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);
            var request = new ConnectionRequest
            {
                settings = settings,
                srcStopName = srcStopName,
                destStopName = destStopName,
                dateTime = dateTime,
                byEarliestDeparture = false,
                range = true,
                srcByCoords = false,
                destByCoords = false,
                srcLat = 0.0,
                destLat = 0.0,
                srcLon = 0.0,
                destLon = 0.0
            };

            IRangeRouteFinder router = RouteFinderBuilder.CreateRangeRouteFinder(request.byEarliestDeparture, request.settings);

            var result = router.FindConnectionsAsync(request).Result;

            Assert.IsNotNull(result);
            Assert.AreEqual(ConnectionSearchError.NoError, result.Error);

            DateTime lastResultDepTime = result.Results![0].DepartureDateTime;
            foreach (var searchResult in result.Results)
            {
                Assert.IsTrue(lastResultDepTime <= searchResult.ArrivalDateTime);
                lastResultDepTime = searchResult.DepartureDateTime;

                ResultIntegrityChecker.AssertResultValid(searchResult, srcStopName, destStopName, false, dateTime, true);
            }
        }

        [DataTestMethod]
        [StopsDataSource]
        public void RangeRouteFinderOverMidnightForwardTest(string srcStopName, string destStopName)
        {
            Settings settings = Settings.DEFAULT;

            var dateTime = new DateTime(2024, 12, 22, 23, 45, 00);
            var request = new ConnectionRequest
            {
                settings = settings,
                srcStopName = srcStopName,
                destStopName = destStopName,
                dateTime = dateTime,
                byEarliestDeparture = true,
                range = true,
                srcByCoords = false,
                destByCoords = false,
                srcLat = 0.0,
                destLat = 0.0,
                srcLon = 0.0,
                destLon = 0.0
            };

            IRangeRouteFinder router = RouteFinderBuilder.CreateRangeRouteFinder(request.byEarliestDeparture, request.settings);

            var result = router.FindConnectionsAsync(request).Result;

            Assert.IsNotNull(result);
            Assert.AreEqual(ConnectionSearchError.NoError, result.Error);

            DateTime lastResultDepTime = result.Results![0].DepartureDateTime;
            foreach (var searchResult in result.Results)
            {
                Assert.IsTrue(lastResultDepTime <= searchResult.ArrivalDateTime);
                lastResultDepTime = searchResult.DepartureDateTime;

                ResultIntegrityChecker.AssertResultValid(searchResult, srcStopName, destStopName, true, dateTime, true);
            }
        }

        [DataTestMethod]
        [StopsDataSource]
        public void RangeRouteFinderOverMidnightBackwardTest(string srcStopName, string destStopName)
        {
            Settings settings = Settings.DEFAULT;

            var dateTime = new DateTime(2024, 12, 23, 00, 15, 00);
            var request = new ConnectionRequest
            {
                settings = settings,
                srcStopName = srcStopName,
                destStopName = destStopName,
                dateTime = dateTime,
                byEarliestDeparture = false,
                range = true,
                srcByCoords = false,
                destByCoords = false,
                srcLat = 0.0,
                destLat = 0.0,
                srcLon = 0.0,
                destLon = 0.0
            };

            IRangeRouteFinder router = RouteFinderBuilder.CreateRangeRouteFinder(request.byEarliestDeparture, request.settings);

            var result = router.FindConnectionsAsync(request).Result;

            Assert.IsNotNull(result);
            Assert.AreEqual(ConnectionSearchError.NoError, result.Error);

            DateTime lastResultDepTime = result.Results![0].DepartureDateTime;
            foreach (var searchResult in result.Results)
            {
                Assert.IsTrue(lastResultDepTime <= searchResult.ArrivalDateTime);
                lastResultDepTime = searchResult.DepartureDateTime;

                ResultIntegrityChecker.AssertResultValid(searchResult, srcStopName, destStopName, false, dateTime, true);
            }
        }
    }

    [TestClass]
    public class AlternativesRouteFinderTests
    {
        [DataTestMethod]
        [AltTripsDataSource]
        public void EarlierAlternativeTripsTest(string srcStopId, string destStopId, string tripId)
        {
            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);

            var request = new AlternativeTripsRequest
            {
                count = 10,
                dateTime = dateTime,
                srcStopId = srcStopId,
                destStopId = destStopId,
                tripId = tripId,
                previous = true
            };

            AlternativesRouteFinder router = RouteFinderBuilder.CreateDirectRouteFinder();

            var result = router.GetAlternativeTrips(request, true);

            Assert.IsNotNull(result);
            Assert.AreEqual(AlternativesSearchError.NoError, result.Error);

            var results = result.Alternatives;


            // srcNodeId is the srcStopId before it contains "Z"
            var srcNodeId = ExtractUPart(srcStopId);
            var destNodeId = ExtractUPart(destStopId);


            foreach (var alt in results)
            {
                var actualSrcNodeId = ExtractUPart(alt.stopPasses[alt.getOnStopIndex].Id);
                var actualDestNodeId = ExtractUPart(alt.stopPasses[alt.getOffStopIndex].Id);

                Assert.AreEqual(srcNodeId, actualSrcNodeId);
                Assert.AreEqual(destNodeId, actualDestNodeId);

                Assert.AreNotEqual(tripId, alt.tripId);
            }

            string ExtractUPart(string input)
            {
                var match = Regex.Match(input, @"(U\d+)");
                return match.Success ? match.Groups[1].Value : string.Empty;
            }
        }

        [DataTestMethod]
        [AltTripsDataSource]
        public void LaterAlternativeTripsTest(string srcStopId, string destStopId, string tripId)
        {
            var dateTime = new DateTime(2024, 12, 22, 07, 07, 00);

            var request = new AlternativeTripsRequest
            {
                count = 10,
                dateTime = dateTime,
                srcStopId = srcStopId,
                destStopId = destStopId,
                tripId = tripId,
                previous = false
            };

            AlternativesRouteFinder router = RouteFinderBuilder.CreateDirectRouteFinder();

            var result = router.GetAlternativeTrips(request, true);

            Assert.IsNotNull(result);
            Assert.AreEqual(AlternativesSearchError.NoError, result.Error);

            var results = result.Alternatives;

            // srcNodeId is the srcStopId before it contains "Z"
            var srcNodeId = ExtractUPart(srcStopId);
            var destNodeId = ExtractUPart(destStopId);

            foreach (var alt in results)
            {
                var actualSrcNodeId = ExtractUPart(alt.stopPasses[alt.getOnStopIndex].Id);
                var actualDestNodeId = ExtractUPart(alt.stopPasses[alt.getOffStopIndex].Id);

                Assert.AreEqual(srcNodeId, actualSrcNodeId);
                Assert.AreEqual(destNodeId, actualDestNodeId);

                Assert.AreNotEqual(tripId, alt.tripId);
            }

            string ExtractUPart(string input)
            {
                var match = Regex.Match(input, @"(U\d+)");
                return match.Success ? match.Groups[1].Value : string.Empty;
            }
        }
    }
}
#pragma warning restore CS8614
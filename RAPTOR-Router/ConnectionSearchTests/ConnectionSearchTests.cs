using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.RouteFinders;
using RAPTOR_Router.Structures.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

AppDomain.CurrentDomain.SetData("DataDirectory", Directory.GetCurrentDirectory());

namespace ConnectionSearchTests
{
    public class StopsDataSourceAttribute : Attribute, ITestDataSource
    {
        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
			string dataLocation = "D:\\Documents\\RAPTOR-router\\RAPTOR-Router\\ConnectionSearchTests\\TestData\\Data.csv";
            IEnumerable<string> lines = File.ReadLines(dataLocation);

			Console.WriteLine(dataLocation);
            foreach (string line in lines)
			{
                Console.WriteLine(line);
                string[] parts = line.Split(",");
				yield return new object[] { parts[0], parts[1] };
			}
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            if (data != null)
                return string.Format(CultureInfo.CurrentCulture, "{0} ({1})", methodInfo.Name, string.Join(",", data));

            return null;
        }
    }


    [TestClass]
	public static class TestInitClass
	{
		public static RouteFinderBuilder Builder = new RouteFinderBuilder();

		[AssemblyInitialize]
		public static void AssemblyInit(TestContext context)
		{
			var config = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory() + "..\\..\\..\\..")
				.AddJsonFile("testConfig.json", optional: false, reloadOnChange: true)
				.Build();
			string gtfsZipArchiveLocation = config["gtfsArchiveLocation"];

			if (gtfsZipArchiveLocation == null)
			{
				throw new InternalTestFailureException("Invalid gtfs zip archive path. Check the testConfig.json file");
			}

			Builder.LoadAllData(gtfsZipArchiveLocation);
		}
	}


	[TestClass]
	public class BasicConnectionSearchTests
	{
		static RouteFinderBuilder builder = TestInitClass.Builder;
        
		public void GetSrcAndDestStopNames(SearchResult result, out string resultStartStopName, out string resultEndStopName)
		{
            switch (result.UsedSegmentTypes[0])
            {
                case SearchResult.SegmentType.Transfer:
                    resultStartStopName = result.UsedTransfers[0].GetStartStopName();
                    break;
                case SearchResult.SegmentType.Trip:
                    resultStartStopName = result.UsedTrips[0].GetStartStopName();
                    break;
                case SearchResult.SegmentType.Bike:
                    resultStartStopName = result.UsedBikeTrips[0].GetStartStopName();
                    break;
                default:
                    Assert.Fail();
                    resultStartStopName = "";
                    break;
            }

            switch (result.UsedSegmentTypes[result.UsedSegmentTypes.Count - 1])
            {
                case SearchResult.SegmentType.Transfer:
                    resultEndStopName = result.UsedTransfers[result.UsedTransfers.Count - 1].GetEndStopName();
                    break;
                case SearchResult.SegmentType.Trip:
                    resultEndStopName = result.UsedTrips[result.UsedTrips.Count - 1].GetEndStopName();
                    break;
                case SearchResult.SegmentType.Bike:
                    resultEndStopName = result.UsedBikeTrips[result.UsedBikeTrips.Count - 1].GetEndStopName();
                    break;
                default:
                    Assert.Fail();
                    resultEndStopName = "";
                    break;
            }
        }

        [DataTestMethod]
		[StopsDataSource]
        public void ConnectionFoundDayTest(string srcStopName, string destStopName)
		{
			Settings settings = Settings.GetDefaultSettings();
			IRouteFinder router = builder.CreateForwardRouteFinder(settings);
			DateTime departureTime;
			DateTime.TryParse("27/11/2023 07:07:07", out departureTime);


			var result = router.FindConnection(srcStopName, destStopName, departureTime);
			string resultStartStopName;
			string resultEndStopName;

			Assert.IsNotNull(result);

			GetSrcAndDestStopNames(result, out resultStartStopName, out resultEndStopName);


			Assert.IsNotNull(result);
			Assert.AreEqual(srcStopName, resultStartStopName);
			Assert.AreEqual(destStopName, resultEndStopName);
		}

		
		[DataTestMethod]
		[StopsDataSource]
		public void ConnectionFoundBeforeMidnightTest(string srcStopName, string destStopName)
		{
			Settings settings = Settings.GetDefaultSettings();
			IRouteFinder router = builder.CreateForwardRouteFinder(settings);
			DateTime departureTime;
			DateTime.TryParse("27/11/2023 23:47:07", out departureTime);


			var result = router.FindConnection(srcStopName, destStopName, departureTime);
			
			GetSrcAndDestStopNames(result, out string resultStartStopName, out string resultEndStopName);

			Assert.IsNotNull(result);
			Assert.AreEqual(srcStopName, resultStartStopName);
			Assert.AreEqual(destStopName, resultEndStopName);
		}

		[DataRow("Chodov", "Chodov")]
		[DataRow("VětXrník", "Chodov")]
		[DataRow("Pelc Tyrolka", "Bílá hora")]
		[DataRow("Červeňanského", "Donova lská")]
		[DataRow("Geologická", "Hulická ")]
		[DataRow("xyz", "Chvaly")]
		[DataTestMethod]
		public void ImpossibleConnectionNotFoundTest(string srcStopName, string destStopName)
		{
			Settings settings = Settings.GetDefaultSettings();
			IRouteFinder router = builder.CreateForwardRouteFinder(settings);
			DateTime departureTime;
			DateTime.TryParse("27/11/2023 07:07:07", out departureTime);


			var result = router.FindConnection(srcStopName, destStopName, departureTime);


			Assert.IsNull(result);
		}
	}

	[TestClass]
	public class ComfortBalanceTests
	{
		static RouteFinderBuilder builder = TestInitClass.Builder;

        [DataTestMethod]
		[StopsDataSource]
		public void HigherComfortHasLessTripsTest(string srcStopName, string destStopName)
		{
			Settings settings1 = Settings.GetDefaultSettings();
			settings1.ComfortBalance = ComfortBalance.LeastTransfers;

			Settings settings2 = Settings.GetDefaultSettings();
			settings2.ComfortBalance = ComfortBalance.Balanced;

			Settings settings3 = Settings.GetDefaultSettings();
			settings3.ComfortBalance = ComfortBalance.ShortestTime;

			Settings settings4 = Settings.GetDefaultSettings();
			settings4.ComfortBalance = ComfortBalance.ShortestTimeAbsolute;

			IRouteFinder router1 = builder.CreateForwardRouteFinder(settings1);
			IRouteFinder router2 = builder.CreateForwardRouteFinder(settings2);
			IRouteFinder router3 = builder.CreateForwardRouteFinder(settings3);
			IRouteFinder router4 = builder.CreateForwardRouteFinder(settings4);

			DateTime departureTime;
			DateTime.TryParse("27/11/2023 07:07:07", out departureTime);


			var result1 = router1.FindConnection(srcStopName, destStopName, departureTime);
			var result2 = router2.FindConnection(srcStopName, destStopName, departureTime);
			var result3 = router3.FindConnection(srcStopName, destStopName, departureTime);
			var result4 = router4.FindConnection(srcStopName, destStopName, departureTime);

			Assert.IsNotNull(result1);
			Assert.IsNotNull(result2);
			Assert.IsNotNull(result3);
			Assert.IsNotNull(result4);
			Assert.IsTrue(
				result1.TripCount <= result2.TripCount
				&& result2.TripCount <= result3.TripCount
				&& result3.TripCount <= result4.TripCount
			);
		}
	}

	[TestClass]
	public class WalkingPreferenceTests
	{
		static RouteFinderBuilder builder = TestInitClass.Builder;

		[DataTestMethod]
		[StopsDataSource]
		public void MaxTransferDistanceTest(string srcStopName, string destStopName)
		{
            Settings settings1 = Settings.GetDefaultSettings();
			settings1.WalkingPreference = WalkingPreference.Low;
			int maxTrDist1 = settings1.GetMaxTransferDistance();

			Settings settings2 = Settings.GetDefaultSettings();
			settings2.WalkingPreference = WalkingPreference.Normal;
			int maxTrDist2 = settings2.GetMaxTransferDistance();

			Settings settings3 = Settings.GetDefaultSettings();
			settings3.WalkingPreference = WalkingPreference.High;
			int maxTrDist3 = settings3.GetMaxTransferDistance();

            DateTime departureTime;
            DateTime.TryParse("27/11/2023 07:07:07", out departureTime);

            IRouteFinder router1 = builder.CreateForwardRouteFinder(settings1);
            IRouteFinder router2 = builder.CreateForwardRouteFinder(settings2);
            IRouteFinder router3 = builder.CreateForwardRouteFinder(settings3);


            var result1 = router1.FindConnection(srcStopName, destStopName, departureTime);
            var result2 = router2.FindConnection(srcStopName, destStopName, departureTime);
            var result3 = router3.FindConnection(srcStopName, destStopName, departureTime);


			TestMaxTransferDistance(result1, maxTrDist1);
			TestMaxTransferDistance(result2, maxTrDist2);
			TestMaxTransferDistance(result3, maxTrDist3);
        }
		void TestMaxTransferDistance(SearchResult result, int maxTransferDistance) 
		{
            if (result is not null)
            {
				foreach(var transfer in result.UsedTransfers)
				{
					Assert.IsTrue(transfer.distance < maxTransferDistance || transfer.srcStopInfo.Name == transfer.destStopInfo.Name);
				}
            }
        }

	}

	[TestClass]
	public class TransferTimeTests
	{
        static RouteFinderBuilder builder = TestInitClass.Builder;

        [DataTestMethod]
        [StopsDataSource]
        public void TransferTimeTest(string srcStopName, string destStopName)
		{
            Settings settings1 = Settings.GetDefaultSettings();
			settings1.TransferTime = TransferTime.UltraShort;
			settings1.UseSharedBikes = false;
			double mpl1 = settings1.GetMovingTransferLengthMultiplier();
			int minStTime1 = settings1.GetStationaryTransferMinimumSeconds();

            Settings settings2 = Settings.GetDefaultSettings();
			settings2.TransferTime = TransferTime.Short;
            settings2.UseSharedBikes = false;
            double mpl2 = settings2.GetMovingTransferLengthMultiplier();
            int minStTime2 = settings2.GetStationaryTransferMinimumSeconds();

            Settings settings3 = Settings.GetDefaultSettings();
			settings3.TransferTime = TransferTime.Normal;
            settings3.UseSharedBikes = false;
            double mpl3 = settings3.GetMovingTransferLengthMultiplier();
            int minStTime3 = settings3.GetStationaryTransferMinimumSeconds();

            Settings settings4 = Settings.GetDefaultSettings();
            settings4.TransferTime = TransferTime.Long;
            settings4.UseSharedBikes = false;
            double mpl4 = settings4.GetMovingTransferLengthMultiplier();
            int minStTime4 = settings4.GetStationaryTransferMinimumSeconds();

            DateTime departureTime;
            DateTime.TryParse("27/11/2023 07:07:07", out departureTime);

            IRouteFinder router1 = builder.CreateForwardRouteFinder(settings1);
            IRouteFinder router2 = builder.CreateForwardRouteFinder(settings2);
            IRouteFinder router3 = builder.CreateForwardRouteFinder(settings3);
            IRouteFinder router4 = builder.CreateForwardRouteFinder(settings4);


            var result1 = router1.FindConnection(srcStopName, destStopName, departureTime);
            var result2 = router2.FindConnection(srcStopName, destStopName, departureTime);
            var result3 = router3.FindConnection(srcStopName, destStopName, departureTime);
			var result4 = router4.FindConnection(srcStopName, destStopName, departureTime);


			TestTransferTime(result1, settings1, minStTime1, mpl1);
			TestTransferTime(result2, settings2, minStTime2, mpl2);
			TestTransferTime(result3, settings3, minStTime3, mpl3);
			TestTransferTime(result4, settings4, minStTime4, mpl4);
                      
        }
		void TestTransferTime(SearchResult result, Settings settings, int minStationaryTransferTime, double movingTransferMultiplier)
		{
            if (result is not null)
            {
				int prevTripsCount = 0;
				int prevTransfersCount = 0;
				// We want to test all transfers between trips
				for(int i = 0; i < result.UsedSegmentTypes.Count - 3; i++)
				{
					var prevSegType = result.UsedSegmentTypes[i];
					var thisSegType = result.UsedSegmentTypes[i + 1];
					var nextSegType = result.UsedSegmentTypes[i + 2];
					if(prevSegType == SearchResult.SegmentType.Trip && thisSegType == SearchResult.SegmentType.Transfer && nextSegType == SearchResult.SegmentType.Trip)
					{
						var trip1 = result.UsedTrips[prevTripsCount];
						var trip2 = result.UsedTrips[prevTripsCount + 1];
						var transfer = result.UsedTransfers[prevTransfersCount];
                        int timeGap = (int)(trip2.stopPasses[trip2.getOnStopIndex].DepartureTime - trip1.stopPasses[trip1.getOffStopIndex].ArrivalTime).TotalSeconds;
                        int transferTime;
                        if (transfer.srcStopInfo.Id == transfer.destStopInfo.Id)
						{
                            transferTime = minStationaryTransferTime;
                        }
                        else
						{
                            transferTime = (int)(transfer.time * movingTransferMultiplier);
                        }

                        Console.WriteLine($"{transferTime} < {timeGap}?");
                        Assert.IsTrue(transferTime <= timeGap);
                    }
				}
            }
        }
    }

	[TestClass]
	public class PaceTests
	{
		static RouteFinderBuilder builder = TestInitClass.Builder;

		[DataTestMethod]
		[StopsDataSource]
		public void CorrectWalkingPaceTest(string srcStopName, string destStopName)
		{
            Settings settings1 = Settings.GetDefaultSettings();
			Settings settings2 = Settings.GetDefaultSettings();
			settings1.WalkingPace = 15;
			settings2.WalkingPace = 5;
            IRouteFinder router1 = builder.CreateForwardRouteFinder(settings1);
            IRouteFinder router2 = builder.CreateForwardRouteFinder(settings2);
            DateTime departureTime;
            DateTime.TryParse("27/11/2023 07:07:07", out departureTime);


            var result1 = router1.FindConnection(srcStopName, destStopName, departureTime);
            var result2 = router2.FindConnection(srcStopName, destStopName, departureTime);

            foreach (var tr in result1.UsedTransfers)
			{
				int diff = (int)(15 * 60 * tr.distance / 1000.0) - tr.time;

				Assert.IsTrue(Math.Abs(diff) <= 1);
			}
            foreach (var tr in result2.UsedTransfers)
            {
                int diff = (int)(5 * 60 * tr.distance / 1000.0) - tr.time;

                Assert.IsTrue(Math.Abs(diff) <= 1);
            }
        }
	}

	
}
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client.Payloads;
using RAPTOR_Router.RAPTORStructures;
using RAPTOR_Router.Routers;
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
			string dataLocation = "C:\\Users\\matej.LAPTOP-PB84M7CF\\Documents\\RAPTOR-router\\RAPTOR-Router\\ConnectionSearchTests\\TestData\\Data.csv";
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

			Builder.LoadDataFromGtfs(gtfsZipArchiveLocation);
		}
	}


	[TestClass]
	public class BasicConnectionSearchTests
	{
		static RouteFinderBuilder builder = TestInitClass.Builder;
        

        [DataTestMethod]
		[StopsDataSource]
        public void ConnectionFoundDayTest(string srcStopName, string destStopName)
		{
			Console.WriteLine("WTF");
			Settings settings = Settings.GetDefaultSettings();
			IRouteFinder router = builder.CreateAdvancedRouter(settings);
			DateTime departureTime;
			DateTime.TryParse("11/11/2023 07:07:07", out departureTime);


			var result = router.FindConnection(srcStopName, destStopName, departureTime);
			string resultStartStopName = result.UsedSegments[0].GetStartStopName();
			string resultEndStopName = result.UsedSegments[result.UsedSegments.Count - 1].GetEndStopName();


			Assert.IsNotNull(result);
			Assert.AreEqual(srcStopName, resultStartStopName);
			Assert.AreEqual(destStopName, resultEndStopName);
		}

		
		[DataTestMethod]
		[StopsDataSource]
		public void ConnectionFoundBeforeMidnightTest(string srcStopName, string destStopName)
		{
			Settings settings = Settings.GetDefaultSettings();
			IRouteFinder router = builder.CreateAdvancedRouter(settings);
			DateTime departureTime;
			DateTime.TryParse("10/11/2023 23:47:07", out departureTime);


			var result = router.FindConnection(srcStopName, destStopName, departureTime);
			string resultStartStopName = result.UsedSegments[0].GetStartStopName();
			string resultEndStopName = result.UsedSegments[result.UsedSegments.Count - 1].GetEndStopName();


			Assert.IsNotNull(result);
			Assert.AreEqual(srcStopName, resultStartStopName);
			Assert.AreEqual(destStopName, resultEndStopName);
		}

		[DataRow("Chodov", "Chodov")]
		[DataRow("VìtXrník", "Chodov")]
		[DataRow("Pelc Tyrolka", "Bílá hora")]
		[DataRow("Èerveòanského", "Donova lská")]
		[DataRow("Geologická", "Hulická ")]
		[DataRow("xyz", "Chvaly")]
		[DataTestMethod]
		public void ImpossibleConnectionNotFoundTest(string srcStopName, string destStopName)
		{
			Settings settings = Settings.GetDefaultSettings();
			IRouteFinder router = builder.CreateAdvancedRouter(settings);
			DateTime departureTime;
			DateTime.TryParse("11/11/2023 07:07:07", out departureTime);


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

			IRouteFinder router1 = builder.CreateAdvancedRouter(settings1);
			IRouteFinder router2 = builder.CreateAdvancedRouter(settings2);
			IRouteFinder router3 = builder.CreateAdvancedRouter(settings3);
			IRouteFinder router4 = builder.CreateAdvancedRouter(settings4);

			DateTime departureTime;
			DateTime.TryParse("11/11/2023 07:07:07", out departureTime);


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
            DateTime.TryParse("11/11/2023 07:07:07", out departureTime);

            IRouteFinder router1 = builder.CreateAdvancedRouter(settings1);
            IRouteFinder router2 = builder.CreateAdvancedRouter(settings2);
            IRouteFinder router3 = builder.CreateAdvancedRouter(settings3);


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
                foreach (var seg in result.UsedSegments)
                {
                    if (seg.segmentType == SearchResult.SegmentType.Transfer)
                    {
                        SearchResult.UsedTransfer tr = (SearchResult.UsedTransfer)seg;

                        Assert.IsTrue(tr.distance < maxTransferDistance || tr.srcStopName == tr.destStopName);
                    }
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
			double mpl1 = settings1.GetMovingTransferLengthMultiplier();
			int minStTime1 = settings1.GetStationaryTransferMinimumSeconds();

            Settings settings2 = Settings.GetDefaultSettings();
			settings2.TransferTime = TransferTime.Short;
            double mpl2 = settings2.GetMovingTransferLengthMultiplier();
            int minStTime2 = settings2.GetStationaryTransferMinimumSeconds();

            Settings settings3 = Settings.GetDefaultSettings();
			settings3.TransferTime = TransferTime.Normal;
            double mpl3 = settings3.GetMovingTransferLengthMultiplier();
            int minStTime3 = settings3.GetStationaryTransferMinimumSeconds();

            Settings settings4 = Settings.GetDefaultSettings();
            settings3.TransferTime = TransferTime.Long;
            double mpl4 = settings4.GetMovingTransferLengthMultiplier();
            int minStTime4 = settings4.GetStationaryTransferMinimumSeconds();

            DateTime departureTime;
            DateTime.TryParse("11/11/2023 07:07:07", out departureTime);

            IRouteFinder router1 = builder.CreateAdvancedRouter(settings1);
            IRouteFinder router2 = builder.CreateAdvancedRouter(settings2);
            IRouteFinder router3 = builder.CreateAdvancedRouter(settings3);
            IRouteFinder router4 = builder.CreateAdvancedRouter(settings4);


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
                for (int i = 0; i < result.UsedSegments.Count - 2; i++)
                {
                    var segment = result.UsedSegments[i];
                    if (segment.segmentType == SearchResult.SegmentType.Trip)
                    {
                        SearchResult.UsedTrip trip1 = (SearchResult.UsedTrip)result.UsedSegments[i];
                        SearchResult.UsedTrip trip2 = (SearchResult.UsedTrip)result.UsedSegments[i + 2];
                        SearchResult.UsedTransfer transfer = (SearchResult.UsedTransfer)result.UsedSegments[i + 1];
                        int timeGap = (int)(trip2.getOnTime - trip1.getOffTime).TotalSeconds;
                        int transferTime;
                        if (transfer.srcStopId == transfer.destStopId)
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
            IRouteFinder router1 = builder.CreateAdvancedRouter(settings1);
            IRouteFinder router2 = builder.CreateAdvancedRouter(settings2);
            DateTime departureTime;
            DateTime.TryParse("11/11/2023 07:07:07", out departureTime);


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
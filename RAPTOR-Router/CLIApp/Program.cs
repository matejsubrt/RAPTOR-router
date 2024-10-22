#define RANGE
#define FIXED_TIME

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.RouteFinders;
using RAPTOR_Router.Structures.Configuration;




namespace CLIApp
{
	internal class Program
    {
        private const bool forward = true;

        static void Main(string[] args)
        {
            //bool ADVANCED_ROUTING = false;

            //var config = new ConfigurationBuilder()
            //    .SetBasePath(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..")))
            //    .AddJsonFile("config.json", optional: false, reloadOnChange: true)
            //    .Build();
            //string gtfsZipArchiveLocation = config["gtfsArchiveLocation"];

            //if (gtfsZipArchiveLocation == null)
            //{
            //    Console.WriteLine("No gtfs archive found in following location: " + config["gtfsLocation"]);
            //    Console.WriteLine("Change the gtfs location in the config.json file, so that the path is correct");
            //    return;
            //}

            // Call the async method using GetAwaiter().GetResult()
            RunRouting().GetAwaiter().GetResult();
        }

        static async Task RunRouting()
        {
            Settings settings = Settings.GetDefaultSettings();
            settings.BikeTripBuffer = BikeTripBuffer.None;
            settings.BikeLockTime = 0;
            settings.BikeUnlockTime = 20;
            settings.WalkingPace = 12;
            settings.CyclingPace = 5;
            settings.TransferTime = TransferTime.Short;
            settings.ComfortBalance = (ComfortBalance)1;
            settings.WalkingPreference = 0;
            settings.UseSharedBikes = true;

            var builder = new RouteFinderBuilder();
            builder.LoadAllData();

            while (true)
            {
                Console.WriteLine("Enter the source stop:");
                string sourceStop = Console.ReadLine();
                Console.WriteLine("Enter the destination stop:");
                string destStop = Console.ReadLine();

                
                DateTime departureTime;
#if FIXED_TIME
                DateTime.TryParse("21/10/2024 16:50:00", out departureTime);
#else
                Console.WriteLine("Enter the departure time in the DD/MM/YYYY hh:mm:ss format (i.e. \"07/07/2023 07:07:07\" corresponds to 7.7.2023, 7:07:07):");
                string dateTime = Console.ReadLine();
                while (!DateTime.TryParse(dateTime, out departureTime))
                {
                    Console.WriteLine("Incorrect time, please enter a correct time in the DD/MM/YYYY hh:mm:ss format");
                    dateTime = Console.ReadLine();
                }
#endif
                Stopwatch sw = Stopwatch.StartNew();

#if RANGE
                var rangeRouter = builder.CreateRangeRouteFinder(forward, settings);
                List<SearchResult> results = new();
                // Await the async method
                await rangeRouter.FindConnectionsAsync(builder, forward, settings, departureTime, departureTime.AddMinutes(15), sourceStop, destStop, results);

                results = results.OrderBy(r => r.ArrivalDateTime).ThenBy(r => r.DepartureDateTime).ToList();

                for (int i = 0; i < results.Count - 1; i++)
                {
                    SearchResult res1 = results[i];
                    SearchResult res2 = results[i + 1];

                    if (res1.ArrivalDateTime >= res2.ArrivalDateTime)
                    {
                        results.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }

                sw.Stop();
                Console.WriteLine("Time elapsed: " + sw.Elapsed);
                sw.Reset();

                int j = 0;
                foreach (var result in results)
                {
                    //Console.WriteLine(departureTime.AddMinutes(j++));
                    Console.WriteLine(result);
                }
#else
                var basicRouter = builder.CreateUniversalRouteFinder(forward, settings);
                var result = basicRouter.FindConnectionWithAlternatives(sourceStop, destStop, departureTime);

                sw.Stop();
                Console.WriteLine("Time elapsed: " + sw.Elapsed);

                foreach (var res in result)
                {
                    Console.WriteLine(res);
                }

                //Console.WriteLine(result);
#endif
            }
        }



        //		/// <summary>
        //		/// Parses the data from the gtfsArchive (the location s specified in the config file), builds a model, runs the CLI application.
        //		/// </summary>
        //		/// <param name="args"></param>
        //		static void Main(string[] args)
        //		{
        //			bool ADVANCED_ROUTING = false;

        //			var config = new ConfigurationBuilder()
        //				.SetBasePath(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..")))
        //				.AddJsonFile("config.json", optional: false, reloadOnChange: true)
        //				.Build();
        //			string gtfsZipArchiveLocation = config["gtfsArchiveLocation"];

        //			if(gtfsZipArchiveLocation == null)
        //			{
        //				Console.WriteLine("No gtfs archive found in following location: " + config["gtfsLocation"]);
        //				Console.WriteLine("Change the gtfs location in the config.json file, so that the path is correct");
        //				return;
        //			}


        //			RunRouting(gtfsZipArchiveLocation);
        //		}

        //		static void RunRouting(string gtfsZipArchiveLocation)
        //		{
        //			Settings settings = Settings.GetDefaultSettings();
        //			settings.BikeTripBuffer = BikeTripBuffer.None;
        //			settings.BikeLockTime = 0;
        //			settings.BikeUnlockTime = 20;
        //			settings.WalkingPace = 12;
        //			settings.CyclingPace = 5;
        //			settings.TransferTime = TransferTime.Short;
        //			settings.ComfortBalance = (ComfortBalance)1;
        //			settings.WalkingPreference = 0;
        //			//settings.TransferTime = TransferTime.Short;
        //			settings.UseSharedBikes = true;

        //			var builder = new RouteFinderBuilder();
        //			builder.LoadAllData();

        //			//IRouteFinder router1 = builder.CreateBackwardRouteFinder(settings);
        //            //IRouteFinder router1 = builder.CreateUniversalRouteFinder(forward, settings);
        //			//DirectRouteFinder router = builder.CreateDirectRouteFinder();




        //   //         DateTime departureTime1;
        //   //         DateTime.TryParse("28/02/2024 13:45:30", out departureTime1);
        //			//var result = router1.FindConnection(50.1158, 14.4476, 50.0683, 14.3030, departureTime1);
        //			//Console.WriteLine(result.ToString());

        //			//router1 = builder.CreateBackwardRouteFinder(settings);



        //			while (true)
        //			{
        //				Console.WriteLine("Enter the source stop:");
        //				string sourceStop = Console.ReadLine();
        //				Console.WriteLine("Enter the destination stop:");
        //				string destStop = Console.ReadLine();


        //				DateTime departureTime;
        //#if DEBUG
        //				//DateTime.TryParse("04/10/2024 07:07:07", out departureTime);
        //				DateTime.TryParse("20/10/2024 12:00:00", out departureTime);
        //#else
        //				Console.WriteLine("Enter the departure time in the DD/MM/YYYY hh:mm:ss format (i.e. \"07/07/2023 07:07:07\" corresponds to 7.7.2023, 7:07:07):");
        //				string dateTime = Console.ReadLine();
        //				while (!DateTime.TryParse(dateTime, out departureTime))
        //				{
        //					Console.WriteLine("Incorrect time, please enter a correct time in the DD/MM/YYYY hh:mm:ss format");
        //					dateTime = Console.ReadLine();
        //				}
        //#endif







        //                //var result = router.GetAlternativeTripe(sourceStop, destStop, departureTime, 10, false);
        //                //if (result is null)
        //                //{
        //                //    Console.WriteLine("Connection could not be found, please try again");
        //                //}
        //                //else
        //                //{
        //                //    foreach (var trip in result)
        //                //    {
        //                //        Console.WriteLine(trip.ToString());
        //                //    }
        //                //}

        //                var rangeRouter = builder.CreateRangeRouteFinder(forward, settings);
        //                List<SearchResult> results = new();
        //                await rangeRouter.FindConnectionsAsync(builder, forward, settings, departureTime, 10, 10, sourceStop, destStop, results);

        //                foreach (var result in results)
        //                {
        //                    Console.WriteLine(result);
        //                }


        //                //var result1 = router1.FindConnection(sourceStop, destStop, departureTime);
        //                ////var result1 = router1.FindConnection(50.1158, 14.4476, 50.1051, 14.4743, departureTime);
        //                //            //var result1 = router1.FindConnection(50.1158, 14.4476, 50.0683, 14.3030, departureTime);
        //                //            if (result1 is null)
        //                //{
        //                //	Console.WriteLine("Connection could not be found, please try again");
        //                //}
        //                //            else
        //                //            {
        //                //                Console.WriteLine(result1.ToString());
        //                //            }

        //                //            //router1 = builder.CreateBackwardRouteFinder(settings);
        //                //router1 = builder.CreateUniversalRouteFinder(forward, settings);


        //                //router = builder.CreateDirectRouteFinder();
        //            }
        //		}
    }
}
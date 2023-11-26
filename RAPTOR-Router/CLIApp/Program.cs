using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.RAPTORStructures;
using RAPTOR_Router.Routers;


namespace CLIApp
{
    internal class Program
    {
        /// <summary>
        /// Parses the data from the gtfsArchive (the location s specified in the config file), builds a model, runs the CLI application.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            bool ADVANCED_ROUTING = true;

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory() + "..\\..\\..\\..")
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();
            string gtfsZipArchiveLocation = config["gtfsArchiveLocation"];

            if(gtfsZipArchiveLocation == null)
            {
                Console.WriteLine("No gtfs archive found in following location: " + config["gtfsLocation"]);
                Console.WriteLine("Change the gtfs location in the config.json file, so that the path is correct");
                return;
            }

            if (ADVANCED_ROUTING)
            {
                RunAdvancedRouting(gtfsZipArchiveLocation);
            }
            else
            {
                RunBasicRouting(gtfsZipArchiveLocation);
            }
        }

        static void RunBasicRouting(string gtfsZipArchiveLocation)
        {
            Settings settings = Settings.GetDefaultSettings();

            var builder = new RouteFinderBuilder();
            builder.LoadDataFromGtfs(gtfsZipArchiveLocation);

            IRouteFinder router = builder.CreateRouter(settings);


            while (true)
            {
                Console.WriteLine("Enter the source stop:");
                string sourceStop = Console.ReadLine();
                Console.WriteLine("Enter the destination stop:");
                string destStop = Console.ReadLine();


                DateTime departureTime;
                DateTime.TryParse("05/11/2023 07:07:07", out departureTime);

                var result = router.FindConnection(sourceStop, destStop, departureTime);
                if (result is null)
                {
                    Console.WriteLine("Connection could not be found, please try again");
                    continue;
                }


                Console.WriteLine(result.ToString());

                router = builder.CreateRouter(settings);
            }
        }
        static void RunAdvancedRouting(string gtfsZipArchiveLocation)
        {
            Settings settings1 = Settings.GetDefaultSettings();
            settings1.ComfortBalance = ComfortBalance.ShortestTimeAbsolute;
            settings1.WalkingPreference = WalkingPreference.Low;

            Settings settings2 = Settings.GetDefaultSettings();
            settings2.ComfortBalance = ComfortBalance.ShortestTime;
            settings2.WalkingPreference = WalkingPreference.High;

            Settings settings3 = Settings.GetDefaultSettings();
            settings3.ComfortBalance = ComfortBalance.Balanced;

            Settings settings4 = Settings.GetDefaultSettings();
            settings4.ComfortBalance = ComfortBalance.LeastTransfers;


            var builder = new RouteFinderBuilder();
            builder.LoadDataFromGtfs(gtfsZipArchiveLocation);

            IRouteFinder router1 = builder.CreateAdvancedRouter(settings1);
            IRouteFinder router2 = builder.CreateAdvancedRouter(settings2);
            IRouteFinder router3 = builder.CreateAdvancedRouter(settings3);
            IRouteFinder router4 = builder.CreateAdvancedRouter(settings4);


            while (true)
            {
                Console.WriteLine("Enter the source stop:");
                string sourceStop = Console.ReadLine();
                Console.WriteLine("Enter the destination stop:");
                string destStop = Console.ReadLine();


                DateTime departureTime;
#if DEBUG
                DateTime.TryParse("11/11/2023 07:07:07", out departureTime);
#else
                Console.WriteLine("Enter the departure time in the DD/MM/YYYY hh:mm:ss format (i.e. \"07/07/2023 07:07:07\" corresponds to 7.7.2023, 7:07:07):");
                string dateTime = Console.ReadLine();
                while (!DateTime.TryParse(dateTime, out departureTime))
                {
                    Console.WriteLine("Incorrect time, please enter a correct time in the DD/MM/YYYY hh:mm:ss format");
                    dateTime = Console.ReadLine();
                }
#endif

                var result1 = router1.FindConnection(sourceStop, destStop, departureTime);
                if (result1 is null)
                {
                    Console.WriteLine("Connection could not be found, please try again");
                    continue;
                }

                var result2 = router2.FindConnection(sourceStop, destStop, departureTime);
                var result3 = router3.FindConnection(sourceStop, destStop, departureTime);
                var result4 = router4.FindConnection(sourceStop, destStop, departureTime);


                Console.WriteLine("Shortest time absolute:");
                Console.WriteLine(result1.ToString());
                Console.WriteLine("Shortest time:");
                Console.WriteLine(result2.ToString());
                Console.WriteLine("Balanced");
                Console.WriteLine(result3.ToString());
                Console.WriteLine("Least transfers");
                Console.WriteLine(result4.ToString());


                router1 = builder.CreateAdvancedRouter(settings1);
                router2 = builder.CreateAdvancedRouter(settings2);
                router3 = builder.CreateAdvancedRouter(settings3);
                router4 = builder.CreateAdvancedRouter(settings4);
            }
        }
    }
}
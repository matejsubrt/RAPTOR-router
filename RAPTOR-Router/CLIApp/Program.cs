using Microsoft.Extensions.Configuration;
using RAPTOR_Router.RouteFinders;
using RAPTOR_Router.Structures.Configuration;


namespace CLIApp
{
	internal class Program
    {
        private const bool forward = true;



		/// <summary>
		/// Parses the data from the gtfsArchive (the location s specified in the config file), builds a model, runs the CLI application.
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			bool ADVANCED_ROUTING = false;

			var config = new ConfigurationBuilder()
				.SetBasePath(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..")))
				.AddJsonFile("config.json", optional: false, reloadOnChange: true)
				.Build();
			string gtfsZipArchiveLocation = config["gtfsArchiveLocation"];

			if(gtfsZipArchiveLocation == null)
			{
				Console.WriteLine("No gtfs archive found in following location: " + config["gtfsLocation"]);
				Console.WriteLine("Change the gtfs location in the config.json file, so that the path is correct");
				return;
			}


			RunRouting(gtfsZipArchiveLocation);
		}

		static void RunRouting(string gtfsZipArchiveLocation)
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
			//settings.TransferTime = TransferTime.Short;
			settings.UseSharedBikes = true;

			var builder = new RouteFinderBuilder();
			builder.LoadAllData();

			//IRouteFinder router1 = builder.CreateBackwardRouteFinder(settings);
            IRouteFinder router1 = builder.CreateUniversalRouteFinder(forward, settings);





   //         DateTime departureTime1;
   //         DateTime.TryParse("28/02/2024 13:45:30", out departureTime1);
			//var result = router1.FindConnection(50.1158, 14.4476, 50.0683, 14.3030, departureTime1);
			//Console.WriteLine(result.ToString());

			//router1 = builder.CreateBackwardRouteFinder(settings);



			while (true)
			{
				Console.WriteLine("Enter the source stop:");
				string sourceStop = Console.ReadLine();
				Console.WriteLine("Enter the destination stop:");
				string destStop = Console.ReadLine();


				DateTime departureTime;
#if DEBUG
				//DateTime.TryParse("04/10/2024 07:07:07", out departureTime);
				DateTime.TryParse("19/10/2024 17:00:00", out departureTime);
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
				//var result1 = router1.FindConnection(50.1158, 14.4476, 50.1051, 14.4743, departureTime);
                //var result1 = router1.FindConnection(50.1158, 14.4476, 50.0683, 14.3030, departureTime);
                if (result1 is null)
				{
					Console.WriteLine("Connection could not be found, please try again");
				}
                else
                {
                    Console.WriteLine(result1.ToString());
                }

                //router1 = builder.CreateBackwardRouteFinder(settings);
				router1 = builder.CreateUniversalRouteFinder(forward, settings);
			}
		}
	}
}
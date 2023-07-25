//#define WEB_API
//#define DEFAULT_GTFS_ARCHIVE

using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.SearchModels;
using RAPTOR_Router.RAPTORStructures;
using RAPTOR_Router.Routers;
using RAPTOR_Router.Web;


namespace RAPTOR_Router
{
	internal class Program
	{
		static void Main(string[] args)
		{
            string gtfsZipArchiveLocation;
#if DEFAULT_GTFS_ARCHIVE
			gtfsZipArchiveLocation = "..\\..\\example-gtfs\\PID_GTFS.zip";
#else
			Console.WriteLine("Enter the path to the GTFS archive that should be used for the connection search:");
            gtfsZipArchiveLocation = Console.ReadLine();
			while(!File.Exists(gtfsZipArchiveLocation))
			{
                Console.WriteLine("The path to the gtfs archive is incorrect. Please specify the correct path:");
                gtfsZipArchiveLocation = Console.ReadLine();
            }
#endif
			Settings settings = Settings.Default;
			RAPTORModel raptor;
			using (GTFS gtfs = GTFS.ParseZipFile(gtfsZipArchiveLocation))
			{
				raptor = new RAPTORModel(gtfs);
			}
			//Collect all the garbage created by the GTFS objects, that are disposed of after creating the RAPTORModel
			GC.Collect();

#if WEB_API
			API api = new API(raptor);
			api.BuildWebApp();
#else
			BuildConsoleApp(raptor);
#endif
		}
		/// <summary>
		/// Builds the console routing app and runs it
		/// </summary>
		/// <param name="raptor">The RAPTOR model of the timetable to use for routing</param>
		static void BuildConsoleApp(RAPTORModel raptor)
		{
			Settings settings = Settings.Default;
			IRouter router = new BasicRouter(settings);

			while (true)
			{
				Console.WriteLine("Enter the source stop:");
				string sourceStop = Console.ReadLine();
				Console.WriteLine("Enter the destination stop:");
				string destStop = Console.ReadLine();

				List<Stop> sourceStops = raptor.GetStopsByName(sourceStop);
				List<Stop> destStops = raptor.GetStopsByName(destStop);

				if(sourceStops.Count == 0 || destStops.Count == 0)
				{
                    Console.WriteLine("Incorrect stop name/s");
					continue;
                }
				SearchModel searchModel = new SearchModel(sourceStops, destStops, DateTime.Now.AddDays(-10));

				var result = router.FindConnection(searchModel);
				Console.WriteLine(result.ToString());

				router = new BasicRouter(settings);
			}
		}
	}
}
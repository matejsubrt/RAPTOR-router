//#define WEB_API
#define DEFAULT_GTFS_ARCHIVE
#define DEFAULT_DEPARTURE_TIME

using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.Models;
using RAPTOR_Router.Routers;
using System;


namespace RAPTOR_Router
{
    internal class Program
	{
		static void Main(string[] args)
		{
            /*
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
			*/
        }
	}
}
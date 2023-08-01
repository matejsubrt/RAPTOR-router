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


            Settings settings = Settings.Default;
            var builder = new RouterBuilder(gtfsZipArchiveLocation);
            RAPTOR_Router.Routers.IRouter router = builder.CreateRouter(settings);


            while (true)
            {
                Console.WriteLine("Enter the source stop:");
                string sourceStop = Console.ReadLine();
                Console.WriteLine("Enter the destination stop:");
                string destStop = Console.ReadLine();
                Console.WriteLine("Enter the departure time in the DD/MM/YYYY hh:mm:ss format (i.e. \"07/07/2023 07:07:07\" corresponds to 7.7.2023, 7:07:07):");
                string dateTime = Console.ReadLine();
                DateTime departureTime;
                while(!DateTime.TryParse(dateTime, out departureTime))
                {
                    Console.WriteLine("Incorrect time, please enter a correct time in the DD/MM/YYYY hh:mm:ss format");
                    dateTime = Console.ReadLine();
                }

                var result = router.FindConnection(sourceStop, destStop, departureTime);
                if(result is null)
                {
                    Console.WriteLine("Connection could not be found, please try again");
                    continue;
                }
                
                Console.WriteLine(result.ToString());
                router = builder.CreateRouter(settings);
            }
        }
    }
}
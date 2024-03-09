using RAPTOR_Router.Models.Results;
using RAPTOR_Router.RouteFinders;
using RAPTOR_Router.Structures.Configuration;

namespace WebAPI
{
    public class Program
    {
        private static RouteFinderBuilder routerBuilder;
        private static Settings settings;
        /// <summary>
        /// Parses the gtfs data in the configured zip archive, initiates a web API on /connection, that returns a JSON representation of the result of the search.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            settings = Settings.GetDefaultSettings();
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory() + "..")
                .AddJsonFile("config.json", optional: false, reloadOnChange: true)
                .Build();
            string gtfsZipArchiveLocation = config["gtfsArchiveLocation"];

            if (gtfsZipArchiveLocation == null)
            {
                Console.WriteLine("No gtfs archive found in following location: " + config["gtfsLocation"]);
                Console.WriteLine("Change the gtfs location in the config.json file, so that the path is correct");
                return;
            }

            routerBuilder = new RouteFinderBuilder();
            routerBuilder.LoadGtfsData(gtfsZipArchiveLocation);


            var appBuilder = WebApplication.CreateBuilder(args);
            appBuilder.Services.AddAuthorization();
            appBuilder.Services.AddEndpointsApiExplorer();
            appBuilder.Services.AddSwaggerGen();
            var app = appBuilder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.UseExceptionHandler(exceptionHandlerApp
                => exceptionHandlerApp.Run(async context => await Results.Problem().ExecuteAsync(context)));

            app.MapGet("/connection", (string srcStopName, string destStopName, string departureDateTime) => HandleRequest(routerBuilder.CreateRouter(settings), srcStopName, destStopName, departureDateTime))
            .WithName("GetConnection")
            .WithOpenApi();

            app.MapGet("/adv-connection", (string srcStopName, string destStopName, string departureDateTime, int walkingPace, int transferTime, int comfortBalance, int walkingPreference) =>
                HandleRequestAdvanced(routerBuilder, srcStopName, destStopName, departureDateTime, walkingPace, transferTime, comfortBalance, walkingPreference))
                .WithName("GetAdvancedConnection")
                .WithOpenApi();


            app.Run();
        }
        
        /// <summary>
        /// Handles a connection search request using the provided router.
        /// </summary>
        /// <param name="router">The initialized router to use</param>
        /// <param name="srcStopName">The exact name of the source stop</param>
        /// <param name="destStopName">The exact name of the destination stop</param>
        /// <param name="departureDateTime">The DateTime of the departure</param>
        /// <returns>The found earliest possible connection, null if none could be found.</returns>
        static SearchResult HandleRequest(RAPTOR_Router.RouteFinders.IRouteFinder router, string srcStopName, string destStopName, string departureDateTime)
        {
            Console.WriteLine();
            DateTime departureTime;
            if (!DateTime.TryParse(departureDateTime, out departureTime))
            {
                return null;
                //throw new HttpResponseException(System.Net.HttpStatusCode.BadRequest);
            }

            var result = router.FindConnection(srcStopName, destStopName, departureTime);

            return result;
        }

        static SearchResult HandleRequestAdvanced(RouteFinderBuilder builder, string srcStopName, string destStopName, string departureDateTime, int walkingPace, int transferTime, int comfortBalance, int walkingPreference)
        {
            IRouteFinder router = builder.CreateAdvancedRouter(new Settings()
            {
                WalkingPace = walkingPace,
                TransferTime = (TransferTime)transferTime,
                ComfortBalance = (ComfortBalance)comfortBalance,
                WalkingPreference = (WalkingPreference)walkingPreference
            });
            DateTime departureTime;
            if (!DateTime.TryParse(departureDateTime, out departureTime))
            {
                return null; // Or throw an appropriate exception
            }

            // TODO: Use the additional parameters in your routing logic
            // Example: router.SetPreferences(walkingPace, transferTime, comfortBalance, walkingPreference);

            var result = router.FindConnection(srcStopName, destStopName, departureTime);

            return result;
        }
    }
}
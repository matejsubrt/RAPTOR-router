using RAPTOR_Router.RAPTORStructures;
using RAPTOR_Router.Routers;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Web.Http;
using Microsoft.AspNetCore.Http;

namespace WebAPI_light
{
    public class Program
    {
        private static RouteFinderBuilder routerBuilder;
        /// <summary>
        /// Parses the gtfs data in the configured zip archive, initiates a web API on /connection, that returns a JSON representation of the result of the search.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory() + "..\\..\\..\\..")
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
            routerBuilder.LoadAllData();


            var appBuilder = WebApplication.CreateBuilder(args);
            appBuilder.Services.AddAuthorization();
            appBuilder.Services.AddEndpointsApiExplorer();
            appBuilder.Services.AddSwaggerGen();

            

            var app = appBuilder.Build();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapSwagger();
            app.UseHttpsRedirection();
            app.UseExceptionHandler(exceptionHandlerApp
                => exceptionHandlerApp.Run(async context
                    => await Results.Problem()
                        .ExecuteAsync(context)));



            app.MapGet("/connection", (string srcStopName, string destStopName, string departureDateTime) => 
                HandleRequest(routerBuilder, srcStopName, destStopName, departureDateTime))
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
        static IResult HandleRequest(RouteFinderBuilder builder, string srcStopName, string destStopName, string departureDateTime)
        {
            Console.WriteLine();
            DateTime departureTime;
            if (!DateTime.TryParse(departureDateTime, out departureTime))
            {
                var message = "Invalid DateTime format";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }

            IRouteFinder router = builder.CreateRouter(Settings.GetDefaultSettings());

            var result = router.FindConnection(srcStopName, destStopName, departureTime);

            return Results.Ok(result);
        }

        static IResult HandleRequestAdvanced(RouteFinderBuilder builder, string srcStopName, string destStopName, string departureDateTime, int walkingPace, int transferTime, int comfortBalance, int walkingPreference)
        {
            DateTime departureTime;
            if (!DateTime.TryParse(departureDateTime, out departureTime))
            {
                var message = "Invalid DateTime format";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }

            Settings settings = new Settings()
            {
                WalkingPace = walkingPace,
                TransferTime = (TransferTime)transferTime,
                ComfortBalance = (ComfortBalance)comfortBalance,
                WalkingPreference = (WalkingPreference)walkingPreference
            };

            if (!builder.ValidateSettings(settings))
            {
                var message = "Invalid settings";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }
            if (!builder.ValidateStopName(srcStopName) || !builder.ValidateStopName(destStopName))
            {
                var message = "Invalid stop name";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }

            if (DateTime.Now.AddDays(14) < departureTime)
            {
                var message = "Departure DateTime is too late";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }

            IRouteFinder router = builder.CreateAdvancedRouter(settings);

            var result = router.FindConnection(srcStopName, destStopName, departureTime);

            return Results.Ok(result);
        }
    }
}

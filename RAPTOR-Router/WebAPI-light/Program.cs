using RAPTOR_Router.RouteFinders;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using RAPTOR_Router.Structures.Configuration;

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
                .SetBasePath(Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..")))
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



            //app.MapGet("/connection", (string srcStopName, string destStopName, string departureDateTime) =>
            //    HandleRequest(routerBuilder, srcStopName, destStopName, departureDateTime))
            //    .WithName("GetConnection")
            //    .WithOpenApi();

            //app.MapGet("/adv-connection", (string srcStopName, string destStopName, string departureDateTime, int walkingPace, int transferTime, int comfortBalance, int walkingPreference) =>
            //    HandleRequestAdvanced(routerBuilder, srcStopName, destStopName, departureDateTime, walkingPace, transferTime, comfortBalance, walkingPreference))
            //    .WithName("GetAdvancedConnection")
            //    .WithOpenApi();

            //app.MapGet("/connection", (string srcStopName, string destStopName, string dateTime, int walkingPace, int cyclingPace, int bikeUnlockTime, int bikeLockTime, bool useSharedBikes, bool bikeMax15Minutes, int transferTime, int comfortBalance, int walkingPreference, int bikeTripBuffer) =>
            //    HandleRequestStops(routerBuilder, srcStopName, destStopName, dateTime, walkingPace, cyclingPace, bikeUnlockTime, bikeLockTime, useSharedBikes, bikeMax15Minutes, transferTime, comfortBalance, walkingPreference, bikeTripBuffer))
            //    .WithName("GetConnection")
            //    .WithOpenApi();
            app.MapPost("/connection/stop-to-stop", (string srcStopName, string destStopName, string dateTime, bool byEarliestDeparture, Settings settings) =>
                HandleRequestStopToStop(routerBuilder, srcStopName, destStopName, dateTime, byEarliestDeparture, settings))
                .WithName("GetConnectionByStopNames")
                .WithOpenApi();
            app.MapPost("/connection/coord-to-coord", (double srcLat, double srcLon, double destLat, double destLon, string dateTime, bool byEarliestDeparture, Settings settings) =>
                HandleRequestCoordToCoord(routerBuilder, srcLat, srcLon, destLat, destLon, dateTime, byEarliestDeparture, settings))
                .WithName("GetConnectionByCoords")
                .WithOpenApi();
            app.Run();

        }

        static IResult HandleRequestStopToStop(RouteFinderBuilder builder, string srcStopName, string destStopName, string dateTimeString, bool byEarliestDeparture, Settings settings)
        {
            // Parse the DateTime
            DateTime dateTime;
            if (!DateTime.TryParse(dateTimeString, out dateTime))
            {
                var message = "Invalid DateTime format";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }

            // Validate the settings
            if (!builder.ValidateSettings(settings))
            {
                var message = "Invalid settings";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }

            // Validate the stop names
            if (!builder.ValidateStopName(srcStopName) || !builder.ValidateStopName(destStopName))
            {
                var message = "Invalid stop name";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }

            // TODO: Validate the DateTime
            //if (DateTime.Now.AddDays(14) < dateTime)
            //{
            //    var message = "Departure DateTime is too late";
            //    HttpError err = new HttpError(message);
            //    return Results.BadRequest(err);
            //}
            //else if(dateTime < DateTime.Now.AddDays(-1)) // TODO: check
            //{
            //    var message = "Departure DateTime is in the past";
            //    HttpError err = new HttpError(message);
            //    return Results.BadRequest(err);
            //}


            IRouteFinder router;
            if (byEarliestDeparture)
            {
                router = builder.CreateForwardRouteFinder(settings);
            }
            else
            {
                router = builder.CreateBackwardRouteFinder(settings);
            }
            var result = router.FindConnection(srcStopName, destStopName, dateTime);

            if(result != null)
            {
                return Results.Ok(result);
            }
            else
            {
                var message = "No connection found";
                HttpError err = new HttpError(message);
                return Results.NotFound(err);
            }
        }
        static IResult HandleRequestCoordToCoord(RouteFinderBuilder builder, double srcLat, double srcLon, double destLat, double destLon, string dateTimeString, bool byEarliestDeparture, Settings settings)
        {
            DateTime dateTime;
            if (!DateTime.TryParse(dateTimeString, out dateTime))
            {
                var message = "Invalid DateTime format";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }

            // Validate the settings
            if (!builder.ValidateSettings(settings))
            {
                var message = "Invalid settings";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }


            if(!builder.ValidateCoords(srcLat, srcLon) || !builder.ValidateCoords(destLat, destLon))
            {
                var message = "Invalid coordinates";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }
            if(!builder.ValidateStopsNearCoords(srcLat, srcLon, settings.UseSharedBikes) || !builder.ValidateStopsNearCoords(destLat, destLon, settings.UseSharedBikes))
            {
                var message = "No stops near the coordinates";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }





            //if (DateTime.Now.AddDays(14) < dateTime)
            //{
            //    var message = "Departure DateTime is too late";
            //    HttpError err = new HttpError(message);
            //    return Results.BadRequest(err);
            //}
            //else if (dateTime < DateTime.Now.AddDays(-1)) // TODO: check
            //{
            //    var message = "Departure DateTime is in the past";
            //    HttpError err = new HttpError(message);
            //    return Results.BadRequest(err);
            //}


            IRouteFinder router;
            if (byEarliestDeparture)
            {
                router = builder.CreateForwardRouteFinder(settings);
            }
            else
            {
                router = builder.CreateBackwardRouteFinder(settings);
            }
            var result = router.FindConnection(srcLat, srcLon, destLat, destLon, dateTime);

            if (result != null)
            {
                return Results.Ok(result);
            }
            else
            {
                var message = "No connection found";
                HttpError err = new HttpError(message);
                return Results.NotFound(err);
            }
        }
    }
}

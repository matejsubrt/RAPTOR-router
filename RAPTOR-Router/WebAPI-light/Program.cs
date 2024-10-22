using RAPTOR_Router.RouteFinders;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Structures.Configuration;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WebAPI_light
{
    public class StopToStopRequest
    {
        public string srcStopName { get; set; }
        public string destStopName { get; set; }
        public string dateTime { get; set; }
        public bool byEarliestDeparture { get; set; }
        public Settings settings { get; set; }
    }

    public class StopToStopRangeRequest : StopToStopRequest
    {
        public int rangeLength { get; set; }
    }

    public class CoordToCoordRequest
    {
        public double srcLon { get; set; }
        public double srcLat { get; set; }
        public double destLon { get; set; }
        public double destLat { get; set; }
        public string dateTime { get; set; }
        public bool byEarliestDeparture { get; set; }
        public Settings settings { get; set; }
    }

    public class AlternativeTripsRequest
    {
        public string srcStopId { get; set; }
        public string destStopId { get; set; }
        public string dateTime { get; set; }
        public bool previous { get; set; }
        public int count { get; set; }
    }

    public class Program
    {
        private static RouteFinderBuilder routerBuilder;
        /// <summary>
        /// Parses the gtfs data in the configured zip archive, initiates a web API on /connection, that returns a JSON representation of the result of the search.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
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
            app.MapPost("/connection/single/stop-to-stop", (StopToStopRequest request) =>
                    HandleRequestStopToStop(routerBuilder, request.srcStopName, request.destStopName, request.dateTime, request.byEarliestDeparture, request.settings))
                .WithName("GetConnectionByStopNames")
                .WithOpenApi();

            app.MapPost("/connection/single/coord-to-coord", (CoordToCoordRequest request) =>
                HandleRequestCoordToCoord(routerBuilder, request.srcLat, request.srcLon, request.destLat, request.destLon, request.dateTime, request.byEarliestDeparture, request.settings))
                .WithName("GetConnectionByCoords")
                .WithOpenApi();


            app.MapPost("/connection/range/stop-to-stop", (StopToStopRangeRequest request) =>
                    HandleRangeRequestStopToStop(request))
                .WithName("GetConnectionsRangeByStopNames")
                .WithOpenApi();

            app.MapPost("/alternative-trips", (AlternativeTripsRequest request) => 
                HandleAlternativeTripsRequest(request))
                .WithName("GetAlternativeTrips")
                .WithOpenApi();
            app.Run();

        }


        static IResult HandleAlternativeTripsRequest(AlternativeTripsRequest request)
        {
            DateTime dateTime;
            if (!DateTime.TryParse(request.dateTime, out dateTime))
            {
                var message = "Invalid DateTime format";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }
            var routeFinder = routerBuilder.CreateDirectRouteFinder();
            var result = routeFinder.GetAlternativeTrips(request.srcStopId, request.destStopId, dateTime, request.count, request.previous);
            return Results.Ok(result);
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
            if (!settings.ValidateParameterValues())
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
            //if (byEarliestDeparture)
            //{
            //    router = builder.CreateForwardRouteFinder(settings);
            //}
            //else
            //{
            //    router = builder.CreateBackwardRouteFinder(settings);
            //}
            router = builder.CreateUniversalRouteFinder(byEarliestDeparture, settings);
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
            if (!settings.ValidateParameterValues())
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
            //if (byEarliestDeparture)
            //{
            //    router = builder.CreateForwardRouteFinder(settings);
            //}
            //else
            //{
            //    router = builder.CreateBackwardRouteFinder(settings);
            //}
            router = builder.CreateUniversalRouteFinder(byEarliestDeparture, settings);
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

        static IResult HandleRangeRequestStopToStop(StopToStopRangeRequest request)
        {
            DateTime dateTime;
            if (!DateTime.TryParse(request.dateTime, out dateTime))
            {
                var message = "Invalid DateTime format";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }

            // Validate the settings
            if (!request.settings.ValidateParameterValues())
            {
                var message = "Invalid settings";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }

            // Validate the stop names
            if (!routerBuilder.ValidateStopName(request.srcStopName) || !routerBuilder.ValidateStopName(request.destStopName))
            {
                var message = "Invalid stop name";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }



            RangeRouteFinder router;
            //if (byEarliestDeparture)
            //{
            //    router = builder.CreateForwardRouteFinder(settings);
            //}
            //else
            //{
            //    router = builder.CreateBackwardRouteFinder(settings);
            //}
            router = routerBuilder.CreateRangeRouteFinder(request.byEarliestDeparture, request.settings);
            List<SearchResult> results = new List<SearchResult>();
            router.FindConnectionsAsync(routerBuilder, request.byEarliestDeparture, request.settings, dateTime, dateTime.AddMinutes(request.rangeLength), request.srcStopName, request.destStopName, results).GetAwaiter().GetResult();
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

            if (results != null)
            {
                return Results.Ok(results);
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

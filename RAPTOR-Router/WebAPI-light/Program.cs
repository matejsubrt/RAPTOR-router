using RAPTOR_Router.RouteFinders;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Structures.Configuration;
using Microsoft.AspNetCore.Http.HttpResults;
using RAPTOR_Router.Structures.Requests;

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


            app.MapPost("/connection", (ConnectionRequest request) => 
                    HandleConnectionRequest(request))
                .WithName("GetConnection")
                .WithOpenApi();

            app.MapPost("/alternative-trips", (AlternativeTripsRequest request) => 
                    HandleAlternativeTripsRequest(request))
                .WithName("GetAlternativeTrips")
                .WithOpenApi();

            app.MapPost("/update-delays", (List<SearchResult> results) =>
                    HandleUpdateDelaysRequest(results))
                .WithName("UpdateDelays")
                .WithOpenApi();
            app.Run();

        }
        static IResult HandleConnectionRequest(ConnectionRequest request)
        {
            DateTime dateTime;
            if (request.dateTime is null)
            {
                var message = "Invalid DateTime format";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }
            else
            {
                dateTime = request.dateTime.Value;
            }

            // Validate the settings
            if (!request.settings.ValidateParameterValues())
            {
                var message = "Invalid settings";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }


            bool srcCoordsValid = true;
            bool srcCoordsHaveStops = true;
            bool destCoordsValid = true;
            bool destCoordsHaveStops = true;
            bool srcStopNameValid = true;
            bool destStopNameValid = true;

            if (request.srcByCoords)
            {
                if (!routerBuilder.ValidateCoords(request.srcLat, request.srcLon))
                {
                    srcCoordsValid = false;
                }

                if (!routerBuilder.ValidateStopsNearCoords(request.srcLat, request.srcLon,
                        request.settings.UseSharedBikes))
                {
                    srcCoordsHaveStops = false;
                }
            }
            else
            {
                if (!routerBuilder.ValidateStopName(request.srcStopName))
                {
                    srcStopNameValid = false;
                }
            }

            if (request.destByCoords)
            {
                if (!routerBuilder.ValidateCoords(request.destLat, request.destLon))
                {
                    destCoordsValid = false;
                }

                if (!routerBuilder.ValidateStopsNearCoords(request.destLat, request.destLon,
                        request.settings.UseSharedBikes))
                {
                    destCoordsHaveStops = false;
                }
            }
            else
            {
                if (!routerBuilder.ValidateStopName(request.destStopName))
                {
                    destStopNameValid = false;
                }
            }

            if (!srcCoordsValid || !destCoordsValid)
            {
                var message = "Invalid coordinates";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }

            if (!srcCoordsHaveStops || !destCoordsHaveStops)
            {
                var message = "No stops near the coordinates";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }

            if (!srcStopNameValid || !destStopNameValid)
            {
                var message = "Invalid stop name";
                HttpError err = new HttpError(message);
                return Results.BadRequest(err);
            }


            if (request.range)
            {
                RangeRouteFinder router = routerBuilder.CreateRangeRouteFinder(request.byEarliestDeparture, request.settings);
                List<SearchResult> results = new List<SearchResult>();

                if (request.srcByCoords && request.destByCoords)
                {
                    results = router.FindConnectionsAsync(routerBuilder, request).GetAwaiter().GetResult();
                }
                else if (!request.srcByCoords && !request.destByCoords)
                {
                    results = router.FindConnectionsAsync(routerBuilder, request).GetAwaiter().GetResult();
                }
                else
                {
                    results = router.FindConnectionsAsync(routerBuilder, request).GetAwaiter().GetResult();
                }

                if (results.Count == 0)
                {
                    var message = "No connection found";
                    HttpError err = new HttpError(message);
                    return Results.NotFound(err);
                }
                else
                {
                    return Results.Ok(results);
                }
            }
            else
            {
                IRouteFinder router = routerBuilder.CreateUniversalRouteFinder(request.byEarliestDeparture, request.settings);
                SearchResult result;

                if (request.srcByCoords && request.destByCoords)
                {
                    result = router.FindConnection(request.srcLat, request.srcLon, request.destLat, request.destLon, dateTime);
                }
                else if (!request.srcByCoords && !request.destByCoords)
                {
                    result = router.FindConnection(request.srcStopName, request.destStopName, dateTime);
                }
                else
                {
                    //TODO: Implement
                    var message = "Coord-to-stop and stop-to-coord searches are not yet supported";
                    HttpError err = new HttpError(message);
                    return Results.BadRequest(err);
                }

                // Return a list to be consistent with range searches
                List<SearchResult> resultsList = new List<SearchResult>
                {
                    result
                };

                if (result != null)
                {
                    return Results.Ok(resultsList);
                }
                else
                {
                    var message = "No connection found";
                    HttpError err = new HttpError(message);
                    return Results.NotFound(err);
                }
            }
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

        static IResult HandleUpdateDelaysRequest(List<SearchResult> results)
        {
            var delayUpdater = routerBuilder.CreateDelayUpdater();
            delayUpdater.UpdateDelays(results);

            return Results.Ok();
        }
    }
}

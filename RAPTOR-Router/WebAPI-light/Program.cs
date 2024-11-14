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
        //private static RouteFinderBuilder routerBuilder = new();
        /// <summary>
        /// Parses the gtfs data in the configured zip archive, initiates a web API on /connection, that returns a JSON representation of the result of the search.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            //routerBuilder = new RouteFinderBuilder();
            RouteFinderBuilder.LoadAllData();


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
        static async Task<IResult> HandleConnectionRequest(ConnectionRequest request)
        {
            if (request.settings is null)
            {
                HttpError err = new HttpError(ConnectionSearchError.InvalidSettings.ToMessage());
                return Results.BadRequest(err);
            }

            CompleteSearchResult result;
            if (request.range)
            {
                IRangeRouteFinder router = RouteFinderBuilder.CreateRangeRouteFinder(request.byEarliestDeparture, request.settings);
                result = await router.FindConnectionsAsync(request);
            }
            else
            {
                ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);
                result = router.FindConnection(request);
            }


            switch (result.Error)
            {
                case ConnectionSearchError.NoError:
                    return Results.Ok(result.Results);
                case ConnectionSearchError.NoConnectionFound:
                    HttpError err404 = new HttpError(result.Error.ToMessage());
                    return Results.NotFound(err404);
                default:
                    HttpError err500 = new HttpError(result.Error.ToMessage());
                    return Results.BadRequest(err500);
            }
        }

        static IResult HandleAlternativeTripsRequest(AlternativeTripsRequest request)
        {
            var routeFinder = RouteFinderBuilder.CreateDirectRouteFinder();
            AlternativeTripsSearchResult result = routeFinder.GetAlternativeTrips(request);

            switch (result.Error)
            {
                case AlternativesSearchError.NoError:
                    return Results.Ok(result.Alternatives);
                case AlternativesSearchError.NoTripsFound:
                    HttpError err404 = new HttpError(result.Error.ToMessage());
                    return Results.NotFound(err404);
                default:
                    HttpError err500 = new HttpError(result.Error.ToMessage());
                    return Results.BadRequest(err500);
            }
        }

        static IResult HandleUpdateDelaysRequest(List<SearchResult> results)
        {
            var delayUpdater = RouteFinderBuilder.CreateDelayUpdater();
            delayUpdater.UpdateDelays(results);

            return Results.Ok(results);
        }
    }
}

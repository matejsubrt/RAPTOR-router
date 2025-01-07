using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.RouteFinders;
using RAPTOR_Router.Structures.Configuration;
using RAPTOR_Router.Structures.Requests;

namespace WebAPI
{
    public class Program
    {
        /// <summary>
        /// Parses the GTFS data in the configured zip archive, initiates a web API on /connection, that returns a JSON representation of the result of the search.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
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
                    => await Results.Problem().ExecuteAsync(context)));

            app.MapPost("/connection", (ConnectionRequest request) => HandleConnectionRequest(request))
                .WithName("GetConnection")
                .WithOpenApi()
                .Produces<List<SearchResult>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

            app.MapPost("/alternative-trips", (AlternativeTripsRequest request) => HandleAlternativeTripsRequest(request))
                .WithName("GetAlternativeTrips")
                .WithOpenApi()
                .Produces<List<SearchResult>>(StatusCodes.Status200OK)
                .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
                .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

            app.MapPost("/update-delays", (List<SearchResult> results) => HandleUpdateDelaysRequest(results))
                .WithName("UpdateDelays")
                .WithOpenApi()
                .Accepts<List<SearchResult>>("application/json")
                .Produces<List<SearchResult>>(StatusCodes.Status200OK);

            app.Run();
        }

        static async Task<ActionResult<IEnumerable<SearchResult>>> HandleConnectionRequest(ConnectionRequest request)
        {
            if (request.settings is null)
            {
                return new BadRequestObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid settings",
                    Detail = ConnectionSearchError.InvalidSettings.ToMessage()
                });
            }

            ConnectionApiResponseResult apiResponseResult;
            if (request.range)
            {
                IRangeRouteFinder router = RouteFinderBuilder.CreateRangeRouteFinder(request.byEarliestDeparture, request.settings);
                apiResponseResult = await router.FindConnectionsAsync(request);
            }
            else
            {
                ISimpleRouteFinder router = RouteFinderBuilder.CreateSimpleRouteFinder(request.byEarliestDeparture, request.settings);
                apiResponseResult = router.FindConnection(request);
            }

            switch (apiResponseResult.Error)
            {
                case ConnectionSearchError.NoError:
                    return new OkObjectResult(apiResponseResult.Results);
                case ConnectionSearchError.NoConnectionFound:
                    return new NotFoundObjectResult(new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "No connection found",
                        Detail = apiResponseResult.Error.ToMessage()
                    });
                default:
                    return new BadRequestObjectResult(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Bad request",
                        Detail = apiResponseResult.Error.ToMessage()
                    });
            }
        }

        static ActionResult<IEnumerable<SearchResult.UsedTrip>> HandleAlternativeTripsRequest(AlternativeTripsRequest request)
        {
            var routeFinder = RouteFinderBuilder.CreateDirectRouteFinder();
            AlternativeTripsApiResponseResult result = routeFinder.GetAlternativeTrips(request);

            switch (result.Error)
            {
                case AlternativesSearchError.NoError:
                    return new OkObjectResult(result.Alternatives);
                case AlternativesSearchError.NoTripsFound:
                    return new NotFoundObjectResult(new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "No alternative trips found",
                        Detail = result.Error.ToMessage()
                    });
                default:
                    return new BadRequestObjectResult(new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Bad request",
                        Detail = result.Error.ToMessage()
                    });
            }
        }

        static ActionResult<IEnumerable<SearchResult>> HandleUpdateDelaysRequest(List<SearchResult> results)
        {
            var delayUpdater = RouteFinderBuilder.CreateDelayUpdater();
            delayUpdater.UpdateDelays(results);

            return new OkObjectResult(results);
        }
    }
}

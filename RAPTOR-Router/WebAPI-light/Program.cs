using RAPTOR_Router.RouteFinders;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using RAPTOR_Router.Models.Results;
using RAPTOR_Router.Structures.Requests;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace WebAPI_light
{
    public class Program
    {
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
                    => await Results.Problem()
                        .ExecuteAsync(context)));

            app.MapPost("/connection", (ConnectionRequest request) => HandleConnectionRequest(request))
                .WithName("GetConnection")
                .WithOpenApi();

            app.MapPost("/alternative-trips", (AlternativeTripsRequest request) => HandleAlternativeTripsRequest(request))
                .WithName("GetAlternativeTrips")
                .WithOpenApi();

            app.MapPost("/update-delays", (List<SearchResult> results) => HandleUpdateDelaysRequest(results))
                .WithName("UpdateDelays")
                .WithOpenApi();

            app.Run();
        }

        static async Task<IResult> HandleConnectionRequest(ConnectionRequest request)
        {
            if (request.settings is null)
            {
                var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid settings",
                    Detail = ConnectionSearchError.InvalidSettings.ToMessage()
                };
                return Results.BadRequest(problemDetails);
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
                    return Results.Ok(apiResponseResult.Results);
                case ConnectionSearchError.NoConnectionFound:
                    var notFoundDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "No connection found",
                        Detail = apiResponseResult.Error.ToMessage()
                    };
                    return Results.NotFound(notFoundDetails);
                default:
                    var badRequestDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Bad request",
                        Detail = apiResponseResult.Error.ToMessage()
                    };
                    return Results.BadRequest(badRequestDetails);
            }
        }

        static IResult HandleAlternativeTripsRequest(AlternativeTripsRequest request)
        {
            var routeFinder = RouteFinderBuilder.CreateDirectRouteFinder();
            AlternativeTripsApiResponseResult result = routeFinder.GetAlternativeTrips(request);

            switch (result.Error)
            {
                case AlternativesSearchError.NoError:
                    return Results.Ok(result.Alternatives);
                case AlternativesSearchError.NoTripsFound:
                    var notFoundDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "No alternative trips found",
                        Detail = result.Error.ToMessage()
                    };
                    return Results.NotFound(notFoundDetails);
                default:
                    var badRequestDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Bad request",
                        Detail = result.Error.ToMessage()
                    };
                    return Results.BadRequest(badRequestDetails);
            }
        }

        static IResult HandleUpdateDelaysRequest(List<SearchResult> results)
        {
            var delayUpdater = RouteFinderBuilder.CreateDelayUpdater();
            var newResults = delayUpdater.UpdateDelays(results);

            return Results.Ok(newResults);
        }
    }
}


using Microsoft.AspNetCore.Builder;
using RAPTOR_Router.SearchModels;
using RAPTOR_Router.RAPTORStructures;
using RAPTOR_Router.Routers;

namespace RAPTOR_Router.Web
{
    /// <summary>
    /// Class representing the web API for the algorithm
    /// </summary>
    internal class API
    {
        /// <summary>
        /// The RAPTOR Model, which should be used to search for the connections
        /// </summary>
        private RAPTORModel raptor;
        /// <summary>
        /// Creates a new API
        /// </summary>
        /// <param name="raptor">The RAPTOR Model to be used for the connection searches of the API</param>
        internal API(RAPTORModel raptor)
        {
            this.raptor = raptor;
        }
        /// <summary>
        /// Builds and runs the web application with the API
        /// </summary>
        internal void BuildWebApp()
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();
            app.MapGet("/connection", (string srcStopName, string destStopName, string dateTime) => HandleRequest(srcStopName, destStopName, dateTime));

            app.Run();
        }
        /// <summary>
        /// Finds a result connection between stops with the specified names on the specified date and time
        /// </summary>
        /// <param name="srcStopName">The name of the source stop</param>
        /// <param name="destStopName">The name of the destination stop</param>
        /// <param name="dateTime">The date and time of the earliest possible departure in YYYYMMDDhhmmss format</param>
        /// <returns>The result of the connection search to be converted to json by the API</returns>
        SearchResult HandleRequest(string srcStopName, string destStopName, string dateTime)
        {
            BasicRouteFinder router = new BasicRouteFinder(Settings.GetDefaultSettings(), raptor);
            List<Stop> sourceStops = raptor.GetStopsByName(srcStopName);
            List<Stop> destStops = raptor.GetStopsByName(destStopName);

            DateTime departureTime = new DateTime(int.Parse(dateTime.Substring(0, 4)), int.Parse(dateTime.Substring(4, 2)), int.Parse(dateTime.Substring(6, 2)), int.Parse(dateTime.Substring(8, 2)), int.Parse(dateTime.Substring(10, 2)), int.Parse(dateTime.Substring(12, 2)));

            SearchModel searchModel = new SearchModel(sourceStops, destStops, departureTime, Settings.GetDefaultSettings()); //TODO: add settings support
            var result = router.FindConnection(searchModel);

            return result;
        }
    }
}

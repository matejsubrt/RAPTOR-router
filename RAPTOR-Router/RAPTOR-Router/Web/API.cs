using Microsoft.AspNetCore.Builder;
using RAPTOR_Router.Problems;
using RAPTOR_Router.RAPTORStructures;
using RAPTOR_Router.Routers;

namespace RAPTOR_Router.Web
{
    internal class API
    {
        RAPTORModel raptor;
        public API(RAPTORModel raptor)
        {
            this.raptor = raptor;
        }
        public void BuildWebApp()
        {
            var builder = WebApplication.CreateBuilder();
            var app = builder.Build();
            app.MapGet("/connection", (string srcStopName, string destStopName, string dateTime) => HandleRequest(srcStopName, destStopName, dateTime));

            app.Run();
        }
        SearchResultDTO HandleRequest(string srcStopName, string destStopName, string dateTime)
        {
            IRouter router = new BasicRouter(Settings.Default);
            List<Stop> sourceStops = raptor.GetStopsByName(srcStopName);
            List<Stop> destStops = raptor.GetStopsByName(destStopName);

            DateTime departureTime = new DateTime(int.Parse(dateTime.Substring(0, 4)), int.Parse(dateTime.Substring(4, 2)), int.Parse(dateTime.Substring(6, 2)), int.Parse(dateTime.Substring(8, 2)), int.Parse(dateTime.Substring(10, 2)), int.Parse(dateTime.Substring(12, 2)));

            JourneySearchModel searchModel = new JourneySearchModel(raptor, sourceStops, destStops, departureTime);
            var result = router.FindConnection(searchModel);

            return new SearchResultDTO(result);
        }
    }
}

using RAPTOR_Router.RAPTORStructures;
using RAPTOR_Router.Routers;
using System.Web.Http;
using System.Net.Http;

namespace WebAPI
{
    public class Program
    {
        private static RouterBuilder routerBuilder;
        private static Settings settings;
        /// <summary>
        /// Parses the gtfs data in the configured zip archive, initiates a web API on /connection, that returns a JSON representation of the result of the search.
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            settings = Settings.Default;
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
            routerBuilder = new RouterBuilder(gtfsZipArchiveLocation);


            var appBuilder = WebApplication.CreateBuilder(args);
            // Add services to the container.
            appBuilder.Services.AddAuthorization();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            appBuilder.Services.AddEndpointsApiExplorer();
            appBuilder.Services.AddSwaggerGen();
            var app = appBuilder.Build();

            // Configure the HTTP request pipeline.
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

            app.Run();
        }
        
        /// <summary>
        /// Handles a connection search request using the provided router.
        /// </summary>
        /// <param name="router">The initialized router to use</param>
        /// <param name="srcStopName">The exact name of the source stop</param>
        /// <param name="destStopName">The exxact name of the destination stop</param>
        /// <param name="departureDateTime">The DateTime of the departure</param>
        /// <returns>The found earliest possible connection, null if none could be found.</returns>
        static SearchResult HandleRequest(RAPTOR_Router.Routers.IRouter router, string srcStopName, string destStopName, string departureDateTime)
        {
            DateTime departureTime;
            if (!DateTime.TryParse(departureDateTime, out departureTime))
            {
                return null;
                //throw new HttpResponseException(System.Net.HttpStatusCode.BadRequest);
            }

            var result = router.FindConnection(srcStopName, destStopName, departureTime);

            return result;
        }
    }
}
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

            app.MapGet("/connection", (string srcStopName, string destStopName, string dateTime) => HandleRequest(routerBuilder.CreateRouter(settings), srcStopName, destStopName, dateTime))
            .WithName("GetConnection")
            .WithOpenApi();

            app.Run();
        }
        
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
using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.Problems;
using RAPTOR_Router.RAPTORStructures;
using RAPTOR_Router.Routers;
using System.Diagnostics;
using RAPTOR_Router.Web;

namespace RAPTOR_Router
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine(stopwatch.Elapsed + ": Stopwatch started");

            Settings settings = Settings.Default;
            RAPTORModel raptor;
            using (GTFS gtfs = GTFS.ParseZipFile("..\\..\\example-gtfs\\PID_GTFS.zip"))
            {
                Console.WriteLine(stopwatch.Elapsed + ": GTFS Loaded, \t\tMemory:" + GC.GetTotalMemory(false));
                raptor = new RAPTORModel(gtfs, stopwatch);
                Console.WriteLine(stopwatch.Elapsed + ": RAPTOR Model loaded, \tMemory:" + GC.GetTotalMemory(false));
            }
            GC.Collect();

            API api = new API(raptor);
            api.BuildWebApp();

            /*
            IRouter router = new BasicRouter(settings);

            Console.WriteLine(stopwatch.Elapsed + ": Router created, \tMemory:" + GC.GetTotalMemory(false));

            
            while (true)
            {
                Console.WriteLine("Enter the source stop:");
                string sourceStop = Console.ReadLine();
                Console.WriteLine("Enter the destination stop:");
                string destStop = Console.ReadLine();

                List<Stop> sourceStops = raptor.GetStopsByName(sourceStop);
                List<Stop> destStops = raptor.GetStopsByName(destStop);

                JourneySearchModel searchModel = new JourneySearchModel(raptor, sourceStops, destStops, DateTime.Now.AddDays(-10));

                var result = router.FindConnection(searchModel);
                Console.WriteLine(result.ToString());
                //Console.WriteLine(stopwatch.Elapsed + ": Result found, \tMemory:" + GC.GetTotalMemory(false));

                router = new BasicRouter(settings);
            }
            */
        }
        
    }
    public class Search
    {
        RAPTORModel raptor;
        IRouter router;

        public Search()
        {
            using (GTFS gtfs = GTFS.ParseZipFile("..\\..\\example-gtfs\\PID_GTFS.zip"))
            {
                raptor = new RAPTORModel(gtfs);
            }
            GC.Collect();
        }

        public string GetConnectionAsJson(string srcStopName, string destStopName, string dateTime)
        {
            router = new BasicRouter(Settings.Default);
            List<Stop> sourceStops = raptor.GetStopsByName(srcStopName);
            List<Stop> destStops = raptor.GetStopsByName(destStopName);

            DateTime departureTime = new DateTime(int.Parse(dateTime.Substring(0, 4)), int.Parse(dateTime.Substring(4, 2)), int.Parse(dateTime.Substring(6, 2)), int.Parse(dateTime.Substring(8, 2)), int.Parse(dateTime.Substring(10, 2)), int.Parse(dateTime.Substring(12, 2)));

            JourneySearchModel searchModel = new JourneySearchModel(raptor, sourceStops, destStops, departureTime);
            var result = router.FindConnection(searchModel);

            return result.ToString();
        }

        public Result GetSearchResult(string srcStopName, string destStopName, string dateTime)
        {
            router = new BasicRouter(Settings.Default);
            List<Stop> sourceStops = raptor.GetStopsByName(srcStopName);
            List<Stop> destStops = raptor.GetStopsByName(destStopName);

            DateTime departureTime = new DateTime(int.Parse(dateTime.Substring(0, 4)), int.Parse(dateTime.Substring(4, 2)), int.Parse(dateTime.Substring(6, 2)), int.Parse(dateTime.Substring(8, 2)), int.Parse(dateTime.Substring(10, 2)), int.Parse(dateTime.Substring(12, 2)));

            JourneySearchModel searchModel = new JourneySearchModel(raptor, sourceStops, destStops, departureTime);
            var searchResult = router.FindConnection(searchModel);
            Result result = new Result();
            foreach(Trip trip in searchResult.UsedTrips)
            {
                result.usedTrips.Add(trip.Route.ShortName);
            }
            foreach(Transfer transfer in searchResult.UsedTransfers)
            {
                result.usedTransfers.Add(transfer.From.Name + " ==> " + transfer.To.Name + " in " + transfer.Time + " sec");
            }
            return result;
        }
    }

    public class Result
    {
        public List<string> usedTrips { get; set; } = new();
        public List<string> usedTransfers { get; set; } = new();
    }
}
using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.Problems;
using RAPTOR_Router.RAPTORStructures;
using RAPTOR_Router.Routers;
using System.Diagnostics;

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
            //IRouter router = new BasicRouter(raptor);
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
        }
    }
}
using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.RAPTORStructures;
using RAPTOR_Router.Routers;
using System.Diagnostics;

namespace RAPTOR_Router
{
    internal class Program
    {
        static void Main(string[] args)
        {
            
            RAPTORModel raptor;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Console.WriteLine(stopwatch.Elapsed + ": Stopwatch started");
            using (GTFS gtfs = GTFS.ParseZipFile("..\\..\\example-gtfs\\PID_GTFS.zip"))
            {
                Console.WriteLine(stopwatch.Elapsed + ": GTFS Loaded, \t\tMemory:" + GC.GetTotalMemory(false));
                raptor = new RAPTORModel(gtfs, stopwatch);
                Console.WriteLine(stopwatch.Elapsed + ": RAPTOR Model loaded, \tMemory:" + GC.GetTotalMemory(false));
            }
            GC.Collect();
            IRouter router = new BasicRouterNew(raptor);
            Console.WriteLine(stopwatch.Elapsed + ": Router created, \tMemory:" + GC.GetTotalMemory(false));



            Console.WriteLine("Enter the source stop:");
            string sourceStop = Console.ReadLine();
            Console.WriteLine("Enter the destination stop:");
            string destStop = Console.ReadLine();

            List<string> sourceStopIds = raptor.GetStopsIdByName(sourceStop);
            List<string> destStopIds = raptor.GetStopsIdByName(destStop);

            var result = router.FindConnection(sourceStopIds, destStopIds, DateTime.Now);
            Console.WriteLine(stopwatch.Elapsed + ": Connection successfully found, \tMemory:" + GC.GetTotalMemory(false));
            Console.WriteLine(result.ToString());
        }
    }
}
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
            IRouter router = new BasicRouter(raptor);
            Console.WriteLine(stopwatch.Elapsed + ": Router created, \tMemory:" + GC.GetTotalMemory(false));
            //From Vetrnik to Motol
            var result = router.FindConnection("U844Z1P", "U394Z1P", new DateTime(2023, 7, 11, 17, 43, 0));
            Console.WriteLine(stopwatch.Elapsed + ": Connection successfully found, \tMemory:" + GC.GetTotalMemory(false));
            Console.WriteLine(result.ToString());
        }
    }
}
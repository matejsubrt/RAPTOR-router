using RAPTOR_Router.GTFSParsing;
using RAPTOR_Router.RAPTORStructures;
using RAPTOR_Router.Routers;

namespace RAPTOR_Router
{
    internal class Program
    {
        static void Main(string[] args)
        {
            GTFS gtfs = GTFS.ParseZipFile("..\\..\\example-gtfs\\PID_GTFS.zip");
            Console.WriteLine();
            RAPTORModel raptor = new RAPTORModel(gtfs);
            gtfs = null;
            GC.Collect();
            IRouter router = new BasicRouter(raptor);
            var result = router.FindConnection("U844Z1P", "U394Z1P", new DateTime(2023, 7, 11, 17, 43, 0));
            Console.WriteLine(result.ToString());
        }
    }
}